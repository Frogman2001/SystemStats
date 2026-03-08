using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SystemStats.Core;
using SystemStats.Persistence;
using SystemStats.Service;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        services.Configure<SystemStatsOptions>(
            hostContext.Configuration.GetSection("SystemStats"));

        services.AddSingleton<ISystemStatsProvider, SystemStatsProvider>();
        services.AddSingleton<ISystemStatsPersistenceWriter>(_ =>
            new SystemStatsPersistenceWriter(PersistenceTarget.RegistryAndSharedMemory));
        services.AddHostedService<Worker>();
    })
    .ConfigureLogging(logging =>
    {
        logging.AddConsole();
    })
    .Build();

await host.RunAsync();
