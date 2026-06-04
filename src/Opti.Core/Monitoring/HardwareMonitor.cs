using LibreHardwareMonitor.Hardware;

namespace Opti.Core.Monitoring;

/// <summary>
/// Lit les capteurs matériels (charge et température CPU/GPU, mémoire)
/// via LibreHardwareMonitor. Nécessite les droits administrateur pour
/// accéder à certains capteurs (ring0).
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

    /// <summary>Effectue une lecture synchrone de tous les capteurs.</summary>
    public SystemMetrics Read()
    {
        if (!_opened) Start();

        _computer.Accept(_visitor);

        float? cpuLoad = null, cpuTemp = null, gpuLoad = null, gpuTemp = null;
        float? memUsed = null, memAvail = null, memLoad = null;
        string? cpuName = null, gpuName = null;

        foreach (var hw in _computer.Hardware)
        {
            switch (hw.HardwareType)
            {
                case HardwareType.Cpu:
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
                    break;

                case HardwareType.GpuNvidia:
                case HardwareType.GpuAmd:
                case HardwareType.GpuIntel:
                    gpuName ??= hw.Name;
                    foreach (var s in hw.Sensors)
                    {
                        if (s.SensorType == SensorType.Load &&
                            (s.Name == "GPU Core" || s.Name.Contains("GPU")))
                            gpuLoad ??= s.Value;
                        else if (s.SensorType == SensorType.Temperature &&
                                 (s.Name.Contains("Core") || gpuTemp is null))
                            gpuTemp = s.Value;
                    }
                    break;

                case HardwareType.Memory:
                    foreach (var s in hw.Sensors)
                    {
                        if (s.SensorType == SensorType.Data && s.Name == "Memory Used")
                            memUsed = s.Value;
                        else if (s.SensorType == SensorType.Data && s.Name == "Memory Available")
                            memAvail = s.Value;
                        else if (s.SensorType == SensorType.Load && s.Name == "Memory")
                            memLoad = s.Value;
                    }
                    break;
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

    public void Dispose()
    {
        if (_opened)
        {
            _computer.Close();
            _opened = false;
        }
    }

    /// <summary>Force la mise à jour récursive des sous-composants.</summary>
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
