# SystemStats

This solution contains three .NET 6 projects that work together to collect and consume basic system statistics.

### Projects

- **SystemStats.Core**  
  Class library (DLL) that exposes system statistics. Currently provides a method to retrieve the current system time via `ISystemStatsProvider` / `SystemStatsProvider`. This DLL is referenced by both the client and service projects.

- **SystemStats.Client**  
  WPF desktop client that calls `SystemStats.Core` on startup to display the current system time. It has a **Refresh Time** button to query the DLL again and update the time, and a **Quit** button that asks for confirmation before exiting.

- **SystemStats.Service**  
  Worker-style background service application that continually runs, using `SystemStats.Core` to get the current system time at a configurable interval (in milliseconds, via `appsettings.json`). Each poll writes the latest time into the Windows registry under `HKCU\Software\SystemStats\LastSystemTime`.

### Build

From the solution root (`SystemStats`):

```bash
dotnet build
```

### Run the WPF Client

From the solution root:

```bash
dotnet run --project SystemStats.Client
```

This opens the UI, shows the current system time, and lets you refresh it or quit with confirmation.

### Run the Service (as a console app)

From the solution root:

```bash
dotnet run --project SystemStats.Service
```

The service will:

- Read `SystemStats:PollingIntervalMilliseconds` from `SystemStats.Service/appsettings.json`.
- At each interval, call `SystemStats.Core` to get the current time.
- Store the time in the registry at `HKCU\Software\SystemStats\LastSystemTime`.
