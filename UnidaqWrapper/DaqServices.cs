using System;
using System.Threading;
using System.Threading.Tasks;
using IcpDas.Daq.System;
using IcpDas.Daq.Analog;

namespace IcpDas.Daq.Service
{
    /// <summary>
    /// Central Service Singleton.
    /// Provides global access to the DAQ System and Shared Data from anywhere in the app.
    /// Usage: DaqServices.Instance.StartAcquisition();
    /// </summary>
    public sealed class DaqServices : IDisposable
    {
        #region Singleton Implementation

        // Lazy initialization ensures thread safety and that the instance is created only when needed.
        private static readonly Lazy<DaqServices> _lazyInstance =
            new Lazy<DaqServices>(() => new DaqServices());

        public static DaqServices Instance => _lazyInstance.Value;

        #endregion

        #region Public Events (Global Logging)

        public event EventHandler<string> LogInfo;
        public event EventHandler<string> LogError;

        #endregion

        #region Data Caches (Global Access Points)

        /// <summary>
        /// Holds the latest Analog Data.
        /// UI Forms can subscribe to DaqServices.Instance.AnalogData.DataUpdated
        /// </summary>
        public SharedDataCache<AnalogMultiChannelDataEventArgs> AnalogData { get; }
            = new SharedDataCache<AnalogMultiChannelDataEventArgs>();

        #endregion

        #region Core Properties

        public DaqSystemManager SystemManager { get; private set; }

        /// <summary>
        /// Shortcut to access the Analog Controller (Start/Stop/Config).
        /// Returns null if board is not yet detected.
        /// </summary>
        public AnalogInputManager Analog => SystemManager?.Analog;

        public bool IsInitialized { get; private set; } = false;

        #endregion

        #region Constructor & Initialization

        private DaqServices()
        {
            SystemManager = new DaqSystemManager();
        }

        /// <summary>
        /// Initializes the driver and wires up the events globally.
        /// Call this once at Program.cs or MainForm Load.
        /// </summary>
        public async Task InitializeSystemAsync(CancellationToken token = default)
        {
            if (IsInitialized) return;

            try
            {
                // Wire up System Manager events
                SystemManager.StatusMessage += (s, msg) => RaiseLogInfo($"[System] {msg}");
                SystemManager.ErrorOccurred += (s, e) => RaiseLogError($"[System Error] {e.Message}");
                SystemManager.BoardDetected += OnBoardDetected;

                // Initialize Driver
                bool success = await SystemManager.InitializeDriverAsync(token);

                if (success)
                {
                    RaiseLogInfo("DAQ System Initialized Successfully.");
                    IsInitialized = true;
                }
                else
                {
                    RaiseLogError("DAQ System Initialization Failed.");
                }
            }
            catch (Exception ex)
            {
                RaiseLogError($"Critical Initialization Error: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region Event Wiring (The Magic Glue)

        private void OnBoardDetected(object sender, BoardDetectedEventArgs e)
        {
            RaiseLogInfo($"Board Found: {e.ModelName} (Index: {e.BoardIndex})");

            // Automatically create Analog Controller if the board supports AI
            if (e.AnalogChannels > 0)
            {
                SystemManager.CreateAnalogController(e.BoardIndex, isHighGain: false);

                if (Analog != null)
                {
                    // === CRITICAL: Connect the Analog Manager to the Shared Cache ===
                    Analog.MultiChannelDataReceived += (s, data) => AnalogData.Update(data);

                    // Wire up logging
                    Analog.InitializationCompleted += (s, msg) => RaiseLogInfo($"[Analog] {msg}");
                    Analog.ErrorOccurred += (s, err) => RaiseLogError($"[Analog Error] {err.Message}");

                    RaiseLogInfo($"Analog Controller Ready for Board {e.BoardIndex}");
                }
            }
        }

        #endregion

        #region Helper Methods

        private void RaiseLogInfo(string message) => LogInfo?.Invoke(this, message);
        private void RaiseLogError(string message) => LogError?.Invoke(this, message);

        #endregion

        #region Disposal

        public void Dispose()
        {
            if (SystemManager != null)
            {
                SystemManager.BoardDetected -= OnBoardDetected;
                if (Analog != null)
                {
                    // Remove event listeners to prevent memory leaks
                    Analog.MultiChannelDataReceived -= (s, data) => AnalogData.Update(data);
                }
                SystemManager.Dispose();
            }
            IsInitialized = false;
            RaiseLogInfo("Service Disposed.");
        }

        #endregion
    }
}
