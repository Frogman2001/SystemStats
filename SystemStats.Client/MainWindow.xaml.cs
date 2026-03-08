using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using SystemStats.Core;
using SystemStats.Contracts;
using SystemStats.Persistence;

namespace SystemStats.Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ISystemStatsProvider _systemStatsProvider;
        private readonly ISystemStatsPersistenceWriter _persistenceWriter;

        public MainWindow()
        {
            InitializeComponent();
            _systemStatsProvider = new SystemStatsProvider();
            _persistenceWriter = new SystemStatsPersistenceWriter(PersistenceTarget.RegistryAndSharedMemory);
            UpdateSystemStats();
        }

        private void UpdateSystemStats()
        {
            SystemStatsSnapshot snapshot = _systemStatsProvider.GetCurrentStats();
            _persistenceWriter.Write(snapshot);

            TimeTextBlock.Text = $"Captured at {snapshot.CapturedAt:O}";

            // CPU overall
            double cpuAverage = snapshot.CpuUsagePercentAverage;
            if (cpuAverage < 0)
            {
                cpuAverage = 0;
            }
            if (cpuAverage > 100)
            {
                cpuAverage = 100;
            }

            CpuAverageProgressBar.Value = cpuAverage;
            CpuAverageTextBlock.Text = $"{snapshot.CpuUsagePercentAverage:F1}%";

            // CPU per core chart
            if (snapshot.CpuUsagePercentPerCore is { } cores && cores.Count > 0)
            {
                var items = cores
                    .Select((value, index) => new CoreUsage($"Core {index}", value))
                    .ToList();

                CpuPerCoreItemsControl.ItemsSource = items;
            }
            else
            {
                CpuPerCoreItemsControl.ItemsSource = null;
            }

            // Memory summary
            var physicalPercent = snapshot.TotalPhysicalMemoryMb > 0
                ? snapshot.UsedPhysicalMemoryMb / snapshot.TotalPhysicalMemoryMb * 100
                : 0;

            var virtualPercent = snapshot.TotalVirtualMemoryMb > 0
                ? snapshot.UsedVirtualMemoryMb / snapshot.TotalVirtualMemoryMb * 100
                : 0;

            MemorySummaryTextBlock.Text =
                $"Physical: {snapshot.UsedPhysicalMemoryMb:F0} / {snapshot.TotalPhysicalMemoryMb:F0} MB ({physicalPercent:F1}%){Environment.NewLine}" +
                $"Virtual:  {snapshot.UsedVirtualMemoryMb:F0} / {snapshot.TotalVirtualMemoryMb:F0} MB ({virtualPercent:F1}%)";

            // Temperatures
            if (snapshot.TemperaturesCelsius is { Count: > 0 } temps)
            {
                var lines = temps
                    .OrderBy(kvp => kvp.Key, StringComparer.OrdinalIgnoreCase)
                    .Select(kvp => $"{kvp.Key}: {kvp.Value:F1} °C");

                TempsTextBlock.Text = string.Join(Environment.NewLine, lines);
            }
            else
            {
                TempsTextBlock.Text = "No temperature data available.";
            }

            // Fans
            if (snapshot.FanSpeedsRpm is { Count: > 0 } fans)
            {
                var lines = fans
                    .OrderBy(kvp => kvp.Key, StringComparer.OrdinalIgnoreCase)
                    .Select(kvp => $"{kvp.Key}: {kvp.Value} RPM");

                FansTextBlock.Text = string.Join(Environment.NewLine, lines);
            }
            else
            {
                FansTextBlock.Text = "No fan data available.";
            }

            // Other stats
            OtherStatsTextBlock.Text =
                $"Processes: {snapshot.ProcessCount}";
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateSystemStats();
        }

        private void QuitButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to quit?",
                "Confirm Quit",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                Close();
            }
        }

        private sealed class CoreUsage
        {
            public string Name { get; }
            public double Value { get; }

            public CoreUsage(string name, double value)
            {
                Name = name;
                Value = value;
            }
        }
    }
}