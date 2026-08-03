// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.

using ComputerVitals.Core;

namespace ComputerVitals.Core.Tests;

[TestClass]
public sealed class TemperatureMonitorTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 2, 18, 0, 0, TimeSpan.Zero);

    [TestMethod]
    public void Refresh_ClassifiesCurrentAndUnsupportedWithoutInventingValues()
    {
        SequenceTemperatureProbe probe = new(
        [
            Result(TemperatureDeviceKind.Cpu, 52, Now),
            new TemperatureProbeResult(
                TemperatureDeviceKind.Gpu,
                "GPU",
                null,
                null,
                Now,
                false,
                "No GPU was discovered.")
        ]);
        TemperatureMonitor monitor = new(probe, historyCapacity: 4, staleAfter: TimeSpan.FromSeconds(5));

        IReadOnlyList<TemperatureSample> samples = monitor.Refresh(Now);

        TemperatureSample cpu = samples.Single(sample => sample.DeviceKind == TemperatureDeviceKind.Cpu);
        TemperatureSample gpu = samples.Single(sample => sample.DeviceKind == TemperatureDeviceKind.Gpu);
        Assert.AreEqual(TemperatureSampleState.Current, cpu.State);
        Assert.AreEqual(52f, cpu.ValueCelsius);
        Assert.AreEqual(52f, cpu.SessionMinimumCelsius);
        Assert.AreEqual(TemperatureSampleState.Unsupported, gpu.State);
        Assert.IsNull(gpu.ValueCelsius);
        StringAssert.Contains(gpu.Reason, "No GPU");
    }

    [TestMethod]
    public void Refresh_ClassifiesOldFiniteReadingAsStale()
    {
        SequenceTemperatureProbe probe = new([Result(TemperatureDeviceKind.Cpu, 51, Now - TimeSpan.FromSeconds(6))]);
        TemperatureMonitor monitor = new(probe, historyCapacity: 4, staleAfter: TimeSpan.FromSeconds(5));

        TemperatureSample cpu = monitor.Refresh(Now).Single(sample => sample.DeviceKind == TemperatureDeviceKind.Cpu);

        Assert.AreEqual(TemperatureSampleState.Stale, cpu.State);
        Assert.AreEqual(51f, cpu.ValueCelsius);
        StringAssert.Contains(cpu.Reason, "older");
    }

    [TestMethod]
    public void Refresh_ClassifiesFailedReadingAsUnavailable()
    {
        SequenceTemperatureProbe probe = new(
        [
            new TemperatureProbeResult(
                TemperatureDeviceKind.Cpu,
                "CPU",
                "Package",
                null,
                Now,
                true,
                "Low-level read failed.")
        ]);
        TemperatureMonitor monitor = new(probe, historyCapacity: 4, staleAfter: TimeSpan.FromSeconds(5));

        TemperatureSample cpu = monitor.Refresh(Now).Single(sample => sample.DeviceKind == TemperatureDeviceKind.Cpu);

        Assert.AreEqual(TemperatureSampleState.Unavailable, cpu.State);
        Assert.IsNull(cpu.ValueCelsius);
        Assert.AreEqual("Low-level read failed.", cpu.Reason);
    }

    [TestMethod]
    public void Refresh_KeepsOnlyConfiguredSessionHistoryCapacity()
    {
        SequenceTemperatureProbe probe = new(
            [Result(TemperatureDeviceKind.Cpu, 40, Now)],
            [Result(TemperatureDeviceKind.Cpu, 50, Now + TimeSpan.FromSeconds(1))],
            [Result(TemperatureDeviceKind.Cpu, 60, Now + TimeSpan.FromSeconds(2))]);
        TemperatureMonitor monitor = new(probe, historyCapacity: 2, staleAfter: TimeSpan.FromSeconds(5));

        monitor.Refresh(Now);
        monitor.Refresh(Now + TimeSpan.FromSeconds(1));
        TemperatureSample cpu = monitor.Refresh(Now + TimeSpan.FromSeconds(2)).Single(sample => sample.DeviceKind == TemperatureDeviceKind.Cpu);

        Assert.AreEqual(50f, cpu.SessionMinimumCelsius);
        Assert.AreEqual(60f, cpu.SessionMaximumCelsius);
    }

    private static TemperatureProbeResult Result(TemperatureDeviceKind kind, float value, DateTimeOffset observedAt) =>
        new(kind, kind.ToString(), "Primary temperature", value, observedAt, true);
}
