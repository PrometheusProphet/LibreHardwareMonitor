// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.

using ComputerVitals.Core;

namespace ComputerVitals.Core.Tests;

[TestClass]
public sealed class DiagnosticCorrelationExplainerTests
{
    [TestMethod]
    public void Assess_QualifiesFailedPreWindowsDockObservationAsPotentiallyRelevant()
    {
        DiagnosticCorrelationAssessment assessment = DiagnosticCorrelationExplainer.Assess(Entry(
            DiagnosticOutcome.Failed,
            DiagnosticConclusion.PotentiallyRelevant,
            DiagnosticObservationStage.BeforeWindowsStarts));

        Assert.AreEqual(DiagnosticConclusion.PotentiallyRelevant, assessment.Qualification);
        StringAssert.Contains(assessment.Summary, "potentially relevant");
        StringAssert.Contains(assessment.Summary, "not a confirmed cause");
        StringAssert.Contains(assessment.EvidenceGap, "before Windows started");
        StringAssert.Contains(assessment.EvidenceGap, "cannot by themselves decide the cause");
    }

    [TestMethod]
    public void Assess_LeavesSuccessfulMultiActionObservationAsObservedRemediation()
    {
        DiagnosticCorrelationAssessment assessment = DiagnosticCorrelationExplainer.Assess(Entry(
            DiagnosticOutcome.Succeeded,
            DiagnosticConclusion.ObservedRemediation,
            DiagnosticObservationStage.BeforeWindowsStarts));

        Assert.AreEqual(DiagnosticConclusion.ObservedRemediation, assessment.Qualification);
        StringAssert.Contains(assessment.Summary, "observed remediation only");
        StringAssert.Contains(assessment.Summary, "cannot isolate one change");
    }

    [TestMethod]
    public void Assess_DoesNotInferADeviceRelationshipWhenNoDeviceWasLinked()
    {
        DiagnosticTimelineEntry entry = Entry(DiagnosticOutcome.Failed, DiagnosticConclusion.PotentiallyRelevant, DiagnosticObservationStage.Unknown) with
        {
            RelatedDevice = null
        };

        DiagnosticCorrelationAssessment assessment = DiagnosticCorrelationExplainer.Assess(entry);

        Assert.AreEqual(DiagnosticConclusion.NoConclusion, assessment.Qualification);
        StringAssert.Contains(assessment.Summary, "not used to infer a relationship");
    }

    private static DiagnosticTimelineEntry Entry(
        DiagnosticOutcome outcome,
        DiagnosticConclusion conclusion,
        DiagnosticObservationStage stage) =>
        new(
            DateTimeOffset.UnixEpoch,
            "Dock attached",
            "Restart after chipset and dock updates",
            outcome,
            "Synthetic fixture",
            conclusion,
            "Synthetic note",
            stage,
            new DiagnosticDeviceReference("Dell Dock WD19S", "an exact local display-name match", DateTimeOffset.UnixEpoch));
}
