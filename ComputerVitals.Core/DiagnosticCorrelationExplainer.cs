// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.

namespace ComputerVitals.Core;

public static class DiagnosticCorrelationExplainer
{
    public static DiagnosticCorrelationAssessment Assess(DiagnosticTimelineEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        if (entry.RelatedDevice is null)
        {
            return new DiagnosticCorrelationAssessment(
                DiagnosticConclusion.NoConclusion,
                "No local component was explicitly linked to this observation. Device presence alone is not used to infer a relationship.",
                DescribeStageEvidenceGap(entry.ObservationStage));
        }

        return entry.Outcome switch
        {
            DiagnosticOutcome.Failed => new DiagnosticCorrelationAssessment(
                DiagnosticConclusion.PotentiallyRelevant,
                $"The failed outcome was explicitly linked to {entry.RelatedDevice.DisplayName} using {entry.RelatedDevice.MatchEvidence}. This makes the device potentially relevant, not a confirmed cause.",
                DescribeStageEvidenceGap(entry.ObservationStage)),
            DiagnosticOutcome.Succeeded when entry.Conclusion == DiagnosticConclusion.ObservedRemediation => new DiagnosticCorrelationAssessment(
                DiagnosticConclusion.ObservedRemediation,
                $"A successful outcome followed the listed action or actions while {entry.RelatedDevice.DisplayName} was explicitly linked. This is an observed remediation only and cannot isolate one change as the root cause.",
                DescribeStageEvidenceGap(entry.ObservationStage)),
            _ => new DiagnosticCorrelationAssessment(
                DiagnosticConclusion.NoConclusion,
                $"{entry.RelatedDevice.DisplayName} was explicitly linked to this observation using {entry.RelatedDevice.MatchEvidence}. The recorded outcome does not establish a causal relationship.",
                DescribeStageEvidenceGap(entry.ObservationStage))
        };
    }

    private static string DescribeStageEvidenceGap(DiagnosticObservationStage stage) => stage switch
    {
        DiagnosticObservationStage.BeforeWindowsStarts => "The observation was reported before Windows started. Windows logs may be absent or incomplete for this stage and cannot by themselves decide the cause.",
        DiagnosticObservationStage.WindowsRunning => "Windows evidence may help, but it must be reconciled with its scope, detail, and observation time.",
        _ => "The observation stage is unknown, so the completeness of Windows evidence is also unknown."
    };
}
