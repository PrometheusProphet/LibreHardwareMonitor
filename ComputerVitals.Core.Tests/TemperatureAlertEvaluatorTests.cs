// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.

using ComputerVitals.Core;

namespace ComputerVitals.Core.Tests;

[TestClass]
public sealed class TemperatureAlertEvaluatorTests
{
    private static readonly DateTimeOffset Start = new(2026, 8, 2, 18, 0, 0, TimeSpan.Zero);

    [TestMethod]
    public void Evaluate_RequiresPersistenceAndUsesHysteresisAndCooldown()
    {
        TemperatureAlertEvaluator evaluator = CreateEvaluator();

        Assert.IsNull(evaluator.Evaluate(Sample(82, Start)));
        TemperatureAlert? first = evaluator.Evaluate(Sample(83, Start + TimeSpan.FromSeconds(10)));
        Assert.IsNotNull(first);
        Assert.AreEqual(80f, first.ThresholdCelsius);

        Assert.IsNull(evaluator.Evaluate(Sample(84, Start + TimeSpan.FromSeconds(11))));
        Assert.IsNull(evaluator.Evaluate(Sample(74, Start + TimeSpan.FromSeconds(20))));
        Assert.IsNull(evaluator.Evaluate(Sample(82, Start + TimeSpan.FromSeconds(25))));
        Assert.IsNull(evaluator.Evaluate(Sample(82, Start + TimeSpan.FromSeconds(35))));

        TemperatureAlert? second = evaluator.Evaluate(Sample(82, Start + TimeSpan.FromSeconds(45)));
        Assert.IsNotNull(second);
    }

    [TestMethod]
    public void Evaluate_DoesNotTriggerOrClearFromUnavailableInput()
    {
        TemperatureAlertEvaluator evaluator = CreateEvaluator();
        Assert.IsNull(evaluator.Evaluate(Sample(82, Start)));

        TemperatureSample unavailable = Sample(null, Start + TimeSpan.FromSeconds(10), TemperatureSampleState.Unavailable);
        Assert.IsNull(evaluator.Evaluate(unavailable));

        TemperatureAlert? alert = evaluator.Evaluate(Sample(82, Start + TimeSpan.FromSeconds(11)));
        Assert.IsNotNull(alert);
    }

    private static TemperatureAlertEvaluator CreateEvaluator() =>
        new(new TemperatureAlertPolicy(80, TimeSpan.FromSeconds(10), 5, TimeSpan.FromSeconds(30)));

    private static TemperatureSample Sample(
        float? value,
        DateTimeOffset observedAt,
        TemperatureSampleState state = TemperatureSampleState.Current) =>
        new(
            TemperatureDeviceKind.Gpu,
            "Synthetic GPU",
            "GPU Core",
            state,
            value,
            value,
            value,
            observedAt,
            state == TemperatureSampleState.Current ? null : "Synthetic failure");
}
