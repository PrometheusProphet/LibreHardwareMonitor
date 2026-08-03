// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.

namespace ComputerVitals.Core;

public sealed record TemperatureAlertPolicy(
    float ThresholdCelsius,
    TimeSpan Persistence,
    float HysteresisCelsius,
    TimeSpan Cooldown)
{
    public void Validate()
    {
        if (!float.IsFinite(ThresholdCelsius))
            throw new ArgumentOutOfRangeException(nameof(ThresholdCelsius));

        if (Persistence < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(Persistence));

        if (!float.IsFinite(HysteresisCelsius) || HysteresisCelsius < 0)
            throw new ArgumentOutOfRangeException(nameof(HysteresisCelsius));

        if (Cooldown < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(Cooldown));
    }
}
