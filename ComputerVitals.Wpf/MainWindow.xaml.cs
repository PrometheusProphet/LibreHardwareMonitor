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
    private IReadOnlyList<TemperatureSample> _latestTemperatureSamples = [];
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
        catch (UnauthorizedAccessException)
        {
            PresentTemperatureFailure(ExpectedFailureKind.SensorInitialization);
        }
        catch (InvalidOperationException)
        {
            PresentTemperatureFailure(ExpectedFailureKind.SensorInitialization);
        }
        catch (DllNotFoundException)
        {
            PresentTemperatureFailure(ExpectedFailureKind.SensorInitialization);
        }
        catch (BadImageFormatException)
        {
            PresentTemperatureFailure(ExpectedFailureKind.SensorInitialization);
        }
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _timer.Stop();
        _probe?.Dispose();
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e) => await RefreshAsync();

    private void ConnectedHardware_Click(object sender, RoutedEventArgs e) =>
        new ConnectedHardwareWindow(_latestTemperatureSamples.ToArray()) { Owner = this }.Show();

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
            _latestTemperatureSamples = samples.ToArray();
            foreach (TemperatureSample sample in samples)
            {
                UpdateCard(sample);
                TemperatureAlert? alert = _alertEvaluator?.Evaluate(sample);
                if (alert is not null && _notifications.Show(alert) is ExpectedFailurePresentation notificationFailure)
                    NotificationStatusText.Text = notificationFailure.Detail;
            }

            StatusText.Text = $"Last read-only refresh: {DateTimeOffset.Now:T}. Session history is kept only while this window is open.";
        }
        catch (UnauthorizedAccessException)
        {
            PresentTemperatureFailure(ExpectedFailureKind.SensorRefresh);
        }
        catch (InvalidOperationException)
        {
            PresentTemperatureFailure(ExpectedFailureKind.SensorRefresh);
        }
        catch (DllNotFoundException)
        {
            PresentTemperatureFailure(ExpectedFailureKind.SensorRefresh);
        }
        catch (BadImageFormatException)
        {
            PresentTemperatureFailure(ExpectedFailureKind.SensorRefresh);
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
        SelectableText state = cpu ? CpuStateText : GpuStateText;
        SelectableText value = cpu ? CpuValueText : GpuValueText;
        SelectableText name = cpu ? CpuNameText : GpuNameText;
        SelectableText source = cpu ? CpuSourceText : GpuSourceText;
        SelectableText range = cpu ? CpuRangeText : GpuRangeText;
        SelectableText freshness = cpu ? CpuFreshnessText : GpuFreshnessText;
        SelectableText reason = cpu ? CpuReasonText : GpuReasonText;

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

    private void PresentTemperatureFailure(ExpectedFailureKind kind)
    {
        ExpectedFailurePresentation presentation = ExpectedFailurePresentationPolicy.For(kind);
        _latestTemperatureSamples = [];
        if (presentation.Disposition == ExpectedFailureDisposition.StopTemperatureMonitoring)
        {
            _timer.Stop();
            _monitor = null;
            _probe?.Dispose();
            _probe = null;
        }
        StatusText.Text = presentation.Headline;
        PresentUnavailableTemperatureCard(CpuCard, CpuStateText, CpuValueText, CpuNameText, CpuSourceText, CpuRangeText, CpuFreshnessText, CpuReasonText, presentation.Detail);
        PresentUnavailableTemperatureCard(GpuCard, GpuStateText, GpuValueText, GpuNameText, GpuSourceText, GpuRangeText, GpuFreshnessText, GpuReasonText, presentation.Detail);
    }

    private static void PresentUnavailableTemperatureCard(
        Border card,
        SelectableText state,
        SelectableText value,
        SelectableText name,
        SelectableText source,
        SelectableText range,
        SelectableText freshness,
        SelectableText reason,
        string detail)
    {
        state.Text = "Unavailable";
        value.Text = "—";
        name.Text = string.Empty;
        source.Text = string.Empty;
        range.Text = string.Empty;
        freshness.Text = string.Empty;
        reason.Text = detail;
        card.BorderBrush = Brushes.IndianRed;
    }
}
