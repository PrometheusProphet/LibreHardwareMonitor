// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.

namespace ComputerVitals.Core;

public sealed record TemperatureAlert(
    TemperatureDeviceKind DeviceKind,
    string DeviceName,
    string? SensorName,
    float ValueCelsius,
    float ThresholdCelsius,
    DateTimeOffset TriggeredAt,
    TimeSpan Persistence);
