namespace SystemStats.Persistence;

public static class PersistenceConstants
{
    public const string RegistrySubKeyPath = @"Software\FinalWire\AIDA64\SensorValues";
    public const string RegistryValueName = "SensorValues";
    public const string SharedMemoryName = "AIDA64_SensorValues";
    public const int SharedMemoryCapacityBytes = 32768;
}

