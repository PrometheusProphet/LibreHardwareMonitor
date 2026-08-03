// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.

namespace ComputerVitals.Core;

public sealed class SessionDiagnosticTimeline
{
    private const int MaximumTextLength = 500;
    public const int MaximumCapacity = 30;
    private static readonly LocalDiagnosticEvidenceObservationKey[] ObservationKeys = Enum
        .GetValues<LocalDiagnosticEvidenceObservationKey>()
        .Where(key => key != LocalDiagnosticEvidenceObservationKey.Unknown)
        .ToArray();
    private readonly int _capacity;
    private readonly List<DiagnosticTimelineEntry> _entries = [];

    public SessionDiagnosticTimeline(int capacity)
    {
        if (capacity <= 0 || capacity > MaximumCapacity)
            throw new ArgumentOutOfRangeException(nameof(capacity));

        _capacity = capacity;
    }

    public IReadOnlyList<DiagnosticTimelineEntry> Entries => _entries;

    public event Action<DiagnosticTimelineEntry>? EntryEvicted;

    public DiagnosticTimelineEntry Record(DiagnosticTimelineEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        if (entry.ObservationKey != LocalDiagnosticEvidenceObservationKey.Unknown)
            throw new ArgumentException("The session timeline allocates observation keys for retained entries.", nameof(entry));
        ValidateText(entry.Condition, nameof(entry.Condition));
        ValidateText(entry.Action, nameof(entry.Action));
        ValidateText(entry.EvidenceSource, nameof(entry.EvidenceSource));
        if (entry.Note is not null && entry.Note.Length > MaximumTextLength)
            throw new ArgumentOutOfRangeException(nameof(entry.Note), $"Entries are limited to {MaximumTextLength} characters per field.");
        if (entry.RelatedDevice is not null)
        {
            ValidateText(entry.RelatedDevice.DisplayName, nameof(entry.RelatedDevice.DisplayName));
            ValidateText(entry.RelatedDevice.MatchEvidence, nameof(entry.RelatedDevice.MatchEvidence));
        }

        if (entry.Conclusion == DiagnosticConclusion.ConfirmedRootCause)
        {
            throw new ArgumentException(
                "A session timeline cannot establish a confirmed root cause. Record the observation and its evidence instead.",
                nameof(entry));
        }

        if (entry.Conclusion == DiagnosticConclusion.ObservedRemediation && entry.Outcome != DiagnosticOutcome.Succeeded)
        {
            throw new ArgumentException(
                "Observed remediation requires a successful outcome and still does not establish root cause.",
                nameof(entry));
        }

        if (_entries.Count == _capacity)
        {
            DiagnosticTimelineEntry oldestEntry = _entries[^1];
            if (entry.ObservedAt <= oldestEntry.ObservedAt)
                return entry;

            _entries.RemoveAt(_entries.Count - 1);
            EntryEvicted?.Invoke(oldestEntry);
            DiagnosticTimelineEntry replacementEntry = entry with { ObservationKey = oldestEntry.ObservationKey };
            _entries.Add(replacementEntry);
            _entries.Sort((left, right) => right.ObservedAt.CompareTo(left.ObservedAt));
            return replacementEntry;
        }

        DiagnosticTimelineEntry retainedEntry = entry with { ObservationKey = AllocateObservationKey() };
        _entries.Add(retainedEntry);
        _entries.Sort((left, right) => right.ObservedAt.CompareTo(left.ObservedAt));
        return retainedEntry;
    }

    public bool IsRetainedObservationKey(LocalDiagnosticEvidenceObservationKey key) =>
        key != LocalDiagnosticEvidenceObservationKey.Unknown &&
        _entries.Any(entry => entry.ObservationKey == key);

    public bool TryGetRetainedEntry(
        LocalDiagnosticEvidenceObservationKey key,
        out DiagnosticTimelineEntry? entry)
    {
        entry = _entries.FirstOrDefault(candidate => candidate.ObservationKey == key);
        return entry is not null;
    }

    private LocalDiagnosticEvidenceObservationKey AllocateObservationKey() =>
        ObservationKeys.FirstOrDefault(key => !IsRetainedObservationKey(key)) is LocalDiagnosticEvidenceObservationKey key &&
        key != LocalDiagnosticEvidenceObservationKey.Unknown
            ? key
            : throw new InvalidOperationException("No session observation key is available.");

    private static void ValidateText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("A concise value is required.", parameterName);

        if (value.Length > MaximumTextLength)
            throw new ArgumentOutOfRangeException(parameterName, $"Entries are limited to {MaximumTextLength} characters per field.");
    }
}
