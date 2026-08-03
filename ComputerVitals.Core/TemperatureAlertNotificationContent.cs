// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.

namespace ComputerVitals.Core;

public sealed record TemperatureAlertNotificationContent(string Title, string Body)
{
    public static TemperatureAlertNotificationContent FromAlert(TemperatureAlert alert)
    {
        ArgumentNullException.ThrowIfNull(alert);

        return new TemperatureAlertNotificationContent(
            $"{alert.DeviceKind} temperature remained above your threshold",
            $"{alert.DeviceName} · {alert.SensorName ?? "temperature source"}: {alert.ValueCelsius:F1} °C " +
            $"(threshold {alert.ThresholdCelsius:F1} °C for {alert.Persistence.TotalSeconds:F0} seconds)");
    }
}
