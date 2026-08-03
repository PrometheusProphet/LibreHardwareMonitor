// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.

using ComputerVitals.Core;

namespace ComputerVitals.Core.Tests;

[TestClass]
public sealed class SessionDiagnosticTimelineTests
{
    [TestMethod]
    public void Record_KeepsNewestEntriesWithinTheBoundedSession()
    {
        SessionDiagnosticTimeline timeline = new(capacity: 2);
        timeline.Record(Entry(DateTimeOffset.UnixEpoch.AddMinutes(1)));
        timeline.Record(Entry(DateTimeOffset.UnixEpoch.AddMinutes(3)));
        timeline.Record(Entry(DateTimeOffset.UnixEpoch.AddMinutes(2)));

        Assert.HasCount(2, timeline.Entries);
        Assert.AreEqual(DateTimeOffset.UnixEpoch.AddMinutes(3), timeline.Entries[0].ObservedAt);
        Assert.AreEqual(DateTimeOffset.UnixEpoch.AddMinutes(2), timeline.Entries[1].ObservedAt);
    }

    [TestMethod]
    public void Record_RequiresSuccessBeforeObservedRemediation()
    {
        SessionDiagnosticTimeline timeline = new(capacity: 5);

        Assert.Throws<ArgumentException>(() => timeline.Record(
            Entry(DateTimeOffset.UnixEpoch, DiagnosticOutcome.Failed, DiagnosticConclusion.ObservedRemediation)));
    }

    [TestMethod]
    public void Record_RefusesAConfirmedRootCauseClaim()
    {
        SessionDiagnosticTimeline timeline = new(capacity: 5);

        Assert.Throws<ArgumentException>(() => timeline.Record(
            Entry(DateTimeOffset.UnixEpoch, DiagnosticOutcome.Succeeded, DiagnosticConclusion.ConfirmedRootCause)));
    }

    [TestMethod]
    public void Record_AllocatesUniqueStableKeysWithoutDerivingThemFromEntryContent()
    {
        SessionDiagnosticTimeline timeline = new(capacity: 3);
        DiagnosticTimelineEntry first = timeline.Record(Entry(DateTimeOffset.UnixEpoch.AddMinutes(1)));
        DiagnosticTimelineEntry second = timeline.Record(Entry(DateTimeOffset.UnixEpoch.AddMinutes(3)) with
        {
            Condition = first.Condition,
            Action = first.Action,
            RelatedDevice = new DiagnosticDeviceReference("Synthetic device", "Synthetic explicit reference", DateTimeOffset.UnixEpoch)
        });
        timeline.Record(Entry(DateTimeOffset.UnixEpoch.AddMinutes(2)));

        Assert.AreNotEqual(LocalDiagnosticEvidenceObservationKey.Unknown, first.ObservationKey);
        Assert.AreNotEqual(LocalDiagnosticEvidenceObservationKey.Unknown, second.ObservationKey);
        Assert.AreNotEqual(first.ObservationKey, second.ObservationKey);
        Assert.AreSame(second, timeline.Entries[0]);
        Assert.AreEqual(second.ObservationKey, timeline.Entries[0].ObservationKey);
    }

    [TestMethod]
    public void Record_EvictsOldKeyAndMakesItsSlotAvailableAgain()
    {
        SessionDiagnosticTimeline timeline = new(capacity: 2);
        DiagnosticTimelineEntry first = timeline.Record(Entry(DateTimeOffset.UnixEpoch.AddMinutes(1)));
        DiagnosticTimelineEntry second = timeline.Record(Entry(DateTimeOffset.UnixEpoch.AddMinutes(3)));
        DiagnosticTimelineEntry third = timeline.Record(Entry(DateTimeOffset.UnixEpoch.AddMinutes(2)));

        Assert.IsFalse(timeline.Entries.Contains(first));
        Assert.IsTrue(timeline.Entries.Contains(second));
        Assert.IsTrue(timeline.Entries.Contains(third));
        Assert.AreEqual(first.ObservationKey, third.ObservationKey);
        Assert.AreEqual(2, timeline.Entries.Select(entry => entry.ObservationKey).Distinct().Count());

        DiagnosticTimelineEntry replacement = timeline.Record(Entry(DateTimeOffset.UnixEpoch.AddMinutes(4)));
        Assert.AreEqual(third.ObservationKey, replacement.ObservationKey);
    }

    private static DiagnosticTimelineEntry Entry(
        DateTimeOffset observedAt,
        DiagnosticOutcome outcome = DiagnosticOutcome.Inconclusive,
        DiagnosticConclusion conclusion = DiagnosticConclusion.NoConclusion) =>
        new(
            observedAt,
            "Dock attached",
            "Restarted Windows",
            outcome,
            "Synthetic fixture",
            conclusion,
            "Synthetic note");
}
