// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.

namespace ComputerVitals.Core;

public static class LocalDiagnosticEvidenceClaimFactory
{
    public static bool IsValid(
        LocalDiagnosticEvidenceSourceKind sourceKind,
        LocalDiagnosticEvidenceDisposition disposition,
        DiagnosticObservationStage observationStage) =>
        Enum.IsDefined(sourceKind) &&
        Enum.IsDefined(disposition) &&
        Enum.IsDefined(observationStage) &&
        sourceKind switch
        {
            LocalDiagnosticEvidenceSourceKind.DeviceManager =>
                observationStage == DiagnosticObservationStage.WindowsRunning &&
                disposition is LocalDiagnosticEvidenceDisposition.ProblemReported or
                    LocalDiagnosticEvidenceDisposition.NoProblemReported or
                    LocalDiagnosticEvidenceDisposition.Unknown,
            LocalDiagnosticEvidenceSourceKind.Unknown =>
                observationStage == DiagnosticObservationStage.Unknown &&
                disposition == LocalDiagnosticEvidenceDisposition.Unknown,
            LocalDiagnosticEvidenceSourceKind.UserObservation or
            LocalDiagnosticEvidenceSourceKind.InstallerSummary or
            LocalDiagnosticEvidenceSourceKind.InstallerDetail =>
                disposition is LocalDiagnosticEvidenceDisposition.Succeeded or
                    LocalDiagnosticEvidenceDisposition.Failed or
                    LocalDiagnosticEvidenceDisposition.Unknown,
            _ => false
        };

    public static LocalDiagnosticEvidenceClaim Create(
        LocalDiagnosticEvidenceObservationKey observationKey,
        LocalDiagnosticEvidenceSourceKind sourceKind,
        LocalDiagnosticEvidenceDisposition disposition,
        DateTimeOffset observedAt,
        DiagnosticObservationStage observationStage,
        DiagnosticDeviceReference? relatedDevice = null)
    {
        if (!Enum.IsDefined(observationKey) || observationKey == LocalDiagnosticEvidenceObservationKey.Unknown)
            throw new ArgumentOutOfRangeException(nameof(observationKey));
        if (!IsValid(sourceKind, disposition, observationStage))
            throw new ArgumentException("The selected source, disposition, and stage are not a valid typed claim.");

        return new LocalDiagnosticEvidenceClaim(
            observationKey,
            sourceKind,
            disposition,
            observedAt,
            observationStage,
            relatedDevice);
    }
}
