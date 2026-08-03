// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System.Collections.ObjectModel;
using System.Windows;
using ComputerVitals.Core;

namespace ComputerVitals.Wpf;

public partial class IncidentTimelineWindow : Window
{
    private readonly SessionDiagnosticTimeline _timeline = new(capacity: 30);
    private readonly ControlledTestAssistant _controlledTests;
    private readonly ObservableCollection<TimelineEntryViewItem> _items = [];
    private readonly DiagnosticDeviceReference? _recognizedDevice;

    public IncidentTimelineWindow(HardwareInventoryItem? recognizedDevice = null)
    {
        _controlledTests = new ControlledTestAssistant(_timeline);
        InitializeComponent();
        OutcomeComboBox.ItemsSource = Enum.GetValues<DiagnosticOutcome>();
        OutcomeComboBox.SelectedItem = DiagnosticOutcome.Inconclusive;
        ConclusionComboBox.ItemsSource = Enum.GetValues<DiagnosticConclusion>()
            .Where(value => value != DiagnosticConclusion.ConfirmedRootCause)
            .ToArray();
        ConclusionComboBox.SelectedItem = DiagnosticConclusion.NoConclusion;
        ObservationStageComboBox.ItemsSource = Enum.GetValues<DiagnosticObservationStage>();
        ObservationStageComboBox.SelectedItem = DiagnosticObservationStage.Unknown;
        ControlledObservationStageComboBox.ItemsSource = Enum.GetValues<DiagnosticObservationStage>();
        ControlledObservationStageComboBox.SelectedItem = DiagnosticObservationStage.Unknown;
        ControlledOutcomeComboBox.ItemsSource = Enum.GetValues<DiagnosticOutcome>();
        ControlledOutcomeComboBox.SelectedItem = DiagnosticOutcome.Inconclusive;
        if (recognizedDevice is not null)
        {
            _recognizedDevice = new DiagnosticDeviceReference(
                recognizedDevice.DisplayName,
                "an exact local inventory match",
                recognizedDevice.IdentityEvidence.ObservedAt);
            RelatedDeviceCheckBox.Visibility = Visibility.Visible;
            RelatedDeviceText.Visibility = Visibility.Visible;
            RelatedDeviceText.Text = $"Available reference: {_recognizedDevice.DisplayName}, observed locally {_recognizedDevice.ObservedAt.ToLocalTime():g}. Select the box only when this observation involved that device. A link is not a causal claim.";
            ControlledRelatedDeviceCheckBox.Visibility = Visibility.Visible;
        }

        TimelineList.ItemsSource = _items;
    }

    private void PrepareControlledTest_Click(object sender, RoutedEventArgs e)
    {
        if (ControlledObservationStageComboBox.SelectedItem is not DiagnosticObservationStage observationStage)
        {
            StatusText.Text = "Choose the stage where the controlled-test result will be observed.";
            return;
        }

        try
        {
            _controlledTests.Prepare(new ControlledTestPlan(
                ControlledVariableTextBox.Text,
                ControlledBaselineTextBox.Text,
                ControlledConstantsTextBox.Text,
                ControlledActionTextBox.Text,
                observationStage,
                OnlyVariableWillChangeCheckBox.IsChecked == true,
                ControlledRelatedDeviceCheckBox.IsChecked == true ? _recognizedDevice : null));
            SetControlledPlanActive(true);
            CurrentControlledPlanText.Text =
                $"Prepared variable: {ControlledVariableTextBox.Text}. Keep constant: {ControlledConstantsTextBox.Text}. " +
                "Perform the listed action yourself, then record only what you observed and its evidence.";
            StatusText.Text = "Controlled test prepared in this window only. Computer Vitals has not performed the action or changed hardware.";
        }
        catch (ArgumentException exception)
        {
            StatusText.Text = exception.Message;
        }
        catch (InvalidOperationException exception)
        {
            StatusText.Text = exception.Message;
        }
    }

    private void RecordControlledResult_Click(object sender, RoutedEventArgs e)
    {
        if (ControlledOutcomeComboBox.SelectedItem is not DiagnosticOutcome outcome)
        {
            StatusText.Text = "Choose the observed result.";
            return;
        }

        try
        {
            _controlledTests.RecordResult(new ControlledTestResult(
                DateTimeOffset.Now,
                outcome,
                ControlledEvidenceSourceTextBox.Text,
                string.IsNullOrWhiteSpace(ControlledResultNoteTextBox.Text) ? null : ControlledResultNoteTextBox.Text));
            RefreshItems();
            ClearControlledPlan();
            StatusText.Text = $"Recorded one controlled-test result. Session entries: {_timeline.Entries.Count}. No root-cause claim was created.";
        }
        catch (ArgumentException exception)
        {
            StatusText.Text = exception.Message;
        }
        catch (InvalidOperationException exception)
        {
            StatusText.Text = exception.Message;
        }
    }

    private void CancelControlledTest_Click(object sender, RoutedEventArgs e)
    {
        _controlledTests.Cancel();
        ClearControlledPlan();
        StatusText.Text = "Controlled test canceled. No result was recorded.";
    }

    private void SetControlledPlanActive(bool active)
    {
        ControlledVariableTextBox.IsEnabled = !active;
        ControlledBaselineTextBox.IsEnabled = !active;
        ControlledConstantsTextBox.IsEnabled = !active;
        ControlledActionTextBox.IsEnabled = !active;
        ControlledObservationStageComboBox.IsEnabled = !active;
        ControlledRelatedDeviceCheckBox.IsEnabled = !active;
        OnlyVariableWillChangeCheckBox.IsEnabled = !active;
        PrepareControlledTestButton.IsEnabled = !active;
        ControlledResultPanel.IsEnabled = active;
    }

    private void ClearControlledPlan()
    {
        SetControlledPlanActive(false);
        ControlledVariableTextBox.Clear();
        ControlledBaselineTextBox.Clear();
        ControlledConstantsTextBox.Clear();
        ControlledActionTextBox.Clear();
        ControlledRelatedDeviceCheckBox.IsChecked = false;
        OnlyVariableWillChangeCheckBox.IsChecked = false;
        ControlledResultNoteTextBox.Clear();
        ControlledEvidenceSourceTextBox.Text = "User observation";
        ControlledOutcomeComboBox.SelectedItem = DiagnosticOutcome.Inconclusive;
        CurrentControlledPlanText.Text = "No controlled test is prepared.";
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
