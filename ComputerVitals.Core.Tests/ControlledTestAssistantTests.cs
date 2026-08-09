// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.

using ComputerVitals.Core;

namespace ComputerVitals.Core.Tests;

[TestClass]
public sealed class ControlledTestAssistantTests
{
    [TestMethod]
    public void Prepare_RejectsAMultiVariableInterventionAsAControlledTest()
    {
        ControlledTestAssistant assistant = CreateAssistant(out _);

        ArgumentException exception = Assert.Throws<ArgumentException>(() => assistant.Prepare(Plan() with
        {
            OnlyVariableWillChange = false,
            UserPerformedAction = "Change the dock connection and install a chipset package"
        }));

        StringAssert.Contains(exception.Message, "multi-variable intervention");
        Assert.IsNull(assistant.CurrentPlan);
    }

    [TestMethod]
    public void RecordResult_AddsOneQualifiedEntryToTheExistingBoundedTimeline()
    {
        ControlledTestAssistant assistant = CreateAssistant(out SessionDiagnosticTimeline timeline);
        DiagnosticDeviceReference explicitlyLinkedDevice = new(
            "Synthetic dock",
            "user explicitly selected the local reference",
            DateTimeOffset.UnixEpoch);
        assistant.Prepare(Plan() with { RelatedDevice = explicitlyLinkedDevice });

        DiagnosticTimelineEntry entry = assistant.RecordResult(new ControlledTestResult(
            DateTimeOffset.UnixEpoch.AddMinutes(2),
            DiagnosticOutcome.Failed,
            "Synthetic user observation",
            "The failure occurred before Windows started."));

        Assert.HasCount(1, timeline.Entries);
        Assert.AreSame(entry, timeline.Entries[0]);
        StringAssert.Contains(entry.Condition, "Controlled variable: dock connection");
        StringAssert.Contains(entry.Condition, "Baseline: dock attached");
        StringAssert.Contains(entry.Condition, "Constants: same host, power source, and software state");
        Assert.AreEqual("User performed: disconnect and reconnect the dock", entry.Action);
        Assert.AreEqual(DiagnosticConclusion.NoConclusion, entry.Conclusion);
        Assert.AreSame(explicitlyLinkedDevice, entry.RelatedDevice);
        Assert.IsNull(assistant.CurrentPlan);

        DiagnosticCorrelationAssessment assessment = DiagnosticCorrelationExplainer.Assess(entry);
        Assert.AreEqual(DiagnosticConclusion.PotentiallyRelevant, assessment.Qualification);
        StringAssert.Contains(assessment.Summary, "not a confirmed cause");
    }

    [TestMethod]
    public void RecordResult_RequiresAPreparedPlanAndNeverOffersARootCauseConclusion()
    {
        ControlledTestAssistant assistant = CreateAssistant(out _);

        Assert.Throws<InvalidOperationException>(() => assistant.RecordResult(new ControlledTestResult(
            DateTimeOffset.UnixEpoch,
            DiagnosticOutcome.Succeeded,
            "Synthetic evidence")));

        Assert.IsFalse(typeof(ControlledTestResult).GetProperties().Any(property => property.PropertyType == typeof(DiagnosticConclusion)));
    }

    private static ControlledTestAssistant CreateAssistant(out SessionDiagnosticTimeline timeline)
    {
        timeline = new SessionDiagnosticTimeline(capacity: 5);
        return new ControlledTestAssistant(timeline);
    }

    private static ControlledTestPlan Plan() => new(
        "dock connection",
        "dock attached",
        "same host, power source, and software state",
        "disconnect and reconnect the dock",
        DiagnosticObservationStage.BeforeWindowsStarts,
        OnlyVariableWillChange: true);
}
