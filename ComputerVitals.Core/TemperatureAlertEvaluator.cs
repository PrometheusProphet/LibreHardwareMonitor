// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.

namespace ComputerVitals.Core;

public sealed class TemperatureAlertEvaluator
{
    private readonly TemperatureAlertPolicy _policy;
    private readonly Dictionary<TemperatureDeviceKind, AlertState> _states = new();

    public TemperatureAlertEvaluator(TemperatureAlertPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);
        policy.Validate();
        _policy = policy;
    }

    public TemperatureAlert? Evaluate(TemperatureSample sample)
    {
        ArgumentNullException.ThrowIfNull(sample);

        if (sample.State != TemperatureSampleState.Current || sample.ValueCelsius is not float value || !float.IsFinite(value))
            return null;

        if (!_states.TryGetValue(sample.DeviceKind, out AlertState? state))
        {
            state = new AlertState();
            _states.Add(sample.DeviceKind, state);
        }

        if (value <= _policy.ThresholdCelsius - _policy.HysteresisCelsius)
        {
            state.AboveSince = null;
            state.IsLatched = false;
            return null;
        }

        if (value < _policy.ThresholdCelsius)
            return null;

        state.AboveSince ??= sample.ObservedAt;
        if (state.IsLatched || sample.ObservedAt - state.AboveSince < _policy.Persistence)
            return null;

        if (state.LastTriggeredAt is DateTimeOffset lastTriggered && sample.ObservedAt - lastTriggered < _policy.Cooldown)
            return null;

        state.IsLatched = true;
        state.LastTriggeredAt = sample.ObservedAt;
        return new TemperatureAlert(
            sample.DeviceKind,
            sample.DeviceName,
            sample.SensorName,
            value,
            _policy.ThresholdCelsius,
            sample.ObservedAt,
            _policy.Persistence);
    }

    private sealed class AlertState
    {
        public DateTimeOffset? AboveSince { get; set; }

        public DateTimeOffset? LastTriggeredAt { get; set; }

        public bool IsLatched { get; set; }
    }
}
