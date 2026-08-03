// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.

namespace ComputerVitals.Core;

public sealed class SessionDiagnosticTimeline
{
    private const int MaximumTextLength = 500;
    private readonly int _capacity;
    private readonly List<DiagnosticTimelineEntry> _entries = [];

    public SessionDiagnosticTimeline(int capacity)
    {
        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity));

        _capacity = capacity;
    }

    public IReadOnlyList<DiagnosticTimelineEntry> Entries => _entries;

    public void Record(DiagnosticTimelineEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ValidateText(entry.Condition, nameof(entry.Condition));
        ValidateText(entry.Action, nameof(entry.Action));
        ValidateText(entry.EvidenceSource, nameof(entry.EvidenceSource));
        if (entry.Note is not null && entry.Note.Length > MaximumTextLength)
            throw new ArgumentOutOfRangeException(nameof(entry.Note), $"Entries are limited to {MaximumTextLength} characters per field.");

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

        _entries.Add(entry);
        _entries.Sort((left, right) => right.ObservedAt.CompareTo(left.ObservedAt));
        while (_entries.Count > _capacity)
            _entries.RemoveAt(_entries.Count - 1);
    }

    private static void ValidateText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("A concise value is required.", parameterName);

        if (value.Length > MaximumTextLength)
            throw new ArgumentOutOfRangeException(parameterName, $"Entries are limited to {MaximumTextLength} characters per field.");
    }
}
