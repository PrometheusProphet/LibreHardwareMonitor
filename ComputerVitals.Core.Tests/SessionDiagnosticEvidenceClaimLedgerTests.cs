// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.

using ComputerVitals.Core;

namespace ComputerVitals.Core.Tests;

[TestClass]
public sealed class SessionDiagnosticEvidenceClaimLedgerTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 3, 22, 0, 0, TimeSpan.Zero);

    [TestMethod]
    public void Record_RejectsUnknownAndInactiveObservationKeys()
    {
        SessionDiagnosticTimeline timeline = new(capacity: 1);
        SessionDiagnosticEvidenceClaimLedger ledger = new(timeline);
        DiagnosticTimelineEntry retained = timeline.Record(Entry(Now));
        LocalDiagnosticEvidenceClaim unknown = new(
            LocalDiagnosticEvidenceObservationKey.Unknown,
            LocalDiagnosticEvidenceSourceKind.UserObservation,
            LocalDiagnosticEvidenceDisposition.Unknown,
            Now,
            DiagnosticObservationStage.Unknown);
        LocalDiagnosticEvidenceClaim inactive = new(
            LocalDiagnosticEvidenceObservationKey.Observation30,
            LocalDiagnosticEvidenceSourceKind.UserObservation,
            LocalDiagnosticEvidenceDisposition.Unknown,
            Now,
            DiagnosticObservationStage.Unknown);

        Assert.Throws<ArgumentException>(() => ledger.Record(unknown));
        Assert.Throws<ArgumentException>(() => ledger.Record(inactive));
        ledger.Record(Claim(retained.ObservationKey));
        Assert.HasCount(1, ledger.Claims);
    }

    [TestMethod]
    public void Claims_ArePrunedWhenTheirTimelineEntryIsEvicted()
    {
        SessionDiagnosticTimeline timeline = new(capacity: 1);
        SessionDiagnosticEvidenceClaimLedger ledger = new(timeline);
        DiagnosticTimelineEntry first = timeline.Record(Entry(Now));
        ledger.Record(Claim(first.ObservationKey));

        timeline.Record(Entry(Now.AddMinutes(1)));

        Assert.IsEmpty(ledger.Claims);
        Assert.IsEmpty(ledger.ClaimsFor(first.ObservationKey));
    }

    [TestMethod]
    public void Record_AtCapacityThirtyEvictsAndPrunesBeforeReusingOnlyTheOldestKey()
    {
        SessionDiagnosticTimeline timeline = new(capacity: SessionDiagnosticTimeline.MaximumCapacity);
        SessionDiagnosticEvidenceClaimLedger ledger = new(timeline);
        DiagnosticTimelineEntry oldest = timeline.Record(Entry(Now));
        ledger.Record(Claim(oldest.ObservationKey));
        for (int offset = 1; offset < SessionDiagnosticTimeline.MaximumCapacity; offset++)
            timeline.Record(Entry(Now.AddMinutes(offset)));

        DiagnosticTimelineEntry replacement = timeline.Record(Entry(Now.AddMinutes(SessionDiagnosticTimeline.MaximumCapacity)));

        Assert.HasCount(SessionDiagnosticTimeline.MaximumCapacity, timeline.Entries);
        Assert.IsFalse(timeline.Entries.Contains(oldest));
        Assert.IsTrue(timeline.Entries.Contains(replacement));
        Assert.AreEqual(oldest.ObservationKey, replacement.ObservationKey);
        Assert.AreEqual(
            SessionDiagnosticTimeline.MaximumCapacity,
            timeline.Entries.Select(entry => entry.ObservationKey).Distinct().Count());
        Assert.AreEqual(Now.AddMinutes(1), timeline.Entries[^1].ObservedAt);
        Assert.IsEmpty(ledger.Claims);
    }

    [TestMethod]
    public void Record_RetainsAtMostThirtyClaimsWithinOneLedgerInstance()
    {
        SessionDiagnosticTimeline timeline = new(capacity: 1);
        SessionDiagnosticEvidenceClaimLedger ledger = new(timeline);
        DiagnosticTimelineEntry entry = timeline.Record(Entry(Now));

        for (int offset = 0; offset < SessionDiagnosticEvidenceClaimLedger.MaximumClaimCount + 1; offset++)
            ledger.Record(Claim(entry.ObservationKey, Now.AddMinutes(offset)));

        Assert.HasCount(SessionDiagnosticEvidenceClaimLedger.MaximumClaimCount, ledger.Claims);
        Assert.AreEqual(Now.AddMinutes(1), ledger.Claims[0].ObservedAt);

        SessionDiagnosticEvidenceClaimLedger otherSessionLedger = new(timeline);
        Assert.IsEmpty(otherSessionLedger.Claims);
    }

    [TestMethod]
    public void Create_MapsOnlyValidEnumCombinationsAndCopiesAnExplicitDeviceExactly()
    {
        DiagnosticDeviceReference device = new("Synthetic dock", "Synthetic explicit reference", Now);
        LocalDiagnosticEvidenceClaim claim = LocalDiagnosticEvidenceClaimFactory.Create(
            LocalDiagnosticEvidenceObservationKey.Observation1,
            LocalDiagnosticEvidenceSourceKind.DeviceManager,
            LocalDiagnosticEvidenceDisposition.NoProblemReported,
            Now,
            DiagnosticObservationStage.WindowsRunning,
            device);

        Assert.AreSame(device, claim.RelatedDevice);
        Assert.IsTrue(LocalDiagnosticEvidenceClaimFactory.IsValid(
            LocalDiagnosticEvidenceSourceKind.UserObservation,
            LocalDiagnosticEvidenceDisposition.Failed,
            DiagnosticObservationStage.BeforeWindowsStarts));
        Assert.IsTrue(LocalDiagnosticEvidenceClaimFactory.IsValid(
            LocalDiagnosticEvidenceSourceKind.InstallerSummary,
            LocalDiagnosticEvidenceDisposition.Succeeded,
            DiagnosticObservationStage.WindowsRunning));
        Assert.IsTrue(LocalDiagnosticEvidenceClaimFactory.IsValid(
            LocalDiagnosticEvidenceSourceKind.InstallerDetail,
            LocalDiagnosticEvidenceDisposition.Unknown,
            DiagnosticObservationStage.Unknown));
        Assert.IsTrue(LocalDiagnosticEvidenceClaimFactory.IsValid(
            LocalDiagnosticEvidenceSourceKind.Unknown,
            LocalDiagnosticEvidenceDisposition.Unknown,
            DiagnosticObservationStage.Unknown));
        Assert.IsFalse(LocalDiagnosticEvidenceClaimFactory.IsValid(
            LocalDiagnosticEvidenceSourceKind.DeviceManager,
            LocalDiagnosticEvidenceDisposition.Succeeded,
            DiagnosticObservationStage.WindowsRunning));
        Assert.IsFalse(LocalDiagnosticEvidenceClaimFactory.IsValid(
            LocalDiagnosticEvidenceSourceKind.Unknown,
            LocalDiagnosticEvidenceDisposition.Unknown,
            DiagnosticObservationStage.WindowsRunning));
    }

    [TestMethod]
    public void Record_RejectsADeviceReferenceThatWasNotExplicitlyOnTheRetainedObservation()
    {
        SessionDiagnosticTimeline timeline = new(capacity: 1);
        SessionDiagnosticEvidenceClaimLedger ledger = new(timeline);
        DiagnosticTimelineEntry retained = timeline.Record(Entry(Now));
        DiagnosticDeviceReference unrelated = new("Different synthetic device", "Synthetic reference", Now);
        LocalDiagnosticEvidenceClaim claim = LocalDiagnosticEvidenceClaimFactory.Create(
            retained.ObservationKey,
            LocalDiagnosticEvidenceSourceKind.UserObservation,
            LocalDiagnosticEvidenceDisposition.Unknown,
            Now,
            DiagnosticObservationStage.Unknown,
            unrelated);

        ArgumentException exception = Assert.Throws<ArgumentException>(() => ledger.Record(claim));

        StringAssert.Contains(exception.Message, "existing explicit device reference");
    }

    [TestMethod]
    public void ClaimsFor_SelectedKeyFiltersOtherObservationsBeforeReconciliation()
    {
        SessionDiagnosticTimeline timeline = new(capacity: 3);
        SessionDiagnosticEvidenceClaimLedger ledger = new(timeline);
        DiagnosticDeviceReference device = new("Synthetic dock", "Synthetic explicit reference", Now);
        DiagnosticTimelineEntry selected = timeline.Record(Entry(Now, device));
        DiagnosticTimelineEntry other = timeline.Record(Entry(Now.AddMinutes(1), device));
        ledger.Record(LocalDiagnosticEvidenceClaimFactory.Create(
            selected.ObservationKey,
            LocalDiagnosticEvidenceSourceKind.InstallerSummary,
            LocalDiagnosticEvidenceDisposition.Failed,
            Now,
            DiagnosticObservationStage.WindowsRunning,
            selected.RelatedDevice));
        ledger.Record(LocalDiagnosticEvidenceClaimFactory.Create(
            other.ObservationKey,
            LocalDiagnosticEvidenceSourceKind.InstallerDetail,
            LocalDiagnosticEvidenceDisposition.Succeeded,
            Now.AddMinutes(1),
            DiagnosticObservationStage.WindowsRunning,
            other.RelatedDevice));

        LocalDiagnosticEvidenceReconciliationResult selectedResult = LocalDiagnosticEvidenceReconciliationEngine.Reconcile(
            new LocalDiagnosticEvidenceReconciliationInput(ledger.ClaimsFor(selected.ObservationKey), selected.RelatedDevice));
        Assert.HasCount(1, selectedResult.ClaimConsiderations);
        Assert.IsEmpty(selectedResult.UnresolvedConflicts);

        ledger.Record(LocalDiagnosticEvidenceClaimFactory.Create(
            selected.ObservationKey,
            LocalDiagnosticEvidenceSourceKind.InstallerDetail,
            LocalDiagnosticEvidenceDisposition.Succeeded,
            Now.AddMinutes(2),
            DiagnosticObservationStage.WindowsRunning,
            selected.RelatedDevice));
        LocalDiagnosticEvidenceReconciliationResult sameKeyResult = LocalDiagnosticEvidenceReconciliationEngine.Reconcile(
            new LocalDiagnosticEvidenceReconciliationInput(ledger.ClaimsFor(selected.ObservationKey), selected.RelatedDevice));
        Assert.HasCount(1, sameKeyResult.UnresolvedConflicts);
    }

    private static DiagnosticTimelineEntry Entry(DateTimeOffset observedAt, DiagnosticDeviceReference? device = null) =>
        new(
            observedAt,
            "Synthetic condition",
            "Synthetic user observation",
            DiagnosticOutcome.Unknown,
            "Synthetic evidence",
            DiagnosticConclusion.NoConclusion,
            ObservationStage: DiagnosticObservationStage.Unknown,
            RelatedDevice: device);

    private static LocalDiagnosticEvidenceClaim Claim(
        LocalDiagnosticEvidenceObservationKey key,
        DateTimeOffset? observedAt = null) =>
        LocalDiagnosticEvidenceClaimFactory.Create(
            key,
            LocalDiagnosticEvidenceSourceKind.UserObservation,
            LocalDiagnosticEvidenceDisposition.Unknown,
            observedAt ?? Now,
            DiagnosticObservationStage.Unknown);
}
