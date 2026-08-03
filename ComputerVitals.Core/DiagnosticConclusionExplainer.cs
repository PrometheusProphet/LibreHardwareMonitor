// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.

namespace ComputerVitals.Core;

public static class DiagnosticConclusionExplainer
{
    public static string Explain(DiagnosticConclusion conclusion) => conclusion switch
    {
        DiagnosticConclusion.NoConclusion => "No conclusion recorded.",
        DiagnosticConclusion.PotentiallyRelevant => "Potentially relevant to the symptom; not a causal claim.",
        DiagnosticConclusion.ObservedRemediation => "Observed remediation after the listed actions; not proof that one action was the root cause.",
        DiagnosticConclusion.ConfirmedRootCause => "Confirmed root cause requires scoped, independent evidence and is not recorded by this session timeline.",
        _ => "Conclusion is unknown."
    };
}
