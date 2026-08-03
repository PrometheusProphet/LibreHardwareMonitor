// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.

namespace ComputerVitals.Core;

public static class LocalDiagnosticEvidenceReconciliationEngine
{
    public static LocalDiagnosticEvidenceReconciliationResult Reconcile(
        LocalDiagnosticEvidenceReconciliationInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(input.Claims);

        LocalDiagnosticEvidenceConsideration[] considerations = input.Claims
            .Select(claim => Consider(claim, input.ExplicitDeviceScope))
            .ToArray();
        LocalDiagnosticEvidenceClaim[] consideredClaims = considerations
            .Where(consideration => consideration.IsConsidered)
            .Select(consideration => consideration.Claim)
            .ToArray();

        return new LocalDiagnosticEvidenceReconciliationResult(
            considerations,
            FindUnresolvedConflicts(consideredClaims),
            DescribeScopeLimitations(considerations));
    }

    private static LocalDiagnosticEvidenceConsideration Consider(
        LocalDiagnosticEvidenceClaim claim,
        DiagnosticDeviceReference? explicitDeviceScope)
    {
        ArgumentNullException.ThrowIfNull(claim);
        Validate(claim);

        if (claim.RelatedDevice is null)
        {
            return new LocalDiagnosticEvidenceConsideration(
                claim,
                LocalDiagnosticEvidenceClaimScope.NoExplicitDeviceLink,
                true);
        }

        if (explicitDeviceScope is null || SameIdentity(claim.RelatedDevice, explicitDeviceScope))
        {
            return new LocalDiagnosticEvidenceConsideration(
                claim,
                LocalDiagnosticEvidenceClaimScope.ExactExplicitDeviceLink,
                true);
        }

        return new LocalDiagnosticEvidenceConsideration(
            claim,
            LocalDiagnosticEvidenceClaimScope.ExcludedIdentityMismatch,
            false);
    }

    private static void Validate(LocalDiagnosticEvidenceClaim claim)
    {
        if (!Enum.IsDefined(claim.ObservationKey))
            throw new ArgumentException("The observation key must be a bounded session observation value.", nameof(claim));

        bool valid = claim.SourceKind switch
        {
            LocalDiagnosticEvidenceSourceKind.DeviceManager => claim.Disposition is
                LocalDiagnosticEvidenceDisposition.ProblemReported or
                LocalDiagnosticEvidenceDisposition.NoProblemReported or
                LocalDiagnosticEvidenceDisposition.Unknown,
            LocalDiagnosticEvidenceSourceKind.UserObservation or
            LocalDiagnosticEvidenceSourceKind.InstallerSummary or
            LocalDiagnosticEvidenceSourceKind.InstallerDetail => claim.Disposition is
                LocalDiagnosticEvidenceDisposition.Succeeded or
                LocalDiagnosticEvidenceDisposition.Failed or
                LocalDiagnosticEvidenceDisposition.Unknown,
            LocalDiagnosticEvidenceSourceKind.Unknown => claim.Disposition == LocalDiagnosticEvidenceDisposition.Unknown,
            _ => false
        };

        if (!valid)
        {
            throw new ArgumentException(
                "The source kind does not permit that disposition. Device Manager reports a problem state only; no problem reported is neither healthy nor an intervention success.",
                nameof(claim));
        }
    }

    private static IReadOnlyList<LocalDiagnosticEvidenceConflict> FindUnresolvedConflicts(
        IReadOnlyList<LocalDiagnosticEvidenceClaim> claims)
    {
        List<LocalDiagnosticEvidenceConflict> conflicts = [];
        foreach (IGrouping<ConflictScope, LocalDiagnosticEvidenceClaim> group in claims
                     .Where(claim =>
                         claim.RelatedDevice is not null &&
                         NormalizeObservationKey(claim.ObservationKey) != LocalDiagnosticEvidenceObservationKey.Unknown)
                     .GroupBy(claim => ConflictScopeKey(claim)))
        {
            LocalDiagnosticEvidenceClaim[] operationClaims = group
                .Where(claim => claim.Disposition is LocalDiagnosticEvidenceDisposition.Succeeded or LocalDiagnosticEvidenceDisposition.Failed)
                .ToArray();
            if (HasBoth(operationClaims, LocalDiagnosticEvidenceDisposition.Succeeded, LocalDiagnosticEvidenceDisposition.Failed))
                conflicts.Add(new LocalDiagnosticEvidenceConflict(operationClaims));

            LocalDiagnosticEvidenceClaim[] deviceManagerClaims = group
                .Where(claim => claim.SourceKind == LocalDiagnosticEvidenceSourceKind.DeviceManager)
                .ToArray();
            if (HasBoth(deviceManagerClaims, LocalDiagnosticEvidenceDisposition.ProblemReported, LocalDiagnosticEvidenceDisposition.NoProblemReported))
                conflicts.Add(new LocalDiagnosticEvidenceConflict(deviceManagerClaims));
        }

        return conflicts;
    }

    private static IReadOnlyList<string> DescribeScopeLimitations(
        IReadOnlyList<LocalDiagnosticEvidenceConsideration> considerations)
    {
        List<string> limitations = [];
        if (considerations.Count == 0)
            limitations.Add("No claims were supplied; evidence remains unknown.");
        if (considerations.Any(consideration => consideration.Claim.Disposition == LocalDiagnosticEvidenceDisposition.Unknown))
            limitations.Add("Unknown claims remain explicit and are not resolved by another source.");
        if (considerations.Any(consideration =>
                NormalizeObservationKey(consideration.Claim.ObservationKey) == LocalDiagnosticEvidenceObservationKey.Unknown))
            limitations.Add("An unknown observation key does not establish that claims describe the same condition.");
        if (considerations.Any(consideration => consideration.Scope == LocalDiagnosticEvidenceClaimScope.ExcludedIdentityMismatch))
            limitations.Add("Claims with a mismatched explicit device identity were excluded; presence or selection is not used to infer a relationship.");
        if (considerations.Any(consideration =>
                consideration.IsConsidered &&
                consideration.Claim.ObservationStage == DiagnosticObservationStage.BeforeWindowsStarts &&
                consideration.Claim.Disposition == LocalDiagnosticEvidenceDisposition.Failed))
            limitations.Add("A failed pre-Windows claim remains outside decisive Windows scope.");
        if (considerations.Any(consideration =>
                consideration.IsConsidered &&
                consideration.Claim.SourceKind == LocalDiagnosticEvidenceSourceKind.DeviceManager &&
                consideration.Claim.Disposition == LocalDiagnosticEvidenceDisposition.NoProblemReported))
            limitations.Add("A Windows Device Manager no-problem report is scope-limited and is not a healthy-state or intervention-success claim.");

        limitations.Add("Claims are retained by source, observation time, and explicit-device scope without selecting or ranking a source.");
        limitations.Add("This boundary reports scope and unresolved conflicts only; it does not derive a conclusion.");
        return limitations;
    }

    private static bool HasBoth(
        IEnumerable<LocalDiagnosticEvidenceClaim> claims,
        LocalDiagnosticEvidenceDisposition first,
        LocalDiagnosticEvidenceDisposition second) =>
        claims.Any(claim => claim.Disposition == first) && claims.Any(claim => claim.Disposition == second);

    private static bool SameIdentity(DiagnosticDeviceReference first, DiagnosticDeviceReference second) =>
        string.Equals(first.DisplayName, second.DisplayName, StringComparison.OrdinalIgnoreCase) &&
        first.ObservedAt == second.ObservedAt;

    private static ConflictScope ConflictScopeKey(LocalDiagnosticEvidenceClaim claim) =>
        new(
            NormalizeObservationKey(claim.ObservationKey),
            claim.RelatedDevice!.DisplayName.ToUpperInvariant(),
            claim.RelatedDevice.ObservedAt);

    private static LocalDiagnosticEvidenceObservationKey NormalizeObservationKey(
        LocalDiagnosticEvidenceObservationKey key) =>
        Enum.IsDefined(key) ? key : LocalDiagnosticEvidenceObservationKey.Unknown;

    private readonly record struct ConflictScope(
        LocalDiagnosticEvidenceObservationKey ObservationKey,
        string DeviceDisplayName,
        DateTimeOffset DeviceObservedAt);
}
