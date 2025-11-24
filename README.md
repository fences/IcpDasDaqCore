#IcpDasDaqCore
High‑Level Analog Input Framework for ICP DAS UniDAQ Devices
with Global Access Architecture (Single Instance / No Extra Objects)
Overview
IcpDasDaqCore is a high‑level, production‑grade framework designed for analog input acquisition using ICP DAS DAQ cards via UniDAQ.dll.

The goal of this project is to provide a clean, safe, and powerful abstraction layer over UniDAQ that can be used easily across any application (WinForms, WPF, Services, and industrial systems).

A central architectural highlight of this framework is the global access design based on a true Singleton service: DaqServices.

This ensures that:

All forms, classes, UI components, and background workers
All real‑time monitoring windows
All analysis modules
can access the same active DAQ system without creating any additional objects.

This guarantees:

No duplicate initialization
No hardware conflicts
No memory leaks
Complete consistency across the entire software
Key Features
1. Global Access Architecture (DaqServices)
The entire DAQ system is accessible through:


content_copy
csharp
DaqServices.Instance
This single instance provides shared, synchronized, and safe access to:

AnalogInputManager
SharedDataCache
DaqSystemManager
System state
Events
Last known data
You never create additional instances.

You never reconfigure channels twice.

Everything is globally available everywhere.

2. High‑Performance Analog Input Manager
AnalogInputManager provides:

Channel configuration (range, regression, zero offset)
Moving‑Average filter per channel
Polynomial regression (y = a0 + a1x + a2x² + …)
Parallel processing for large data sets
Matrix‑based multi‑channel output
Automatic error handling and restart logic
Per‑sample and per‑channel access helpers
3. Multi‑Channel Matrix Output
All data from all channels is delivered as matrices:

DataMatrix[sample, channel]
RawDataMatrix[sample, channel]
You can easily extract:


content_copy
csharp
var ch = e.GetChannelData("Sensor1");     // Processed + Raw
float v = e.GetValue(0, "Sensor2");       // Single value
4. Shared Data Cache
SharedDataCache stores the latest multi‑channel data block and makes it immediately available to the entire application:

Thread‑safe
Instant access
Broadcasts DataUpdated event
Zero additional memory overhead
No need to subscribe to AnalogManager everywhere
5. Automatic Device Detection
DaqSystemManager:

Initializes UniDAQ safely
Detects installed ICP DAS cards
Creates AnalogInputManager only when hardware exists
Sends status and error events
Prevents double initialization (SemaphoreSlim controlled)
6. Industrial‑Grade Reliability
Includes:

Retry logic
Auto restart
Graceful stop
No deadlocks
Safe cancellation tokens
Parallel pipeline optimization
Project Architecture
IcpDasDaqCore

│

├─ DaqServices // Main global access point (Singleton)

│

├─ DaqSystemManager // Driver initialization, board detection

│

├─ AnalogInputManager // Channel config, filtering, regression, data engine

│

└─ SharedDataCache // Last data block, shared across entire app

This layered architecture ensures safety, modularity, and clean decoupling.

Usage Example
Below is a complete example covering:

System initialization
Adding channels
Starting acquisition
Receiving multi‑channel data
Access from any other form/class
1. Initialize System (e.g., MainForm Load)

content_copy
csharp
private async void MainForm_Load(object sender, EventArgs e)
{
    await DaqServices.Instance.InitializeSystemAsync();

    // Add channels
    DaqServices.Instance.Analog.AddChannel(
        "Sensor1", 0,
        VoltageRange.Bipolar_10V,
        movingAverageWindow: 5000,
        regressionCoeffs: new[] { 10.0, 0.5 },
        zeroOffset: 0
    );

    DaqServices.Instance.Analog.AddChannel(
        "Sensor2", 1,
        VoltageRange.Bipolar_5V,
        movingAverageWindow: 1000
    );
}
2. Start Acquisition

content_copy
csharp
DaqServices.Instance.Analog.Start(
    samplingRate: 10000,
    dataCount: 100          // Samples per block
);
3. Receive Multi‑Channel Data

content_copy
csharp
DaqServices.Instance.Analog.MultiChannelDataReceived += (s, e) =>
{
    // Example: get processed data of Sensor1
    var s1 = e.GetChannelData("Sensor1").DataMatrix;

    // Example: get single value
    float v = e.GetValue(0, "Sensor1");

    // Example: work with whole matrix
    float[,] m = e.DataMatrix;
};
4. Global Access in Any Other Form or Class
No references, no new objects — just use the singleton.


content_copy
csharp
private void ChartForm_Load(object sender, EventArgs e)
{
    DaqServices.Instance.AnalogData.DataUpdated += OnData;
}

private void OnData(object sender, AnalogMultiChannelDataEventArgs e)
{
    var ch = e.GetChannelData("Sensor1");
    float latest = ch.DataMatrix.Last();
    chart1.AddPoint(latest);
}
5. Graceful Shutdown

content_copy
csharp
private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
{
    DaqServices.Instance.Dispose();
}
Advantages of the Global Singleton Architecture
Zero redundant objects
Zero repeated initialization
Zero configuration duplication
Data is always consistent
Safe and thread‑synchronised
Clean codebase for large‑scale industrial apps
Highly suitable for multi‑form WinForms / WPF projects
UI and Logic remain completely decoupled
This architecture is specifically tailored for DAQ systems where duplication can cause hardware conflicts.

Use Cases
Industrial monitoring
Data logging
Machine vibration analysis
Power measurement
Temperature/pressure sensor systems
CNC and motion monitoring
Automated test systems
IoT industrial gateways
License
Recommended: Apache License 2.0

(Preserves your name and includes attribution + strong legal protection)

Need a Logo, NuGet packaging, or Extended Documentation?
Tell me and I will generate:

A SVG/PNG logo
A full PDF documentation
NuGet publish description
UML diagrams (class + sequence)
GIF demo for GitHub
If you’d like, I can now generate:

✅ Full English + Persian dual‑language README

✅ Badge set (Build, License, .NET version)

✅ Professional GitHub description section

Just tell me.
