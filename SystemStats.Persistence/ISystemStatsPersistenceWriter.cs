using SystemStats.Contracts;

namespace SystemStats.Persistence;

public interface ISystemStatsPersistenceWriter
{
    void Write(SystemStatsSnapshot snapshot);
}

