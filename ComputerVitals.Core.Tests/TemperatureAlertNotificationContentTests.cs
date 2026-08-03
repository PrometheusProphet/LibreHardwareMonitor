// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.

using ComputerVitals.Core;

namespace ComputerVitals.Core.Tests;

[TestClass]
public sealed class TemperatureAlertNotificationContentTests
{
    [TestMethod]
    public void FromAlert_ExplainsSyntheticTriggerWithoutClaimingUniversalSafety()
    {
        TemperatureAlert alert = new(
            TemperatureDeviceKind.Cpu,
            "Synthetic CPU",
            "Package",
            83.25f,
            80f,
            new DateTimeOffset(2026, 8, 3, 12, 0, 0, TimeSpan.Zero),
            TimeSpan.FromSeconds(10));

        TemperatureAlertNotificationContent content = TemperatureAlertNotificationContent.FromAlert(alert);

        Assert.AreEqual("Cpu temperature remained above your threshold", content.Title);
        Assert.AreEqual(
            "Synthetic CPU · Package: 83.2 °C (threshold 80.0 °C for 10 seconds)",
            content.Body);
        Assert.IsFalse(content.Title.Contains("safe", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(content.Body.Contains("safe", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void FromAlert_UsesTruthfulFallbackWhenSensorNameIsMissing()
    {
        TemperatureAlert alert = new(
            TemperatureDeviceKind.Gpu,
            "Synthetic GPU",
            null,
            91f,
            90f,
            new DateTimeOffset(2026, 8, 3, 12, 0, 0, TimeSpan.Zero),
            TimeSpan.Zero);

        TemperatureAlertNotificationContent content = TemperatureAlertNotificationContent.FromAlert(alert);

        StringAssert.Contains(content.Body, "temperature source");
    }
}
