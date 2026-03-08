namespace SystemStats.Contracts;

public class SystemStatsSnapshot
{
    public DateTime CapturedAt { get; set; }

    public double CpuUsagePercentAverage { get; set; }

    public IReadOnlyList<float>? CpuUsagePercentPerCore { get; set; }

    public double TotalPhysicalMemoryMb { get; set; }

    public double UsedPhysicalMemoryMb { get; set; }

    public double TotalVirtualMemoryMb { get; set; }

    public double UsedVirtualMemoryMb { get; set; }

    public IDictionary<string, double>? TemperaturesCelsius { get; set; }

    public IDictionary<string, int>? FanSpeedsRpm { get; set; }

    public int ProcessCount { get; set; }
}
