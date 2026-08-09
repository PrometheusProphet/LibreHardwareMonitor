// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.

namespace ComputerVitals.Core;

public sealed class ControlledTestAssistant
{
    private const int MaximumPlanFieldLength = 150;
    private readonly SessionDiagnosticTimeline _timeline;

    public ControlledTestAssistant(SessionDiagnosticTimeline timeline)
    {
        ArgumentNullException.ThrowIfNull(timeline);
        _timeline = timeline;
    }

    public ControlledTestPlan? CurrentPlan { get; private set; }

    public void Prepare(ControlledTestPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        if (CurrentPlan is not null)
            throw new InvalidOperationException("Finish or cancel the current controlled test before preparing another.");

        ValidatePlanField(plan.Variable, nameof(plan.Variable));
        ValidatePlanField(plan.BaselineCondition, nameof(plan.BaselineCondition));
        ValidatePlanField(plan.ConstantConditions, nameof(plan.ConstantConditions));
        ValidatePlanField(plan.UserPerformedAction, nameof(plan.UserPerformedAction));
        if (!Enum.IsDefined(plan.ObservationStage))
            throw new ArgumentOutOfRangeException(nameof(plan.ObservationStage));
        if (!plan.OnlyVariableWillChange)
        {
            throw new ArgumentException(
                "A multi-variable intervention is not a controlled test. Record it as an ordinary observation instead.",
                nameof(plan));
        }

        CurrentPlan = plan;
    }

    public DiagnosticTimelineEntry RecordResult(ControlledTestResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        ControlledTestPlan plan = CurrentPlan ?? throw new InvalidOperationException("Prepare one controlled test before recording a result.");
        if (!Enum.IsDefined(result.Outcome))
            throw new ArgumentOutOfRangeException(nameof(result.Outcome));

        DiagnosticTimelineEntry entry = new(
            result.ObservedAt,
            $"Controlled variable: {plan.Variable}\nBaseline: {plan.BaselineCondition}\nConstants: {plan.ConstantConditions}",
            $"User performed: {plan.UserPerformedAction}",
            result.Outcome,
            result.EvidenceSource,
            DiagnosticConclusion.NoConclusion,
            result.Note,
            plan.ObservationStage,
            plan.RelatedDevice);

        DiagnosticTimelineEntry retainedEntry = _timeline.Record(entry);
        CurrentPlan = null;
        return retainedEntry;
    }

    public void Cancel() => CurrentPlan = null;

    private static void ValidatePlanField(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("A concise value is required.", parameterName);
        if (value.Length > MaximumPlanFieldLength)
            throw new ArgumentOutOfRangeException(parameterName, $"Controlled-test plan fields are limited to {MaximumPlanFieldLength} characters.");
    }
}
