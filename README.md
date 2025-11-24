# IcpDasDaqCore
### High-Level Analog Input Framework for ICP DAS UniDAQ Devices
**Global Access Architecture — True Singleton, Zero Conflicts**

```
┌──────────────────────────────────────────────┐
│   ICP DAS DAQ – High-Level Input Framework   │
│        Clean • Safe • Global • Reliable      │
└──────────────────────────────────────────────┘
```

---

## 🚀 Overview

**IcpDasDaqCore** is a high-level, production-grade framework designed for analog input acquisition using ICP DAS DAQ devices through **UniDAQ.dll**.

The purpose of this project is to provide a **clean**, **safe**, and **powerful abstraction layer** that works seamlessly across:

* WinForms
* WPF
* Background Services
* Industrial real-time systems

A key architectural highlight is the **Global Access Singleton Design** (`DaqServices`), ensuring:

* No duplicate initialization
* No hardware conflicts
* No additional DAQ objects ever created
* Full data consistency across the entire application

---

## ⭐ Key Features

### 1️⃣ Global Access Architecture (`DaqServices`)

The entire DAQ system is exposed through a *single* global instance:

```csharp
DaqServices.Instance
```

This singleton provides synchronized access to:

* `AnalogInputManager`
* `SharedDataCache`
* `DaqSystemManager`
* System state
* Events
* Last known data

No extra objects. No re-initialization. No risks.

---

### 2️⃣ High-Performance Analog Input Manager

`AnalogInputManager` provides:

* Per-channel configuration (range, offset, regression)
* Moving-average digital filtering
* Polynomial regression (y = a0 + a1x + a2x² + …)
* Parallel data processing
* Matrix-based multi-channel output
* Auto-restart logic
* Built-in error handling
* Per-sample / per-channel helper access

---

### 3️⃣ Multi-Channel Matrix Output

All processed and raw data is delivered as:

```
DataMatrix[sample, channel]
RawDataMatrix[sample, channel]
```

Examples:

```csharp
var ch = e.GetChannelData("Sensor1"); // processed + raw data
float value = e.GetValue(0, "Sensor2");
```

---

### 4️⃣ Shared Data Cache

`SharedDataCache` provides:

* The latest multi-channel data block
* Thread-safe access
* Zero duplication of memory
* A global `DataUpdated` event
* No need to subscribe to AnalogInputManager everywhere

---

### 5️⃣ Automatic Device Detection

`DaqSystemManager` handles:

* Safe UniDAQ initialization
* Hardware detection
* Controlled, conflict-free initialization using `SemaphoreSlim`
* Driver state events
* Error events
* Creation of `AnalogInputManager` only when a board exists

---

### 6️⃣ Industrial-Grade Reliability

Built with continuous operation in mind:

* Retry mechanisms
* Auto-restart capabilities
* Graceful shutdown
* No deadlocks
* Safe cancellation tokens
* Parallel pipeline optimization

---

## 🧩 Project Architecture

```
IcpDasDaqCore
│
├─ DaqServices          // Global singleton – main entry point
│
├─ DaqSystemManager     // UniDAQ device detection & driver initialization
│
├─ AnalogInputManager   // Channels, filtering, regression, engine
│
└─ SharedDataCache      // Latest data block shared across app
```

This layered architecture ensures safety, modularity, and clean separation.

---

## 📘 Usage Example

### 1️⃣ System Initialization

```csharp
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
```

---

### 2️⃣ Start Acquisition

```csharp
DaqServices.Instance.Analog.Start(
    samplingRate: 10000,
    dataCount: 100        // samples per block
);
```

---

### 3️⃣ Receive Multi-Channel Data

```csharp
DaqServices.Instance.Analog.MultiChannelDataReceived += (s, e) =>
{
    // processed block
    var s1 = e.GetChannelData("Sensor1").DataMatrix;

    // single extracted value
    float v = e.GetValue(0, "Sensor1");

    // full multi-channel matrix
    float[,] matrix = e.DataMatrix;
};
```

---

### 4️⃣ Global Access from Any Form / Class

```csharp
private void ChartForm_Load(object sender, EventArgs e)
{
    DaqServices.Instance.AnalogData.DataUpdated += OnData;
}

private void OnData(object sender, AnalogMultiChannelDataEventArgs e)
{
    var ch = e.GetChannelData("Sensor1");
    float latest = ch.DataMatrix.Last();
    chart.AddPoint(latest);
}
```

---

### 5️⃣ Graceful Shutdown

```csharp
private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
{
    DaqServices.Instance.Dispose();
}
```

---

## 🏆 Advantages of Global Singleton Architecture

* Zero redundant objects
* Zero double initialization
* Zero configuration duplication
* Hardware conflict-free
* Consistent data across app
* Thread-synchronized
* Perfect for large industrial applications
* UI and logic fully decoupled

Especially suitable for DAQ systems where duplication can create hardware contention.

---

## 🏭 Use Cases

* Industrial monitoring
* Data logging systems
* Vibration analysis
* Power / energy measurement
* Temperature / pressure systems
* CNC / servo motion monitoring
* Automated test platforms
* IoT industrial gateways

---

## 📄 License

**Recommended:** Apache License 2.0
(Attribution protection + business-friendly)

---

