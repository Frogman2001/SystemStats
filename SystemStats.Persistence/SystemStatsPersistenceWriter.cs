using System.IO.MemoryMappedFiles;
using System.Text;
using SystemStats.Contracts;
using WinRegistry = Microsoft.Win32.Registry;

namespace SystemStats.Persistence;

public sealed class SystemStatsPersistenceWriter : ISystemStatsPersistenceWriter
{
    private const string TemperatureUnit = "C";
    private const string FanSpeedUnit = "RPM";
    private const string CpuUsageUnit = "%";

    private readonly PersistenceTarget _target;

    public SystemStatsPersistenceWriter(PersistenceTarget target)
    {
        _target = target;
    }

    public void Write(SystemStatsSnapshot snapshot)
    {
        if (_target is PersistenceTarget.RegistryOnly or PersistenceTarget.RegistryAndSharedMemory)
        {
            WriteToRegistry(snapshot);
        }

        if (_target is PersistenceTarget.SharedMemoryOnly or PersistenceTarget.RegistryAndSharedMemory)
        {
            WriteToSharedMemory(snapshot);
        }
    }

    private static void WriteToRegistry(SystemStatsSnapshot snapshot)
    {
        string payload = BuildSensorValuesPayload(snapshot);

        using var key = WinRegistry.CurrentUser.CreateSubKey(PersistenceConstants.RegistrySubKeyPath);
        key?.SetValue(PersistenceConstants.RegistryValueName, payload);
    }

    private static void WriteToSharedMemory(SystemStatsSnapshot snapshot)
    {
        string payload = BuildSensorValuesPayload(snapshot);
        byte[] bytes = Encoding.ASCII.GetBytes(payload);

        if (bytes.Length + 1 > PersistenceConstants.SharedMemoryCapacityBytes)
        {
            Array.Resize(ref bytes, PersistenceConstants.SharedMemoryCapacityBytes - 1);
        }

        using var memoryMappedFile = MemoryMappedFile.CreateOrOpen(
            PersistenceConstants.SharedMemoryName,
            PersistenceConstants.SharedMemoryCapacityBytes,
            MemoryMappedFileAccess.ReadWrite);

        using var accessor = memoryMappedFile.CreateViewAccessor(
            0,
            PersistenceConstants.SharedMemoryCapacityBytes,
            MemoryMappedFileAccess.Write);

        accessor.WriteArray(0, bytes, 0, bytes.Length);
        accessor.Write(bytes.Length, (byte)0);
    }

    private static string BuildSensorValuesPayload(SystemStatsSnapshot snapshot)
    {
        var builder = new StringBuilder();

        builder.Append("<AIDA64_SensorValues>");

        builder.AppendFormat(
            "<sensor id=\"CapturedAt\" label=\"Captured At\" value=\"{0:O}\" unit=\"\" />",
            snapshot.CapturedAt);

        builder.AppendFormat(
            "<sensor id=\"CpuUsageAverage\" label=\"CPU Usage\" value=\"{0:F1}\" unit=\"{1}\" />",
            snapshot.CpuUsagePercentAverage,
            CpuUsageUnit);

        if (snapshot.CpuUsagePercentPerCore is { Count: > 0 } cores)
        {
            for (int index = 0; index < cores.Count; index++)
            {
                double value = cores[index];

                builder.AppendFormat(
                    "<sensor id=\"CpuCore{0}\" label=\"CPU Core {0}\" value=\"{1:F1}\" unit=\"{2}\" />",
                    index,
                    value,
                    CpuUsageUnit);
            }
        }

        if (snapshot.TemperaturesCelsius is { Count: > 0 } temperatures)
        {
            foreach (var pair in temperatures)
            {
                builder.AppendFormat(
                    "<sensor id=\"Temp_{0}\" label=\"{0}\" value=\"{1:F1}\" unit=\"{2}\" />",
                    pair.Key,
                    pair.Value,
                    TemperatureUnit);
            }
        }

        if (snapshot.FanSpeedsRpm is { Count: > 0 } fans)
        {
            foreach (var pair in fans)
            {
                builder.AppendFormat(
                    "<sensor id=\"Fan_{0}\" label=\"{0}\" value=\"{1}\" unit=\"{2}\" />",
                    pair.Key,
                    pair.Value,
                    FanSpeedUnit);
            }
        }

        builder.Append("</AIDA64_SensorValues>");

        return builder.ToString();
    }
}

