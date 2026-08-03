// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.

namespace ComputerVitals.Core;

public sealed class SessionDiagnosticEvidenceClaimLedger
{
    public const int MaximumClaimCount = 30;
    private readonly SessionDiagnosticTimeline _timeline;
    private readonly List<LocalDiagnosticEvidenceClaim> _claims = [];

    public SessionDiagnosticEvidenceClaimLedger(SessionDiagnosticTimeline timeline)
    {
        ArgumentNullException.ThrowIfNull(timeline);
        _timeline = timeline;
    }

    public IReadOnlyList<LocalDiagnosticEvidenceClaim> Claims
    {
        get
        {
            PruneInactive();
            return _claims;
        }
    }

    public void Record(LocalDiagnosticEvidenceClaim claim)
    {
        ArgumentNullException.ThrowIfNull(claim);
        PruneInactive();
        if (claim.ObservationKey == LocalDiagnosticEvidenceObservationKey.Unknown ||
            !_timeline.TryGetRetainedEntry(claim.ObservationKey, out DiagnosticTimelineEntry? observation))
        {
            throw new ArgumentException("A claim must use an active retained observation key.", nameof(claim));
        }
        if (claim.RelatedDevice is not null && !ReferenceEquals(claim.RelatedDevice, observation!.RelatedDevice))
        {
            throw new ArgumentException("A claim may copy only the retained observation's existing explicit device reference.", nameof(claim));
        }
        if (!LocalDiagnosticEvidenceClaimFactory.IsValid(
                claim.SourceKind,
                claim.Disposition,
                claim.ObservationStage))
        {
            throw new ArgumentException("The claim does not use a valid typed source, disposition, and stage combination.", nameof(claim));
        }

        _claims.Add(claim);
        while (_claims.Count > MaximumClaimCount)
            _claims.RemoveAt(0);
    }

    public IReadOnlyList<LocalDiagnosticEvidenceClaim> ClaimsFor(
        LocalDiagnosticEvidenceObservationKey observationKey)
    {
        PruneInactive();
        if (observationKey == LocalDiagnosticEvidenceObservationKey.Unknown ||
            !_timeline.IsRetainedObservationKey(observationKey))
        {
            return [];
        }

        return _claims.Where(claim => claim.ObservationKey == observationKey).ToArray();
    }

    public void PruneInactive() =>
        _claims.RemoveAll(claim => !_timeline.IsRetainedObservationKey(claim.ObservationKey));
}
