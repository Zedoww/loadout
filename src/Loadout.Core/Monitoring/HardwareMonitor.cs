using LibreHardwareMonitor.Hardware;

namespace Loadout.Core.Monitoring;

/// <summary>
/// Reads hardware sensors (CPU/GPU load and temperature, memory) through
/// LibreHardwareMonitor. Administrator rights are required to access some
/// sensors (ring0 driver).
/// </summary>
public sealed class HardwareMonitor : IDisposable
{
    private readonly Computer _computer;
    private readonly UpdateVisitor _visitor = new();
    private bool _opened;

    public HardwareMonitor()
    {
        _computer = new Computer
        {
            IsCpuEnabled = true,
            IsGpuEnabled = true,
            IsMemoryEnabled = true,
            IsMotherboardEnabled = false,
            IsControllerEnabled = false,
            IsNetworkEnabled = false,
            IsStorageEnabled = false,
        };
    }

    public void Start()
    {
        if (_opened) return;
        _computer.Open();
        _opened = true;
    }

    /// <summary>Performs a synchronous read of every sensor.</summary>
    public SystemMetrics Read()
    {
        if (!_opened) Start();

        _computer.Accept(_visitor);

        float? cpuLoad = null, cpuTemp = null, gpuLoad = null, gpuTemp = null;
        float? memUsed = null, memAvail = null, memLoad = null;
        string? cpuName = null, gpuName = null;

        // Pick the primary GPU first: a machine can expose both an integrated
        // GPU (AMD APU / Intel iGPU) and a discrete card (e.g. an NVIDIA RTX).
        // We must report the discrete one, not whichever happens to be first.
        IHardware? primaryGpu = SelectPrimaryGpu();

        foreach (var hw in _computer.Hardware)
        {
            if (hw.HardwareType == HardwareType.Cpu)
            {
                cpuName ??= hw.Name;
                foreach (var s in hw.Sensors)
                {
                    if (s.SensorType == SensorType.Load && s.Name == "CPU Total")
                        cpuLoad = s.Value;
                    else if (s.SensorType == SensorType.Temperature &&
                             (s.Name.Contains("Package") || s.Name.Contains("Tctl") ||
                              s.Name.Contains("Core (Tctl/Tdie)") || cpuTemp is null))
                        cpuTemp = s.Value;
                }
            }
            else if (ReferenceEquals(hw, primaryGpu))
            {
                gpuName = hw.Name;
                foreach (var s in hw.Sensors)
                {
                    if (s.SensorType == SensorType.Load &&
                        (s.Name == "GPU Core" || s.Name.Contains("GPU")))
                        gpuLoad ??= s.Value;
                    else if (s.SensorType == SensorType.Temperature &&
                             (s.Name.Contains("Core") || gpuTemp is null))
                        gpuTemp = s.Value;
                }
            }
            else if (hw.HardwareType == HardwareType.Memory)
            {
                foreach (var s in hw.Sensors)
                {
                    if (s.SensorType == SensorType.Data && s.Name == "Memory Used")
                        memUsed = s.Value;
                    else if (s.SensorType == SensorType.Data && s.Name == "Memory Available")
                        memAvail = s.Value;
                    else if (s.SensorType == SensorType.Load && s.Name == "Memory")
                        memLoad = s.Value;
                }
            }
        }

        float? memTotal = memUsed is not null && memAvail is not null
            ? memUsed + memAvail
            : null;

        return new SystemMetrics
        {
            CpuLoad = cpuLoad,
            CpuTemperature = cpuTemp,
            CpuName = cpuName,
            GpuLoad = gpuLoad,
            GpuTemperature = gpuTemp,
            GpuName = gpuName,
            MemoryUsedGb = memUsed,
            MemoryTotalGb = memTotal,
            MemoryLoad = memLoad,
        };
    }

    /// <summary>
    /// Selects the most relevant GPU. Discrete cards (NVIDIA, then AMD) win over
    /// integrated graphics (Intel); ties are broken by current core load so the
    /// card actually doing the work is reported.
    /// </summary>
    private IHardware? SelectPrimaryGpu()
    {
        IHardware? best = null;
        int bestScore = int.MinValue;

        foreach (var hw in _computer.Hardware)
        {
            int priority = hw.HardwareType switch
            {
                HardwareType.GpuNvidia => 3,
                HardwareType.GpuAmd => 2,
                HardwareType.GpuIntel => 1,
                _ => -1,
            };
            if (priority < 0) continue;

            float load = 0;
            foreach (var s in hw.Sensors)
                if (s.SensorType == SensorType.Load && s.Name == "GPU Core")
                    load = s.Value ?? 0;

            int score = priority * 1000 + (int)load;
            if (score > bestScore)
            {
                bestScore = score;
                best = hw;
            }
        }

        return best;
    }

    public void Dispose()
    {
        if (_opened)
        {
            _computer.Close();
            _opened = false;
        }
    }

    /// <summary>Forces a recursive update of every sub-component.</summary>
    private sealed class UpdateVisitor : IVisitor
    {
        public void VisitComputer(IComputer computer) => computer.Traverse(this);
        public void VisitHardware(IHardware hardware)
        {
            hardware.Update();
            foreach (var sub in hardware.SubHardware) sub.Accept(this);
        }
        public void VisitSensor(ISensor sensor) { }
        public void VisitParameter(IParameter parameter) { }
    }
}
