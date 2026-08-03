// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using ComputerVitals.Core;

namespace ComputerVitals.Wpf;

public partial class MainWindow : Window
{
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(2);
    private readonly WindowsAppNotificationService _notifications;
    private readonly DispatcherTimer _timer;
    private LibreHardwareTemperatureProbe? _probe;
    private TemperatureMonitor? _monitor;
    private TemperatureAlertEvaluator? _alertEvaluator;
    private bool _refreshing;

    public MainWindow(WindowsAppNotificationService notifications, string notificationStatus)
    {
        _notifications = notifications;
        InitializeComponent();

        NotificationStatusText.Text = notificationStatus;
        _timer = new DispatcherTimer { Interval = RefreshInterval };
        _timer.Tick += async (_, _) => await RefreshAsync();
        Loaded += OnLoaded;
        Closed += OnClosed;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            _probe = new LibreHardwareTemperatureProbe();
            _monitor = new TemperatureMonitor(_probe, historyCapacity: 60, staleAfter: TimeSpan.FromSeconds(5));
            _timer.Start();
            await RefreshAsync();
        }
        catch (Exception exception)
        {
            StatusText.Text = $"Sensor probe unavailable: {exception.Message}";
        }
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _timer.Stop();
        _probe?.Dispose();
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e) => await RefreshAsync();

    private void ApplyAlert_Click(object sender, RoutedEventArgs e)
    {
        if (!float.TryParse(ThresholdTextBox.Text, NumberStyles.Float, CultureInfo.CurrentCulture, out float threshold) ||
            !double.TryParse(PersistenceTextBox.Text, NumberStyles.Float, CultureInfo.CurrentCulture, out double persistenceSeconds) ||
            !float.TryParse(HysteresisTextBox.Text, NumberStyles.Float, CultureInfo.CurrentCulture, out float hysteresis) ||
            persistenceSeconds < 0 ||
            hysteresis < 0)
        {
            StatusText.Text = "Enter a numeric threshold and non-negative persistence and hysteresis values.";
            return;
        }

        try
        {
            _alertEvaluator = new TemperatureAlertEvaluator(
                new TemperatureAlertPolicy(
                    threshold,
                    TimeSpan.FromSeconds(persistenceSeconds),
                    hysteresis,
                    TimeSpan.FromMinutes(5)));
            StatusText.Text = $"Alert armed at {threshold:F1} °C after {persistenceSeconds:F0} seconds; clears below {threshold - hysteresis:F1} °C.";
        }
        catch (ArgumentOutOfRangeException exception)
        {
            StatusText.Text = $"Alert configuration is invalid: {exception.ParamName}.";
        }
    }

    private async Task RefreshAsync()
    {
        if (_refreshing || _monitor is null)
            return;

        _refreshing = true;
        try
        {
            IReadOnlyList<TemperatureSample> samples = await Task.Run(() => _monitor.Refresh(DateTimeOffset.UtcNow));
            foreach (TemperatureSample sample in samples)
            {
                UpdateCard(sample);
                TemperatureAlert? alert = _alertEvaluator?.Evaluate(sample);
                if (alert is not null)
                    _notifications.Show(alert);
            }

            StatusText.Text = $"Last read-only refresh: {DateTimeOffset.Now:T}. Session history is kept only while this window is open.";
        }
        catch (Exception exception)
        {
            StatusText.Text = $"Refresh failed; previous values are not presented as current: {exception.Message}";
        }
        finally
        {
            _refreshing = false;
        }
    }

    private void UpdateCard(TemperatureSample sample)
    {
        bool cpu = sample.DeviceKind == TemperatureDeviceKind.Cpu;
        Border card = cpu ? CpuCard : GpuCard;
        TextBlock state = cpu ? CpuStateText : GpuStateText;
        TextBlock value = cpu ? CpuValueText : GpuValueText;
        TextBlock name = cpu ? CpuNameText : GpuNameText;
        TextBlock source = cpu ? CpuSourceText : GpuSourceText;
        TextBlock range = cpu ? CpuRangeText : GpuRangeText;
        TextBlock freshness = cpu ? CpuFreshnessText : GpuFreshnessText;
        TextBlock reason = cpu ? CpuReasonText : GpuReasonText;

        state.Text = sample.State.ToString();
        value.Text = sample.ValueCelsius is float current ? $"{current:F1} °C" : "—";
        name.Text = sample.DeviceName;
        source.Text = $"Source: {sample.SensorName ?? "—"}";
        range.Text = sample.SessionMinimumCelsius is float minimum && sample.SessionMaximumCelsius is float maximum
            ? $"Session range: {minimum:F1}–{maximum:F1} °C"
            : "Session range: —";
        freshness.Text = sample.State is TemperatureSampleState.Current or TemperatureSampleState.Stale
            ? $"Observed: {sample.ObservedAt.ToLocalTime():T}"
            : "Freshness: no usable sample";
        reason.Text = sample.Reason ?? "Reading is current.";
        card.BorderBrush = sample.State switch
        {
            TemperatureSampleState.Current => Brushes.SeaGreen,
            TemperatureSampleState.Stale => Brushes.DarkGoldenrod,
            TemperatureSampleState.Unavailable => Brushes.IndianRed,
            _ => Brushes.Gray
        };
    }
}
