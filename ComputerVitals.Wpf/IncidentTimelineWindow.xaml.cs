// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System.Collections.ObjectModel;
using System.Windows;
using ComputerVitals.Core;

namespace ComputerVitals.Wpf;

public partial class IncidentTimelineWindow : Window
{
    private readonly SessionDiagnosticTimeline _timeline = new(capacity: 30);
    private readonly ObservableCollection<TimelineEntryViewItem> _items = [];
    private readonly DiagnosticDeviceReference? _recognizedDevice;

    public IncidentTimelineWindow(HardwareInventoryItem? recognizedDevice = null)
    {
        InitializeComponent();
        OutcomeComboBox.ItemsSource = Enum.GetValues<DiagnosticOutcome>();
        OutcomeComboBox.SelectedItem = DiagnosticOutcome.Inconclusive;
        ConclusionComboBox.ItemsSource = Enum.GetValues<DiagnosticConclusion>()
            .Where(value => value != DiagnosticConclusion.ConfirmedRootCause)
            .ToArray();
        ConclusionComboBox.SelectedItem = DiagnosticConclusion.NoConclusion;
        ObservationStageComboBox.ItemsSource = Enum.GetValues<DiagnosticObservationStage>();
        ObservationStageComboBox.SelectedItem = DiagnosticObservationStage.Unknown;
        if (recognizedDevice is not null)
        {
            _recognizedDevice = new DiagnosticDeviceReference(
                recognizedDevice.DisplayName,
                "an exact local inventory match",
                recognizedDevice.IdentityEvidence.ObservedAt);
            RelatedDeviceCheckBox.Visibility = Visibility.Visible;
            RelatedDeviceText.Visibility = Visibility.Visible;
            RelatedDeviceText.Text = $"Available reference: {_recognizedDevice.DisplayName}, observed locally {_recognizedDevice.ObservedAt.ToLocalTime():g}. Select the box only when this observation involved that device. A link is not a causal claim.";
        }

        TimelineList.ItemsSource = _items;
    }

    private void Record_Click(object sender, RoutedEventArgs e)
    {
        if (OutcomeComboBox.SelectedItem is not DiagnosticOutcome outcome ||
            ConclusionComboBox.SelectedItem is not DiagnosticConclusion conclusion ||
            ObservationStageComboBox.SelectedItem is not DiagnosticObservationStage observationStage)
        {
            StatusText.Text = "Choose an outcome, observation stage, and conclusion level.";
            return;
        }

        try
        {
            _timeline.Record(new DiagnosticTimelineEntry(
                DateTimeOffset.Now,
                ConditionTextBox.Text,
                ActionTextBox.Text,
                outcome,
                EvidenceSourceTextBox.Text,
                conclusion,
                string.IsNullOrWhiteSpace(NoteTextBox.Text) ? null : NoteTextBox.Text,
                observationStage,
                RelatedDeviceCheckBox.IsChecked == true ? _recognizedDevice : null));
            RefreshItems();
            StatusText.Text = $"Recorded local observation at {DateTimeOffset.Now:T}. Session entries: {_timeline.Entries.Count}.";
            ConditionTextBox.Clear();
            ActionTextBox.Clear();
            NoteTextBox.Clear();
        }
        catch (ArgumentException exception)
        {
            StatusText.Text = exception.Message;
        }
    }

    private void RefreshItems()
    {
        _items.Clear();
        foreach (DiagnosticTimelineEntry entry in _timeline.Entries)
            _items.Add(TimelineEntryViewItem.FromEntry(entry));
    }
}

public sealed record TimelineEntryViewItem(string Headline, string Detail, string Qualification)
{
    public static TimelineEntryViewItem FromEntry(DiagnosticTimelineEntry entry)
    {
        DiagnosticCorrelationAssessment correlation = DiagnosticCorrelationExplainer.Assess(entry);
        return new TimelineEntryViewItem(
            $"{entry.ObservedAt.ToLocalTime():g} — {entry.Outcome}",
            $"Condition: {entry.Condition}\nAction: {entry.Action}\nEvidence: {entry.EvidenceSource}" +
            $"\nObservation stage: {entry.ObservationStage}" +
            (entry.RelatedDevice is null ? string.Empty : $"\nRelated local device: {entry.RelatedDevice.DisplayName}") +
            (string.IsNullOrWhiteSpace(entry.Note) ? string.Empty : $"\nNote: {entry.Note}"),
            $"{DiagnosticConclusionExplainer.Explain(entry.Conclusion)}\nCorrelation: {correlation.Summary}\nEvidence gap: {correlation.EvidenceGap}");
    }
}
