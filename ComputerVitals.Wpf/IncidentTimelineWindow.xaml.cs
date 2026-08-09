// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using ComputerVitals.Core;

namespace ComputerVitals.Wpf;

public partial class IncidentTimelineWindow : Window
{
    private readonly SessionDiagnosticTimeline _timeline = new(capacity: 30);
    private readonly ControlledTestAssistant _controlledTests;
    private readonly SessionDiagnosticEvidenceClaimLedger _claimLedger;
    private readonly ObservableCollection<TimelineEntryViewItem> _items = [];
    private readonly IReadOnlyList<TemperatureSample> _temperatureSamples;
    private readonly HardwareInventoryItem? _inventoryItem;
    private readonly DiagnosticDeviceReference? _availableDevice;

    public IncidentTimelineWindow(
        IReadOnlyList<TemperatureSample> temperatureSamples,
        HardwareInventoryItem? inventoryItem = null)
    {
        ArgumentNullException.ThrowIfNull(temperatureSamples);
        _temperatureSamples = temperatureSamples.ToArray();
        _inventoryItem = inventoryItem;
        _controlledTests = new ControlledTestAssistant(_timeline);
        _claimLedger = new SessionDiagnosticEvidenceClaimLedger(_timeline);
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
        ClaimSourceComboBox.ItemsSource = Enum.GetValues<LocalDiagnosticEvidenceSourceKind>();
        ClaimSourceComboBox.SelectedItem = LocalDiagnosticEvidenceSourceKind.UserObservation;
        if (inventoryItem is not null)
        {
            _availableDevice = new DiagnosticDeviceReference(
                inventoryItem.DisplayName,
                "the available local inventory reference",
                inventoryItem.IdentityEvidence.ObservedAt);
            RelatedDeviceCheckBox.Visibility = Visibility.Visible;
            RelatedDeviceText.Visibility = Visibility.Visible;
            RelatedDeviceText.Text = $"Available reference: {_availableDevice.DisplayName}, observed locally {_availableDevice.ObservedAt.ToLocalTime():g}. Select the box only when this observation involved that device. A link is not a causal claim.";
            ControlledRelatedDeviceCheckBox.Visibility = Visibility.Visible;
        }

        TimelineList.ItemsSource = _items;
        ConfigureClaimInputs();
        UpdateRecommendation(null);
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
                ControlledRelatedDeviceCheckBox.IsChecked == true ? _availableDevice : null));
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
            _claimLedger.PruneInactive();
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
                RelatedDeviceCheckBox.IsChecked == true ? _availableDevice : null));
            _claimLedger.PruneInactive();
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
        _claimLedger.PruneInactive();
        _items.Clear();
        foreach (DiagnosticTimelineEntry entry in _timeline.Entries)
            _items.Add(TimelineEntryViewItem.FromEntry(entry));

        if (_items.Count > 0)
            TimelineList.SelectedIndex = 0;
        else
            UpdateRecommendation(null);
    }

    private void TimelineList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) =>
        UpdateRecommendation((TimelineList.SelectedItem as TimelineEntryViewItem)?.Entry);

    private void ClaimSourceComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) =>
        ConfigureClaimInputs();

    private void ClaimInput_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) =>
        UpdateClaimRecordState();

    private void ConfigureClaimInputs()
    {
        LocalDiagnosticEvidenceSourceKind sourceKind = ClaimSourceComboBox.SelectedItem is LocalDiagnosticEvidenceSourceKind selected
            ? selected
            : LocalDiagnosticEvidenceSourceKind.Unknown;
        LocalDiagnosticEvidenceDisposition[] dispositions = sourceKind switch
        {
            LocalDiagnosticEvidenceSourceKind.DeviceManager =>
            [
                LocalDiagnosticEvidenceDisposition.ProblemReported,
                LocalDiagnosticEvidenceDisposition.NoProblemReported,
                LocalDiagnosticEvidenceDisposition.Unknown
            ],
            LocalDiagnosticEvidenceSourceKind.Unknown => [LocalDiagnosticEvidenceDisposition.Unknown],
            _ =>
            [
                LocalDiagnosticEvidenceDisposition.Succeeded,
                LocalDiagnosticEvidenceDisposition.Failed,
                LocalDiagnosticEvidenceDisposition.Unknown
            ]
        };
        DiagnosticObservationStage[] stages = sourceKind switch
        {
            LocalDiagnosticEvidenceSourceKind.DeviceManager => [DiagnosticObservationStage.WindowsRunning],
            LocalDiagnosticEvidenceSourceKind.Unknown => [DiagnosticObservationStage.Unknown],
            _ => Enum.GetValues<DiagnosticObservationStage>()
        };

        ClaimDispositionComboBox.ItemsSource = dispositions;
        ClaimDispositionComboBox.SelectedItem = dispositions[0];
        ClaimObservationStageComboBox.ItemsSource = stages;
        ClaimObservationStageComboBox.IsEnabled = stages.Length > 1;
        TimelineEntryViewItem? selectedObservation = TimelineList.SelectedItem as TimelineEntryViewItem;
        ClaimObservationStageComboBox.SelectedItem = stages.Contains(selectedObservation?.Entry.ObservationStage ?? DiagnosticObservationStage.Unknown)
            ? selectedObservation?.Entry.ObservationStage ?? DiagnosticObservationStage.Unknown
            : stages[0];
        UpdateClaimRecordState();
    }

    private void UpdateClaimRecordState()
    {
        DiagnosticTimelineEntry? selectedObservation = (TimelineList.SelectedItem as TimelineEntryViewItem)?.Entry;
        bool hasRetainedSelection = selectedObservation is not null &&
            _timeline.IsRetainedObservationKey(selectedObservation.ObservationKey);
        bool valid = hasRetainedSelection &&
            ClaimSourceComboBox.SelectedItem is LocalDiagnosticEvidenceSourceKind sourceKind &&
            ClaimDispositionComboBox.SelectedItem is LocalDiagnosticEvidenceDisposition disposition &&
            ClaimObservationStageComboBox.SelectedItem is DiagnosticObservationStage stage &&
            LocalDiagnosticEvidenceClaimFactory.IsValid(sourceKind, disposition, stage);

        RecordTypedClaimButton.IsEnabled = valid;
        ClaimSelectionText.Text = hasRetainedSelection
            ? "The typed claim will use this selected session observation and copy only its already explicit device reference, if present."
            : "Select a retained observation before recording a typed claim.";
    }

    private void RecordTypedClaim_Click(object sender, RoutedEventArgs e)
    {
        if ((TimelineList.SelectedItem as TimelineEntryViewItem)?.Entry is not DiagnosticTimelineEntry selectedObservation ||
            !_timeline.IsRetainedObservationKey(selectedObservation.ObservationKey) ||
            ClaimSourceComboBox.SelectedItem is not LocalDiagnosticEvidenceSourceKind sourceKind ||
            ClaimDispositionComboBox.SelectedItem is not LocalDiagnosticEvidenceDisposition disposition ||
            ClaimObservationStageComboBox.SelectedItem is not DiagnosticObservationStage stage ||
            !LocalDiagnosticEvidenceClaimFactory.IsValid(sourceKind, disposition, stage))
        {
            StatusText.Text = "Select a retained observation and a valid typed source combination.";
            UpdateClaimRecordState();
            return;
        }

        try
        {
            LocalDiagnosticEvidenceClaim claim = LocalDiagnosticEvidenceClaimFactory.Create(
                selectedObservation.ObservationKey,
                sourceKind,
                disposition,
                DateTimeOffset.Now,
                stage,
                selectedObservation.RelatedDevice);
            _claimLedger.Record(claim);
            UpdateRecommendation(selectedObservation);
            StatusText.Text = "Recorded one typed source claim for the selected session observation.";
        }
        catch (ArgumentException exception)
        {
            StatusText.Text = exception.Message;
        }
    }

    private void UpdateRecommendation(DiagnosticTimelineEntry? selectedObservation)
    {
        OfficialVendorUpdateGuidance? vendorGuidance = _inventoryItem is null
            ? null
            : OfficialVendorUpdateGuidanceResolver.Resolve(_inventoryItem);
        LocalDiagnosticRecommendation recommendation = LocalDiagnosticRecommendationEngine.Create(
            new LocalDiagnosticRecommendationInput(
                selectedObservation,
                _timeline.Entries.ToArray(),
                _temperatureSamples.ToArray(),
                _inventoryItem,
                vendorGuidance));

        RecommendationObservationText.Text = recommendation.Observation;
        RecommendationWhyText.Text = $"Why it may matter\n{recommendation.WhyItMayMatter}";
        RecommendationConfidenceText.Text = $"Confidence: {recommendation.Confidence}";
        RecommendationBasisText.Text = recommendation.ConfidenceBasis;
        RecommendationEvidenceText.Text = $"Evidence considered\n{recommendation.EvidenceConsidered}";
        RecommendationUnknownsText.Text = $"Conflicts and unknowns\n{recommendation.ConflictsAndUnknowns}";
        RecommendationNextText.Text = $"Safest next observation\n{recommendation.SafestNextObservation}";
        UpdateSourceReconciliation(selectedObservation);
        UpdateClaimRecordState();
    }

    private void UpdateSourceReconciliation(DiagnosticTimelineEntry? selectedObservation)
    {
        if (selectedObservation is null || !_timeline.IsRetainedObservationKey(selectedObservation.ObservationKey))
        {
            SourceReconciliationText.Text = "No observation is selected. No typed source claims are considered.";
            return;
        }

        IReadOnlyList<LocalDiagnosticEvidenceClaim> claims = _claimLedger.ClaimsFor(selectedObservation.ObservationKey);
        LocalDiagnosticEvidenceReconciliationResult reconciliation = LocalDiagnosticEvidenceReconciliationEngine.Reconcile(
            new LocalDiagnosticEvidenceReconciliationInput(claims, selectedObservation.RelatedDevice));
        StringBuilder text = new();
        if (reconciliation.ClaimConsiderations.Count == 0)
        {
            text.Append("No typed source claims are recorded for this selected observation.");
        }
        else
        {
            foreach (LocalDiagnosticEvidenceConsideration consideration in reconciliation.ClaimConsiderations)
            {
                LocalDiagnosticEvidenceClaim claim = consideration.Claim;
                text.AppendLine(
                    $"Source: {claim.SourceKind}; disposition: {claim.Disposition}; recorded: {claim.ObservedAt.ToLocalTime():g}; " +
                    $"stage: {claim.ObservationStage}; scope: {DescribeClaimScope(consideration.Scope)}.");
            }
        }

        text.AppendLine(reconciliation.UnresolvedConflicts.Count == 0
            ? "Unresolved conflicts: none."
            : $"Unresolved conflicts: {reconciliation.UnresolvedConflicts.Count}. No source is selected or ranked.");
        foreach (string limitation in reconciliation.ScopeLimitations)
            text.AppendLine($"Scope limit: {limitation}");
        SourceReconciliationText.Text = text.ToString().TrimEnd();
    }

    private static string DescribeClaimScope(LocalDiagnosticEvidenceClaimScope scope) => scope switch
    {
        LocalDiagnosticEvidenceClaimScope.ExactExplicitDeviceLink => "exact explicit device link",
        LocalDiagnosticEvidenceClaimScope.NoExplicitDeviceLink => "no explicit device link",
        LocalDiagnosticEvidenceClaimScope.ExcludedIdentityMismatch => "excluded mismatched explicit device identity",
        _ => "unknown scope"
    };
}

public sealed record TimelineEntryViewItem(
    DiagnosticTimelineEntry Entry,
    string Headline,
    string Detail,
    string Qualification)
{
    public static TimelineEntryViewItem FromEntry(DiagnosticTimelineEntry entry)
    {
        DiagnosticCorrelationAssessment correlation = DiagnosticCorrelationExplainer.Assess(entry);
        return new TimelineEntryViewItem(
            entry,
            $"{entry.ObservedAt.ToLocalTime():g} — {entry.Outcome}",
            $"Condition: {entry.Condition}\nAction: {entry.Action}\nEvidence: {entry.EvidenceSource}" +
            $"\nObservation stage: {entry.ObservationStage}" +
            (entry.RelatedDevice is null ? string.Empty : $"\nRelated local device: {entry.RelatedDevice.DisplayName}") +
            (string.IsNullOrWhiteSpace(entry.Note) ? string.Empty : $"\nNote: {entry.Note}"),
            $"{DiagnosticConclusionExplainer.Explain(entry.Conclusion)}\nCorrelation: {correlation.Summary}\nEvidence gap: {correlation.EvidenceGap}");
    }
}
