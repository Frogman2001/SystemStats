using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SystemStats.Core;
using SystemStats.Contracts;
using SystemStats.Persistence;

namespace SystemStats.Service;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly ISystemStatsProvider _systemStatsProvider;
    private readonly ISystemStatsPersistenceWriter _persistenceWriter;
    private readonly SystemStatsOptions _options;

    public Worker(
        ILogger<Worker> logger,
        ISystemStatsProvider systemStatsProvider,
        ISystemStatsPersistenceWriter persistenceWriter,
        IOptions<SystemStatsOptions> options)
    {
        _logger = logger;
        _systemStatsProvider = systemStatsProvider;
        _persistenceWriter = persistenceWriter;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SystemStats service starting with interval {Interval} ms",
            _options.PollingIntervalMilliseconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                SystemStatsSnapshot snapshot = _systemStatsProvider.GetCurrentStats();

                _persistenceWriter.Write(snapshot);

                _logger.LogInformation("Recorded system stats at {Time}", snapshot.CapturedAt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while collecting or writing system stats.");
            }

            var delay = _options.PollingIntervalMilliseconds;
            if (delay < 1)
            {
                delay = 1000;
            }

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("SystemStats service is stopping.");
    }
}
