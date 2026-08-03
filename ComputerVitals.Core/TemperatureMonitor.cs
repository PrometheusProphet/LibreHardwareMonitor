// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.

namespace ComputerVitals.Core;

public sealed class TemperatureMonitor
{
    private static readonly TemperatureDeviceKind[] RequiredDevices =
    [
        TemperatureDeviceKind.Cpu,
        TemperatureDeviceKind.Gpu
    ];

    private readonly ITemperatureProbe _probe;
    private readonly int _historyCapacity;
    private readonly TimeSpan _staleAfter;
    private readonly Dictionary<TemperatureDeviceKind, Queue<(DateTimeOffset ObservedAt, float Value)>> _history = new();

    public TemperatureMonitor(ITemperatureProbe probe, int historyCapacity, TimeSpan staleAfter)
    {
        ArgumentNullException.ThrowIfNull(probe);

        if (historyCapacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(historyCapacity));

        if (staleAfter < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(staleAfter));

        _probe = probe;
        _historyCapacity = historyCapacity;
        _staleAfter = staleAfter;
    }

    public IReadOnlyList<TemperatureSample> Refresh(DateTimeOffset now)
    {
        IReadOnlyList<TemperatureProbeResult> results = _probe.Read();
        List<TemperatureSample> samples = new(RequiredDevices.Length);

        foreach (TemperatureDeviceKind deviceKind in RequiredDevices)
        {
            TemperatureProbeResult? result = results.FirstOrDefault(candidate => candidate.DeviceKind == deviceKind);
            samples.Add(result is null
                ? MissingDevice(deviceKind, now)
                : Classify(result, now));
        }

        return samples;
    }

    private TemperatureSample Classify(TemperatureProbeResult result, DateTimeOffset now)
    {
        if (!result.IsSupported)
        {
            return CreateSample(
                result,
                TemperatureSampleState.Unsupported,
                null,
                result.Reason ?? "This device or temperature source is not supported.");
        }

        if (result.ValueCelsius is not float value || !float.IsFinite(value))
        {
            return CreateSample(
                result,
                TemperatureSampleState.Unavailable,
                null,
                result.Reason ?? "The temperature source did not return a usable value.");
        }

        AddHistory(result.DeviceKind, result.ObservedAt, value);

        bool stale = now - result.ObservedAt > _staleAfter;
        return CreateSample(
            result,
            stale ? TemperatureSampleState.Stale : TemperatureSampleState.Current,
            value,
            stale ? result.Reason ?? "The last reading is older than the freshness window." : result.Reason);
    }

    private TemperatureSample CreateSample(
        TemperatureProbeResult result,
        TemperatureSampleState state,
        float? value,
        string? reason)
    {
        (float? minimum, float? maximum) = Range(result.DeviceKind);
        return new TemperatureSample(
            result.DeviceKind,
            result.DeviceName,
            result.SensorName,
            state,
            value,
            minimum,
            maximum,
            result.ObservedAt,
            reason);
    }

    private TemperatureSample MissingDevice(TemperatureDeviceKind deviceKind, DateTimeOffset now) =>
        new(
            deviceKind,
            deviceKind == TemperatureDeviceKind.Cpu ? "CPU" : "GPU",
            null,
            TemperatureSampleState.Unsupported,
            null,
            null,
            null,
            now,
            $"No {deviceKind.ToString().ToUpperInvariant()} hardware was discovered.");

    private void AddHistory(TemperatureDeviceKind deviceKind, DateTimeOffset observedAt, float value)
    {
        if (!_history.TryGetValue(deviceKind, out Queue<(DateTimeOffset ObservedAt, float Value)>? values))
        {
            values = new Queue<(DateTimeOffset ObservedAt, float Value)>();
            _history.Add(deviceKind, values);
        }

        values.Enqueue((observedAt, value));
        while (values.Count > _historyCapacity)
            values.Dequeue();
    }

    private (float? Minimum, float? Maximum) Range(TemperatureDeviceKind deviceKind)
    {
        if (!_history.TryGetValue(deviceKind, out Queue<(DateTimeOffset ObservedAt, float Value)>? values) || values.Count == 0)
            return (null, null);

        return (values.Min(value => value.Value), values.Max(value => value.Value));
    }
}
