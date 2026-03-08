using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using SystemStats.Contracts;

namespace SystemStats.Core;

public interface ISystemStatsProvider
{
    SystemStatsSnapshot GetCurrentStats();
}

public class SystemStatsProvider : ISystemStatsProvider
{
    private readonly PerformanceCounter _totalCpuCounter;
    private readonly IReadOnlyList<PerformanceCounter> _perCoreCpuCounters;

    public SystemStatsProvider()
    {
        _totalCpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");

        var category = new PerformanceCounterCategory("Processor");
        var coreInstanceNames = category
            .GetInstanceNames()
            .Where(name => !string.Equals(name, "_Total", StringComparison.OrdinalIgnoreCase))
            .OrderBy(name => name)
            .ToArray();

        _perCoreCpuCounters = coreInstanceNames
            .Select(instanceName => new PerformanceCounter("Processor", "% Processor Time", instanceName))
            .ToArray();
    }

    public SystemStatsSnapshot GetCurrentStats()
    {
        var (totalPhysical, availablePhysical, totalVirtual, availableVirtual) = GetMemoryStatus();

        var perCoreCpuUsage = _perCoreCpuCounters
            .Select(counter => SafeGetNextValue(counter))
            .ToArray();

        var temperatures = GetTemperatures();
        var fanSpeeds = GetFanSpeeds();

        return new SystemStatsSnapshot
        {
            CapturedAt = DateTime.Now,
            CpuUsagePercentAverage = SafeGetNextValue(_totalCpuCounter),
            CpuUsagePercentPerCore = perCoreCpuUsage,
            TotalPhysicalMemoryMb = BytesToMegabytes(totalPhysical),
            UsedPhysicalMemoryMb = BytesToMegabytes(totalPhysical - availablePhysical),
            TotalVirtualMemoryMb = BytesToMegabytes(totalVirtual),
            UsedVirtualMemoryMb = BytesToMegabytes(totalVirtual - availableVirtual),
            TemperaturesCelsius = temperatures,
            FanSpeedsRpm = fanSpeeds,
            ProcessCount = Process.GetProcesses().Length
        };
    }

    private static float SafeGetNextValue(PerformanceCounter counter)
    {
        try
        {
            return counter.NextValue();
        }
        catch
        {
            return 0;
        }
    }

    private static Dictionary<string, double> GetTemperatures()
    {
        var result = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        try
        {
            using var thermalZoneSearcher = new ManagementObjectSearcher(
                "root\\WMI",
                "SELECT Name, CurrentTemperature FROM MSAcpi_ThermalZoneTemperature");

            foreach (var obj in thermalZoneSearcher.Get())
            {
                var name = obj["Name"]?.ToString();

                if (obj["CurrentTemperature"] is uint rawValue)
                {
                    var celsius = (rawValue / 10.0) - 273.15;

                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        result[name] = celsius;
                    }
                }
            }
        }
        catch (ManagementException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }

        try
        {
            using var cimTemperatureSearcher = new ManagementObjectSearcher(
                "root\\CIMV2",
                "SELECT Name, CurrentReading FROM Win32_TemperatureProbe");

            foreach (var obj in cimTemperatureSearcher.Get())
            {
                var name = obj["Name"]?.ToString();

                if (obj["CurrentReading"] is int probeValue && !string.IsNullOrWhiteSpace(name))
                {
                    result[name] = probeValue;
                }
            }
        }
        catch (ManagementException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }

        return result;
    }

    private static Dictionary<string, int> GetFanSpeeds()
    {
        var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        try
        {
            using var fanSearcher = new ManagementObjectSearcher(
                "root\\CIMV2",
                "SELECT Name, DesiredSpeed FROM Win32_Fan");

            foreach (var obj in fanSearcher.Get())
            {
                var name = obj["Name"]?.ToString();

                if (obj["DesiredSpeed"] is uint speed && !string.IsNullOrWhiteSpace(name))
                {
                    result[name] = (int)speed;
                }
            }
        }
        catch (ManagementException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }

        return result;
    }

    private static (ulong TotalPhysical, ulong AvailablePhysical, ulong TotalVirtual, ulong AvailableVirtual) GetMemoryStatus()
    {
        var status = new MemoryStatusEx
        {
            Length = (uint)Marshal.SizeOf<MemoryStatusEx>()
        };

        if (!GlobalMemoryStatusEx(ref status))
        {
            throw new InvalidOperationException("Unable to query global memory status.");
        }

        return (status.TotalPhys, status.AvailPhys, status.TotalVirtual, status.AvailVirtual);
    }

    private static double BytesToMegabytes(ulong value)
    {
        const double bytesPerMegabyte = 1024 * 1024;
        return value / bytesPerMegabyte;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MemoryStatusEx
    {
        public uint Length;
        public uint MemoryLoad;
        public ulong TotalPhys;
        public ulong AvailPhys;
        public ulong TotalPageFile;
        public ulong AvailPageFile;
        public ulong TotalVirtual;
        public ulong AvailVirtual;
        public ulong AvailExtendedVirtual;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx buffer);
}
