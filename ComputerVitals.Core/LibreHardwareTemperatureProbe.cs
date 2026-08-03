// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.

using LibreHardwareMonitor.Hardware;

namespace ComputerVitals.Core;

public sealed class LibreHardwareTemperatureProbe : ITemperatureProbe, IDisposable
{
    private readonly Computer _computer;
    private readonly TimeProvider _timeProvider;
    private bool _disposed;

    public LibreHardwareTemperatureProbe(TimeProvider? timeProvider = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
        _computer = new Computer
        {
            IsCpuEnabled = true,
            IsGpuEnabled = true
        };
        _computer.Open();
    }

    public IReadOnlyList<TemperatureProbeResult> Read()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        DateTimeOffset observedAt = _timeProvider.GetUtcNow();
        IHardware[] hardware = EnumerateHardware(_computer.Hardware).ToArray();

        foreach (IHardware item in hardware)
            item.Update();

        return
        [
            ReadDevice(TemperatureDeviceKind.Cpu, hardware.Where(item => item.HardwareType == HardwareType.Cpu), observedAt),
            ReadDevice(TemperatureDeviceKind.Gpu, hardware.Where(IsGpu), observedAt)
        ];
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _computer.Close();
        _disposed = true;
    }

    private static TemperatureProbeResult ReadDevice(
        TemperatureDeviceKind deviceKind,
        IEnumerable<IHardware> candidates,
        DateTimeOffset observedAt)
    {
        IHardware[] devices = candidates.ToArray();
        if (devices.Length == 0)
        {
            return new TemperatureProbeResult(
                deviceKind,
                deviceKind == TemperatureDeviceKind.Cpu ? "CPU" : "GPU",
                null,
                null,
                observedAt,
                false,
                $"No {deviceKind.ToString().ToUpperInvariant()} hardware was discovered.");
        }

        (IHardware Hardware, ISensor Sensor)[] sensors = devices
            .SelectMany(hardware => hardware.Sensors
                .Where(sensor => sensor.SensorType == SensorType.Temperature)
                .Select(sensor => (Hardware: hardware, Sensor: sensor)))
            .OrderBy(candidate => Priority(deviceKind, candidate.Sensor.Name))
            .ToArray();

        (IHardware Hardware, ISensor Sensor) source = sensors
            .FirstOrDefault(candidate => candidate.Sensor.Value is float value && float.IsFinite(value));

        if (source.Sensor is null && sensors.Length > 0)
            source = sensors[0];

        if (source.Sensor is null)
        {
            return new TemperatureProbeResult(
                deviceKind,
                devices[0].Name,
                null,
                null,
                observedAt,
                true,
                "No readable temperature source is available. Required low-level access may be unavailable.");
        }

        float? value = source.Sensor.Value is float reading && float.IsFinite(reading) ? reading : null;
        return new TemperatureProbeResult(
            deviceKind,
            source.Hardware.Name,
            source.Sensor.Name,
            value,
            observedAt,
            true,
            value.HasValue ? null : "The temperature source was discovered but did not return a usable value.");
    }

    private static int Priority(TemperatureDeviceKind deviceKind, string sensorName)
    {
        string[] preferredNames = deviceKind == TemperatureDeviceKind.Cpu
            ? ["Core (Tctl/Tdie)", "Core (Tdie)", "CPU Package", "Package"]
            : ["GPU Core", "GPU Hot Spot", "GPU Memory Junction"];

        int index = Array.FindIndex(preferredNames, name => string.Equals(name, sensorName, StringComparison.OrdinalIgnoreCase));
        return index < 0 ? preferredNames.Length : index;
    }

    private static bool IsGpu(IHardware hardware) => hardware.HardwareType is
        HardwareType.GpuNvidia or HardwareType.GpuAmd or HardwareType.GpuIntel;

    private static IEnumerable<IHardware> EnumerateHardware(IEnumerable<IHardware> roots)
    {
        foreach (IHardware hardware in roots)
        {
            yield return hardware;

            foreach (IHardware child in EnumerateHardware(hardware.SubHardware))
                yield return child;
        }
    }
}
