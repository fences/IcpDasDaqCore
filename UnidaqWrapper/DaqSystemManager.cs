using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UniDAQ_Ns; // Ensure UniDAQ.net.dll is referenced
using IcpDas.Daq.Analog; // Refers to the namespace of the previous code

namespace IcpDas.Daq.System
{
    #region EventArgs Models

    public class DaqErrorEventArgs : EventArgs
    {
        public string Source { get; }
        public ushort ErrorCode { get; }
        public string Message { get; }
        public DateTime Timestamp { get; }

        public DaqErrorEventArgs(string source, ushort code, string message)
        {
            Source = source;
            ErrorCode = code;
            Message = message;
            Timestamp = DateTime.UtcNow;
        }
    }

    public class BoardDetectedEventArgs : EventArgs
    {
        public ushort BoardIndex { get; }
        public string ModelName { get; }
        public int AnalogChannels { get; }
        public int DigitalChannels { get; }

        public BoardDetectedEventArgs(ushort index, string model, int aiCh, int dioCh)
        {
            BoardIndex = index;
            ModelName = model;
            AnalogChannels = aiCh;
            DigitalChannels = dioCh;
        }
    }

    #endregion

    /// <summary>
    /// Central manager for the UniDAQ Driver.
    /// Handles driver initialization, board detection, and creation of sub-controllers (Analog/Digital).
    /// </summary>
    public sealed class DaqSystemManager : IDisposable
    {
        #region Static Driver Management
        // Ensures the underlying C++ driver is initialized only once globally.
        private static readonly SemaphoreSlim _driverLock = new SemaphoreSlim(1, 1);
        private static int _driverRefCount = 0;
        private static bool _isDriverInitialized = false;
        #endregion

        #region Fields & Properties
        private bool _disposed = false;
        private readonly UniDAQ.IXUD_CARD_INFO[] _sCardInfo = new UniDAQ.IXUD_CARD_INFO[UniDAQ.MAX_BOARD_NUMBER];
        private readonly UniDAQ.IXUD_DEVICE_INFO[] _sDeviceInfo = new UniDAQ.IXUD_DEVICE_INFO[UniDAQ.MAX_BOARD_NUMBER];

        // Sub-Managers
        public AnalogInputManager Analog { get; private set; }

        // Placeholder for Digital Manager (Assuming you have a similar class for Digital IO)
        // public DigitalIOManager Digital { get; private set; } 

        public bool IsDriverInitialized => _isDriverInitialized;
        public int AnalogRetryLimit { get; set; } = 5;
        #endregion

        #region Events
        public event EventHandler<DaqErrorEventArgs> ErrorOccurred;
        public event EventHandler<BoardDetectedEventArgs> BoardDetected;
        public event EventHandler<string> StatusMessage;
        #endregion

        #region Constructor
        public DaqSystemManager()
        {
            
        }
        #endregion

        #region Driver & Hardware Initialization

        /// <summary>
        /// Initializes the UniDAQ driver and scans for connected boards.
        /// </summary>
        public async Task<bool> InitializeDriverAsync(CancellationToken cancellationToken = default)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(DaqSystemManager));

            await _driverLock.WaitAsync(cancellationToken);
            try
            {
                if (!_isDriverInitialized)
                {
                    ushort totalBoards = 0;
                    ushort rtn = UniDAQ.Ixud_DriverInit(ref totalBoards);

                    if (rtn != UniDAQ.Ixud_NoErr)
                    {
                        RaiseError("DriverInit", rtn, "Failed to initialize UniDAQ driver.");
                        return false;
                    }

                    _isDriverInitialized = true;
                    StatusMessage?.Invoke(this, $"Driver Initialized. Total Boards Found: {totalBoards}");

                    // Scan connected boards
                    for (ushort wBoardIndex = 0; wBoardIndex < totalBoards; wBoardIndex++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        DetectBoard(wBoardIndex);
                    }
                }

                _driverRefCount++;
                return true;
            }
            catch (Exception ex)
            {
                RaiseError("DriverInit", 0, $"Exception: {ex.Message}");
                return false;
            }
            finally
            {
                _driverLock.Release();
            }
        }

        private void DetectBoard(ushort boardIndex)
        {
            byte[] szModeName = new byte[32]; // 20 is risky, 32 is safer
            ushort rtn = UniDAQ.Ixud_GetCardInfo(boardIndex, ref _sDeviceInfo[boardIndex], ref _sCardInfo[boardIndex], szModeName);

            if (rtn != UniDAQ.Ixud_NoErr)
            {
                RaiseError("GetCardInfo", rtn, $"Failed to read info for Board {boardIndex}");
                return;
            }

            string model = Encoding.Default.GetString(szModeName).TrimEnd('\0');

            // Notify listeners (UI or Logic) that a board is found
            BoardDetected?.Invoke(this, new BoardDetectedEventArgs(
                boardIndex,
                model,
                _sCardInfo[boardIndex].wAIChannels,
                _sCardInfo[boardIndex].wDIPorts + _sCardInfo[boardIndex].wDIOPorts
            ));
        }

        #endregion

        #region Controller Factory Methods

        /// <summary>
        /// Creates and configures the Analog Input Manager for a specific board.
        /// </summary>
        /// <param name="boardNo">The board index.</param>
        /// <param name="isHighGain">Set true if card is High Gain model.</param>
        public void CreateAnalogController(ushort boardNo, bool isHighGain = false)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(DaqSystemManager));

            if (Analog != null)
            {
                // If specifically managing one board per manager, warn.
                RaiseError("InitAnalog", 0, "Analog manager already exists in this context.");
                return;
            }

            try
            {
                Analog = new AnalogInputManager(boardNo, isHighGain)
                {
                    RetryLimit = AnalogRetryLimit
                };

                // Wire up events from the sub-manager to the main manager
                Analog.InitializationCompleted += (s, msg) => StatusMessage?.Invoke(this, $"[Analog] {msg}");
                Analog.ErrorOccurred += (s, e) => RaiseError($"Analog-Ch{e.ChannelIndex}", (ushort)e.ErrorCode, e.Message);

                StatusMessage?.Invoke(this, $"Analog Controller created for Board {boardNo}.");
            }
            catch (Exception ex)
            {
                RaiseError("InitAnalog", 0, $"Failed to create analog manager: {ex.Message}");
            }
        }

        /*
        // Example for Digital (Uncomment when DigitalIOManager is ready)
        public void CreateDigitalController(ushort boardNo)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(DaqSystemManager));
            
            try
            {
                // Digital = new DigitalIOManager(boardNo);
                // Digital.ErrorOccurred += (s, e) => RaiseError("Digital", e.ErrorCode, e.Message);
            }
            catch (Exception ex)
            {
                RaiseError("InitDigital", 0, ex.Message);
            }
        }
        */

        #endregion

        #region Disposal

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                // 1. Dispose Sub-Controllers
                Analog?.Dispose();
                // Digital?.Dispose();

                // 2. Decrement Driver Reference
                _driverLock.Wait();
                try
                {
                    _driverRefCount--;
                    if (_driverRefCount <= 0 && _isDriverInitialized)
                    {
                        // Only close the physical driver if no other managers are using it
                        try
                        {
                            UniDAQ.Ixud_DriverClose();
                            StatusMessage?.Invoke(this, "UniDAQ Driver Closed.");
                        }
                        catch (Exception ex)
                        {
                            RaiseError("Dispose", 0, $"Driver close error: {ex.Message}");
                        }
                        _isDriverInitialized = false;
                        _driverRefCount = 0; // Safety clamp
                    }
                }
                finally
                {
                    _driverLock.Release();
                }
            }

            _disposed = true;
        }

        #endregion

        #region Helpers

        private void RaiseError(string source, ushort code, string message)
        {
            ErrorOccurred?.Invoke(this, new DaqErrorEventArgs(source, code, message));
        }

        #endregion
    }
}
