// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.

using ComputerVitals.Core;

namespace ComputerVitals.Core.Tests;

internal sealed class SequenceTemperatureProbe(params IReadOnlyList<TemperatureProbeResult>[] readings) : ITemperatureProbe
{
    private readonly Queue<IReadOnlyList<TemperatureProbeResult>> _readings = new(readings);

    public IReadOnlyList<TemperatureProbeResult> Read()
    {
        if (_readings.Count == 0)
            throw new InvalidOperationException("No synthetic reading remains.");

        return _readings.Dequeue();
    }
}
