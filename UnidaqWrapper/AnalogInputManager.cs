using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel; // For Description Attribute
using System.Diagnostics;
using System.Linq;
using System.Reflection;     // For getting Description
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using UniDAQ_Ns;

namespace IcpDas.Daq.Analog
{
    /// <summary>
    /// Manages Analog Input operations for ICPDAS DAQ Cards.
    /// Supports Enum-based configuration for Voltage Ranges.
    /// </summary>
    public class AnalogInputManager : IDisposable
    {
        #region Events

        public event EventHandler<AnalogDataEventArgs> DataReceived;
        public event EventHandler<AnalogMultiChannelDataEventArgs> MultiChannelDataReceived;
        public event EventHandler<AnalogErrorEventArgs> ErrorOccurred;
        public event EventHandler<string> InitializationCompleted;
        public event EventHandler<long> ReadCycleCompleted;

        #endregion

        #region Fields & Properties

        private const int PARALLEL_THRESHOLD = 1000;
        private const int STOP_TIMEOUT_MS = 500;

        private readonly ReaderWriterLockSlim _channelLock = new ReaderWriterLockSlim();
        private readonly List<AnalogInputChannel> _channels = new List<AnalogInputChannel>();

        private CancellationTokenSource _cts;
        private Task _readingTask;
        private volatile bool _isRunning;
        private volatile bool _stopRequested;
        private int _retryCount;
        private int _stateValue = (int)AnalogState.Stopped;
        private bool _isInitialized;

        public ushort BoardNo { get; }
        public ushort CardType { get; set; }
        public float SamplingRate { get; private set; } = 1000f;
        public uint DataCount { get; private set; } = 256;
        public bool UseMultiChannelOutput { get; set; } = true;
        public bool UseParallel { get; set; }
        public int RetryLimit { get; set; } = 5;

        public AnalogState State
        {
            get => (AnalogState)Interlocked.CompareExchange(ref _stateValue, 0, 0);
            private set => Interlocked.Exchange(ref _stateValue, (int)value);
        }

        #endregion

        #region Constructor

        public AnalogInputManager(ushort boardNo, bool isHighGain = false)
        {
            BoardNo = boardNo;
            CardType = isHighGain ? (ushort)1 : (ushort)0;
            InitializeCard();
        }

        public IReadOnlyList<AnalogInputChannel> Channels => _channels;

        #endregion

        #region Public Methods

        /// <summary>
        /// Configures a new analog input channel using the VoltageRange Enum.
        /// </summary>
        /// <param name="name">Unique channel name.</param>
        /// <param name="index">Physical channel index.</param>
        /// <param name="range">Voltage range enum (e.g. VoltageRange.Bipolar_10V).</param>
        public void AddChannel(string name, int index, VoltageRange range,
            int movingAverageWindow = 0, double[] regressionCoeffs = null, float zeroOffset = 0)
        {
            _channelLock.EnterWriteLock();
            try
            {
                // Cast Enum directly to ushort for the Config Code
                ushort configCode = (ushort)range;

                var channel = new AnalogInputChannel(name, index, configCode, regressionCoeffs, zeroOffset);

                if (movingAverageWindow > 1)
                    channel.SetFilter(movingAverageWindow);

                _channels.Add(channel);
            }
            finally { _channelLock.ExitWriteLock(); }
        }

        public void ClearAllChannels()
        {
            _channelLock.EnterWriteLock();
            try { _channels.Clear(); }
            finally { _channelLock.ExitWriteLock(); }
        }

        public void Start(float samplingRate, uint dataCount)
        {
            if (_isRunning) return;
            if (!_isInitialized)
            {
                RaiseError("Start", -1, UniDAQ.Ixud_SetControlErr, $"Card {BoardNo} not initialized.");
                return;
            }

            _channelLock.EnterReadLock();
            try
            {
                if (_channels.Count == 0)
                {
                    RaiseError("Start", -1, UniDAQ.Ixud_InvalidChannels, "No channels configured.");
                    return;
                }
            }
            finally { _channelLock.ExitReadLock(); }

            if (samplingRate < 100 || samplingRate > 200000)
            {
                RaiseError("Start", -1, UniDAQ.Ixud_InvalidDataCount, "Invalid sampling rate.");
                return;
            }

            SamplingRate = samplingRate;
            DataCount = dataCount;

            if (_stopRequested) Interlocked.Exchange(ref _retryCount, 0);
            _stopRequested = false;

            _cts?.Dispose();
            _cts = new CancellationTokenSource();

            _isRunning = true;
            State = AnalogState.Running;
            _readingTask = Task.Run(() => ReadAnalogLoop(_cts.Token));
        }

        public void Stop()
        {
            if (!_isRunning) return;

            _stopRequested = true;
            Interlocked.Exchange(ref _retryCount, 0);

            try
            {
                _cts?.Cancel();
                _readingTask?.Wait(STOP_TIMEOUT_MS);
            }
            catch (Exception ex)
            {
                RaiseError("System", -1, 9999, $"Error stopping: {ex.Message}");
            }

            _isRunning = false;
            State = AnalogState.Stopped;
        }

        public void UpdateZero(string name, float newZero)
        {
            ModifyChannel(name, ch => ch.Zero = newZero);
        }

        public void UpdateRegression(string name, double[] newRegression)
        {
            ModifyChannel(name, ch => ch.Regression = newRegression);
        }

        #endregion

        #region Private Logic (Reading Loop)

        private bool InitializeCard()
        {
            ushort rtn = UniDAQ.Ixud_ConfigAI(BoardNo, 2, 2048, CardType, 0);

            if (rtn == UniDAQ.Ixud_NoErr)
            {
                _isInitialized = true;
                InitializationCompleted?.Invoke(this, $"Board {BoardNo} initialized.");
                return true;
            }

            _isInitialized = false;
            RaiseError("Init", -1, rtn, "Initialization failed.");
            return false;
        }

        private void ModifyChannel(string name, Action<AnalogInputChannel> action)
        {
            _channelLock.EnterUpgradeableReadLock();
            try
            {
                var channel = _channels.FirstOrDefault(ch => ch.Name == name);
                if (channel != null)
                {
                    _channelLock.EnterWriteLock();
                    try { action(channel); }
                    finally { _channelLock.ExitWriteLock(); }
                }
            }
            finally { _channelLock.ExitUpgradeableReadLock(); }
        }

        private async Task ReadAnalogLoop(CancellationToken token)
        {
            ushort[] chList, confList;
            AnalogInputChannel[] channelsSnapshot;

            _channelLock.EnterReadLock();
            try
            {
                chList = _channels.Select(c => (ushort)c.Index).ToArray();
                confList = _channels.Select(c => c.ConfigCode).ToArray();
            }
            finally { _channelLock.ExitReadLock(); }

            float[] flatBuffer = new float[DataCount * chList.Length];
            float[,] matrixBuffer = UseMultiChannelOutput ? new float[DataCount, chList.Length] : null;
            float[,] rawMatrixBuffer = UseMultiChannelOutput ? new float[DataCount, chList.Length] : null;

            var sw = new Stopwatch();

            try
            {
                while (!token.IsCancellationRequested)
                {
                    sw.Restart();

                    if (!PerformHardwareOperations(chList, confList, flatBuffer)) break;

                    _channelLock.EnterReadLock();
                    try { channelsSnapshot = _channels.ToArray(); }
                    finally { _channelLock.ExitReadLock(); }

                    bool parallel = UseParallel && DataCount > PARALLEL_THRESHOLD;

                    if (UseMultiChannelOutput)
                        ProcessMultiChannel(channelsSnapshot, flatBuffer, matrixBuffer, rawMatrixBuffer, parallel);
                    else
                        ProcessSingleChannelEvents(channelsSnapshot, flatBuffer, parallel);

                    ReadCycleCompleted?.Invoke(this, sw.ElapsedMilliseconds);
                    await Task.Yield();
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                RaiseError("Loop", -1, 0, ex.Message);
            }
            finally
            {
                _isRunning = false;
                State = AnalogState.Stopped;
                if (!_stopRequested) TryRestart();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool PerformHardwareOperations(ushort[] channelList,
            ushort[] configList, float[] flatBuffer)
        {
            ushort rtn = UniDAQ.Ixud_StartAIScan(BoardNo, (ushort)channelList.Length,
                channelList, configList, SamplingRate, DataCount);
            if (rtn != UniDAQ.Ixud_NoErr)
            {
                RaiseError("StartAIScan", -1, rtn, "Failed start");
                return false;
            }

            rtn = UniDAQ.Ixud_GetAIBuffer(BoardNo,
                DataCount * (uint)channelList.Length, flatBuffer);
            if (rtn != UniDAQ.Ixud_NoErr)
            {
                RaiseError("GetAIBuffer", -1, rtn, "Failed get buffer");
                return false;
            }

            rtn = UniDAQ.Ixud_StopAI(BoardNo);
            if (rtn != UniDAQ.Ixud_NoErr)
            {
                RaiseError("StopAI", -1, rtn, "Failed stop");
                return false;
            }

            return true;
        }

        private void ProcessMultiChannel(AnalogInputChannel[] channels, float[] flatBuffer,
            float[,] matrix, float[,] rawMatrix, bool parallel)
        {
            int chCount = channels.Length;
            Action<int> body = i =>
            {
                for (int ch = 0; ch < chCount; ch++)
                {
                    float raw = flatBuffer[i * chCount + ch];
                    var channel = channels[ch];
                    float processed = channel.Filter != null ? channel.Filter.Next(raw) : raw;

                    rawMatrix[i, ch] = processed;
                    matrix[i, ch] = Fit(processed, channel.Regression) - channel.Zero;
                }
            };

            if (parallel) Parallel.For(0, (int)DataCount, body);
            else for (int i = 0; i < DataCount; i++) body(i);

            MultiChannelDataReceived?.Invoke(this, new AnalogMultiChannelDataEventArgs(channels, matrix, rawMatrix));
        }

        private void ProcessSingleChannelEvents(AnalogInputChannel[] channels, float[] flatBuffer, bool parallel)
        {
            int chCount = channels.Length;
            Action<int> body = ch =>
            {
                var channel = channels[ch];
                float[] processedBuf = ArrayPool<float>.Shared.Rent((int)DataCount);
                float[] rawBuf = ArrayPool<float>.Shared.Rent((int)DataCount);
                try
                {
                    for (int i = 0; i < DataCount; i++)
                    {
                        float raw = flatBuffer[i * chCount + ch];
                        float val = channel.Filter != null ? channel.Filter.Next(raw) : raw;
                        rawBuf[i] = val;
                        processedBuf[i] = Fit(val, channel.Regression) - channel.Zero;
                    }
                    float[] safeProcessed = new float[DataCount];
                    float[] safeRaw = new float[DataCount];
                    Array.Copy(processedBuf, safeProcessed, DataCount);
                    Array.Copy(rawBuf, safeRaw, DataCount);
                    DataReceived?.Invoke(this, new AnalogDataEventArgs(channel.Name, channel.Index, safeProcessed, safeRaw));
                }
                finally
                {
                    ArrayPool<float>.Shared.Return(processedBuf);
                    ArrayPool<float>.Shared.Return(rawBuf);
                }
            };

            if (parallel) Parallel.For(0, chCount, body);
            else for (int i = 0; i < chCount; i++) body(i);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float Fit(double value, double[] coeffs)
        {
            if (coeffs == null || coeffs.Length == 0) return (float)value;
            double result = coeffs[coeffs.Length - 1];
            for (int i = coeffs.Length - 2; i >= 0; i--)
                result = result * value + coeffs[i];
            return (float)result;
        }

        private void TryRestart()
        {
            if (_stopRequested) return;
            int tries = Interlocked.Increment(ref _retryCount);
            if (RetryLimit > 0 && tries > RetryLimit)
            {
                RaiseError("System", -1, 9999, $"Retry limit ({RetryLimit}) exceeded.");
                return;
            }
            Task.Delay(200).ContinueWith(_ => { if (!_stopRequested) Start(SamplingRate, DataCount); });
        }

        private void RaiseError(string source, int channelIdx, ushort code, string extraMsg)
        {
            string uniDaqMsg = UniDaqErrors.TryGetValue(code, out var msg) ? msg : "Unknown Error";
            string fullMsg = $"[{source}] {uniDaqMsg} - {extraMsg} (Retry: {_retryCount})";
            ErrorOccurred?.Invoke(this, new AnalogErrorEventArgs(source, channelIdx, code, fullMsg));
        }

        #endregion

        #region Dispose

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;
            _stopRequested = true;
            if (_isRunning) try { Stop(); } catch { }
            if (disposing)
            {
                _cts?.Dispose();
                _channelLock?.Dispose();
            }
            _disposed = true;
        }
        private bool _disposed;

        #endregion


        #region Static Error Codes

        private static readonly Dictionary<int, string> UniDaqErrors = new Dictionary<int, string>
        {
            { 0, "Correct" },
            { 1, "Open driver error" },
            { 2, "Plug & Play error" },
            { 3, "The driver was not open" },
            { 4, "Receive driver version error" },
            { 5, "Board number error" },
            { 6, "No board found" },
            { 7, "Board Mapping error" },
            { 8, "Digital input/output mode setting error" },
            { 9, "Invalid address" },
            { 10, "Invalid size" },
            { 11, "Invalid port number" },
            { 12, "This board model is not supported" },
            { 13, "This function is not supported" },
            { 14, "Invalid channel number" },
            { 15, "Invalid value" },
            { 16, "Invalid mode" },
            { 17, "Timeout while receiving analog input status" },
            { 18, "Timeout error" },
            { 19, "Configuration code table index not found" },
            { 20, "ADC controller timeout" },
            { 21, "PCI table index not found" },
            { 22, "Invalid setting value" },
            { 23, "Memory allocation error" },
            { 24, "Interrupt event installation error" },
            { 25, "Interrupt IRQ installation error" },
            { 26, "Interrupt IRQ removal error" },
            { 27, "Error clearing interrupt count" },
            { 28, "System buffer retrieval error" },
            { 29, "Event creation error" },
            { 30, "Resolution not supported" },
            { 31, "Thread creation error" },
            { 32, "Thread timeout error" },
            { 33, "FIFO overflow error" },
            { 34, "FIFO timeout error" },
            { 35, "Get interrupt installation status" },
            { 36, "Get system buffer status" },
            { 37, "Set buffer count error" },
            { 38, "Set buffer info error" },
            { 39, "Card ID not found" },
            { 40, "Event thread error" },
            { 41, "Auto-create event error" },
            { 42, "Register thread error" },
            { 43, "Search event error" },
            { 44, "FIFO reset error" },
            { 45, "Invalid EEPROM block" },
            { 46, "Invalid EEPROM address" },
            { 47, "Acquire spin lock error" },
            { 48, "Release spin lock error" },
            { 49, "Analog input setting error" },
            { 50, "Invalid channel number" },
            { 51, "Invalid model number" },
            { 52, "Map address setting error" },
            { 53, "Map address releasing error" },
            { 54, "Invalid memory offset" },
            { 55, "Shared memory open failed" },
            { 56, "Invalid data count" },
            { 57, "EEPROM writing error" },
            { 58, "CardIO error" },
            { 59, "MemoryIO error" },
            { 60, "Set scan channel error" },
            { 61, "Set scan config error" },
            { 62, "Get MMIO map status" }
        };

       

        #endregion
    }

    #region Enum & Helper Types

    /// <summary>
    /// Standard UniDAQ Voltage Ranges mapped to their Config Codes.
    /// Use this enum in AddChannel().
    /// </summary>
    public enum VoltageRange : ushort
    {
        [Description("Bipolar ±10V")] Bipolar_10V = 0,
        [Description("Bipolar ±20V")] Bipolar_20V = 23,
        [Description("Bipolar ±5V")] Bipolar_5V = 1,
        [Description("Bipolar ±2.5V")] Bipolar_2_5V = 2,
        [Description("Bipolar ±1.25V")] Bipolar_1_25V = 3,
        [Description("Bipolar ±0.625V")] Bipolar_0_625V = 4,
        [Description("Bipolar ±0.3125V")] Bipolar_0_3125V = 5,
        [Description("Bipolar ±0.5V")] Bipolar_0_5V = 6,
        [Description("Bipolar ±0.05V")] Bipolar_0_05V = 7,
        [Description("Bipolar ±0.005V")] Bipolar_0_005V = 8,
        [Description("Bipolar ±1V")] Bipolar_1V = 9,
        [Description("Bipolar ±0.1V")] Bipolar_0_1V = 10,
        [Description("Bipolar ±0.01V")] Bipolar_0_01V = 11,
        [Description("Bipolar ±0.001V")] Bipolar_0_001V = 12,

        [Description("Unipolar 0–20V")] Unipolar_20V = 13,
        [Description("Unipolar 0–10V")] Unipolar_10V = 14,
        [Description("Unipolar 0–5V")] Unipolar_5V = 15,
        [Description("Unipolar 0–2.5V")] Unipolar_2_5V = 16,
        [Description("Unipolar 0–1.25V")] Unipolar_1_25V = 17,
        [Description("Unipolar 0–0.625V")] Unipolar_0_625V = 18,
        [Description("Unipolar 0–1V")] Unipolar_1V = 19,
        [Description("Unipolar 0–0.1V")] Unipolar_0_1V = 20,
        [Description("Unipolar 0–0.01V")] Unipolar_0_01V = 21,
        [Description("Unipolar 0–0.001V")] Unipolar_0_001V = 22
    }

    public static class EnumExtensions
    {
        // Helper to get the Description string for UI (optional usage)
        public static string GetDescription(this Enum value)
        {
            FieldInfo field = value.GetType().GetField(value.ToString());
            DescriptionAttribute attribute = Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) as DescriptionAttribute;
            return attribute == null ? value.ToString() : attribute.Description;
        }
    }

    public enum AnalogState { Stopped = 0, Running = 1 }

    public class AnalogInputChannel
    {
        public string Name { get; }
        public int Index { get; }
        public ushort ConfigCode { get; }
        public double[] Regression { get; set; }
        public float Zero { get; set; }
        public SimpleMovingAverage Filter { get; private set; }

        public AnalogInputChannel(string name, int index, ushort configCode, double[] regression, float zero)
        {
            Name = name;
            Index = index;
            ConfigCode = configCode;
            Regression = regression;
            Zero = zero;
        }

        public void SetFilter(int windowSize) => Filter = new SimpleMovingAverage(windowSize);
    }

    public class SimpleMovingAverage
    {
        private readonly float[] _buffer;
        private int _ptr;
        private float _sum;
        private int _count;

        public SimpleMovingAverage(int windowSize)
        {
            if (windowSize < 1) throw new ArgumentException("Window size must be > 0");
            _buffer = new float[windowSize];
        }

        public float Next(float val)
        {
            _sum -= _buffer[_ptr];
            _sum += val;
            _buffer[_ptr] = val;
            _ptr = (_ptr + 1) % _buffer.Length;
            if (_count < _buffer.Length) _count++;
            return _sum / _count;
        }
    }

    public class AnalogDataEventArgs : EventArgs
    {
        public string ChannelName { get; }
        public int ChannelIndex { get; }
        public float[] Data { get; }
        public float[] RawData { get; }
        public AnalogDataEventArgs(string name, int idx, float[] data, float[] raw)
        {
            ChannelName = name; ChannelIndex = idx; Data = data; RawData = raw;
        }
    }

    public class AnalogMatrix
    {
        public float[] DataMatrix { get; set; }
        public float[] RawDataMatrix { get; set; }
    }

    public class AnalogMultiChannelDataEventArgs : EventArgs
    {
        public DateTime Timestamp { get; }
        public IReadOnlyList<AnalogInputChannel> Channels { get; }
        public float[,] DataMatrix { get; }
        public float[,] RawDataMatrix { get; }

        private readonly Dictionary<string, int> _channelNameIndexMap;

        public AnalogMultiChannelDataEventArgs(
            IReadOnlyList<AnalogInputChannel> channels,
            float[,] dataMatrix,
            float[,] rawDataMatrix)
        {
            Channels = channels ?? throw new ArgumentNullException(nameof(channels));
            DataMatrix = dataMatrix ?? throw new ArgumentNullException(nameof(dataMatrix));
            RawDataMatrix = rawDataMatrix ?? throw new ArgumentNullException(nameof(rawDataMatrix));
            Timestamp = DateTime.Now;

            _channelNameIndexMap = new Dictionary<string, int>(channels.Count, StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < channels.Count; i++)
            {
                _channelNameIndexMap[channels[i].Name] = i;
            }
        }

        public AnalogMatrix GetChannelData(int channelIndex)
        {
            if (channelIndex < 0 || channelIndex >= Channels.Count)
                throw new ArgumentOutOfRangeException(nameof(channelIndex));

            int dataCount = DataMatrix.GetLength(0);
            var matrix = new AnalogMatrix
            {
                DataMatrix = new float[dataCount],
                RawDataMatrix = new float[dataCount]
            };

            for (int i = 0; i < dataCount; i++)
            {
                matrix.DataMatrix[i] = DataMatrix[i, channelIndex];
                matrix.RawDataMatrix[i] = RawDataMatrix[i, channelIndex];
            }
            return matrix;
        }

        public AnalogMatrix GetChannelData(string channelName)
        {
            if (!_channelNameIndexMap.TryGetValue(channelName, out int index))
                throw new KeyNotFoundException($"Channel name '{channelName}' not found.");

            return GetChannelData(index);
        }

        public float GetValue(int sampleIndex, string channelName)
        {
            if (!_channelNameIndexMap.TryGetValue(channelName, out int index))
                throw new KeyNotFoundException($"Channel name '{channelName}' not found.");
            return DataMatrix[sampleIndex, index];
        }

        public float GetRawValue(int sampleIndex, string channelName)
        {
            if (!_channelNameIndexMap.TryGetValue(channelName, out int index))
                throw new KeyNotFoundException($"Channel name '{channelName}' not found.");
            return RawDataMatrix[sampleIndex, index];
        }

        public float GetValue(int sampleIndex, int channelIndex) => DataMatrix[sampleIndex, channelIndex];
        public float GetRawValue(int sampleIndex, int channelIndex) => RawDataMatrix[sampleIndex, channelIndex];
    }


    public class AnalogErrorEventArgs : EventArgs
    {
        public string Source { get; }
        public int ChannelIndex { get; }
        public int ErrorCode { get; }
        public string Message { get; }
        public AnalogErrorEventArgs(string src, int idx, int code, string msg)
        {
            Source = src; ChannelIndex = idx; ErrorCode = code; Message = msg;
        }
    }
    #endregion
}
