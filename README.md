# ICP DAS DAQ Core Framework

### High-Level, Production-Grade Analog Input Engine for ICP DAS UniDAQ Devices

**Global Singleton Architecture — Multi-Channel, Filtered, Regression-Based Analog Acquisition**

---

## 🚀 Overview

**ICP DAS DAQ Core Framework** is a high-performance, industrial-grade .NET framework designed to simplify analog data acquisition from ICP DAS boards (via **UniDAQ.dll**).

This framework wraps the low-level ICP DAS driver with a **clean, safe, global, synchronized abstraction layer** that works flawlessly across:

* WinForms / WPF
* Windows Services
* Industrial data logging
* Monitoring dashboards
* Real-time measurement pipelines

A key architectural choice is the **Global Singleton Access Point (`DaqServices`)**:

* No redundant objects
* No hardware conflicts
* No duplicate initialization
* All classes, forms, and UI components share the same DAQ engine

---

## ⭐ Key Features

### ✔ Global Singleton Architecture

A single access point:

```csharp
DaqServices.Instance
```

Gives your entire application unified access to:

* AnalogInputManager
* SharedDataCache
* DaqSystemManager
* Logging
* Driver state
* Multi-channel data events

---

### ✔ High-Performance Analog Input Engine

`AnalogInputManager` provides:

* Multi-channel scanning
* Moving-average filtering
* Polynomial regression (any order)
* Noise reduction
* Zero offset adjustment
* Parallel processing
* Matrix-based output (`float[sample, channel]`)
* Auto-retry with safe restart
* Per-channel or multi-channel event modes
* Horner-based polynomial evaluation (fast)

---

### ✔ Shared Data Cache

`SharedDataCache<T>` (via `DaqServices.AnalogData`) gives:

* The **latest multi-channel block**
* Thread-safe access
* Global event `DataUpdated`
* Zero redundant subscriptions
* Zero memory duplication

---

### ✔ Automatic Device Detection

`DaqSystemManager` handles:

* Driver initialization (once only — safe with SemaphoreSlim)
* Board scanning
* Model detection
* Auto-creation of `AnalogInputManager`
* Event bridging to `DaqServices`

---

### ✔ Industrial Reliability

* Auto-restart on failures
* Retry limit protection
* Cancellation-safe loops
* Deadlock-free design
* Hardware-safe initialization
* Shared driver instance (ref-counting)

---

# 🧩 Architecture

```
ICP DAS DAQ Core Framework
│
├── DaqServices (Singleton)
│     ├── Global Logging
│     ├── SharedDataCache<AnalogMultiChannelDataEventArgs>
│     └── DaqSystemManager
│
├── DaqSystemManager
│     ├── UniDAQ Driver Initialization (global)
│     ├── Board Detection
│     └── Analog Controller Factory
│
└── AnalogInputManager
      ├── Channel Configuration
      ├── Moving Avg Filter
      ├── Polynomial Regression
      ├── Multi-Channel Engine
      ├── Error & Retry Pipeline
      └── AI Scan Loop
```

---

# 📁 File Structure

```
/src
 ├── AnalogInputManager.cs        // Multi-channel analog engine
 ├── DaqServices.cs               // App-wide singleton & glue layer
 ├── DaqSystemManager.cs          // UniDAQ driver / board detection
 └── SharedDataCache.cs           // Lightweight global data cache
```

---


# 📘 Usage Example (Main Form Initialization)

```csharp
private async void MainForm_Load(object sender, EventArgs e)
{
    rtbStatus.Multiline = true;

    DaqServices.Instance.LogInfo += (s, msg) =>
        rtbStatus.AppendText(msg + "\r\n");

    await DaqServices.Instance.InitializeSystemAsync();

    if (DaqServices.Instance.Analog != null)
    {
        var analog = DaqServices.Instance.Analog;

        analog.CardType = 0;
        analog.UseMultiChannelOutput = true;
        analog.UseParallel = false;
        analog.RetryLimit = 5;

        double[] reg1 = { 10.0, 0.5 };
        double[] reg2 = { 1.0, 0.5, 0.3, 0.2, 0.1 };
        int window = 5000;

        analog.AddChannel("Sensor1", 0, VoltageRange.Bipolar_10V, window, reg1);
        analog.AddChannel("Sensor2", 1, VoltageRange.Bipolar_10V, window, reg2);
        analog.AddChannel("Sensor3", 3, VoltageRange.Bipolar_10V);
        analog.AddChannel("Sensor4", 5, VoltageRange.Bipolar_10V);
        analog.AddChannel("Sensor5", 6, VoltageRange.Bipolar_10V);
        analog.AddChannel("Sensor6", 7, VoltageRange.Bipolar_10V);
    }
}
```

---

# ▶ Start Acquisition

```csharp
DaqServices.Instance.Analog.Start(
    samplingRate: 10000,
    dataCount: 100
);
```

---

# 📊 Multi-Form Data Access Example

Every form can subscribe globally:

```csharp
DaqServices.Instance.AnalogData.DataUpdated += OnAnalogDataUpdated;
```

Thread-safe update:

```csharp
private void OnAnalogDataUpdated(object s, AnalogMultiChannelDataEventArgs e)
{
    if (InvokeRequired)
        BeginInvoke(new Action(() => UpdateChartData(e)));
    else
        UpdateChartData(e);
}
```

Matrix-based chart update:

```csharp
private void UpdateChartData(AnalogMultiChannelDataEventArgs data)
{
    daqChart.SuspendLayout();

    for (int i = 0; i < data.Channels.Count; i++)
    {
        string name = data.Channels[i].Name;

        float sum = 0;
        int count = data.DataMatrix.GetLength(0);

        for (int k = 0; k < count; k++)
            sum += data.RawDataMatrix[k, i];

        float avg = sum / count;

        daqChart.Series[name].Points.AddY(avg);

        if (daqChart.Series[name].Points.Count > MAX_HISTORY)
            daqChart.Series[name].Points.RemoveAt(0);
    }

    daqChart.ResumeLayout();
}
```

---

# 🔧 API Reference (Condensed)

## DaqServices (Singleton)

```csharp
public static DaqServices Instance { get; }
public AnalogInputManager Analog { get; }
public SharedDataCache<AnalogMultiChannelDataEventArgs> AnalogData { get; }
public Task InitializeSystemAsync();
public event EventHandler<string> LogInfo;
public event EventHandler<string> LogError;
```

---

## DaqSystemManager

```csharp
public Task<bool> InitializeDriverAsync();
public void CreateAnalogController(ushort boardNo, bool isHighGain);
public event EventHandler<BoardDetectedEventArgs> BoardDetected;
public event EventHandler<DaqErrorEventArgs> ErrorOccurred;
```

---

## AnalogInputManager

```csharp
public void AddChannel(string name, int index, VoltageRange range,
                       int movingAvg = 0, double[] regression = null, float zero = 0);

public void Start(float samplingRate, uint dataCount);
public void Stop();

public event EventHandler<AnalogMultiChannelDataEventArgs> MultiChannelDataReceived;
public event EventHandler<AnalogDataEventArgs> DataReceived;
public event EventHandler<AnalogErrorEventArgs> ErrorOccurred;
```

---

# ⚠ Error Handling

Errors propagate via:

### • `AnalogErrorEventArgs`

### • `DaqErrorEventArgs`

### • `RetryLimit` + auto-restart pipeline

UniDAQ error codes are mapped internally:

```csharp
{0: "No Error", 1: "Open Driver Error", 6: "No Board Found", ...}
```

---

# 🏭 Use Cases

* Industrial monitoring
* Sensor fusion
* High-speed multi-channel acquisition
* Vibration analysis
* Power/energy measurement
* Automated test stands
* IoT gateways

---

# 📄 License

Recommended: **Apache License 2.0** (safe for open-source & commercial use)
