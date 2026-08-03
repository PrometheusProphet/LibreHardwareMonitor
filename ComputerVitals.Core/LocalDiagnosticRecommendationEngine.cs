// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System.Globalization;
using System.Text;

namespace ComputerVitals.Core;

public static class LocalDiagnosticRecommendationEngine
{
    public static LocalDiagnosticRecommendation Create(LocalDiagnosticRecommendationInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(input.SessionEntries);
        ArgumentNullException.ThrowIfNull(input.TemperatureSamples);

        DiagnosticTimelineEntry? observation = input.SelectedObservation;
        if (observation is null)
        {
            return new LocalDiagnosticRecommendation(
                "No observation is selected.",
                "No relationship or conclusion can be assessed without a selected observation.",
                DiagnosticRecommendationConfidence.Insufficient,
                "Confidence is insufficient because no observation is selected.",
                DescribeTemperatureEvidence(input.TemperatureSamples),
                "Incident outcome, stage, source, device scope, and conflicts are unknown.",
                "Select or record one observation before considering a next observation.");
        }

        DiagnosticCorrelationAssessment correlation = DiagnosticCorrelationExplainer.Assess(observation);
        bool knownOutcome = observation.Outcome is DiagnosticOutcome.Succeeded or DiagnosticOutcome.Failed;
        bool controlledTest = IsControlledTest(observation);
        bool identityMismatch = HasMaterialIdentityMismatch(observation, input.InventoryItem);
        bool mixedOutcomes = HasMixedKnownOutcomes(observation, input.SessionEntries);
        bool inventoryInScope = observation.RelatedDevice is not null && input.InventoryItem is not null && !identityMismatch;
        DiagnosticRecommendationConfidence confidence = knownOutcome && !identityMismatch && !mixedOutcomes
            ? DiagnosticRecommendationConfidence.Limited
            : DiagnosticRecommendationConfidence.Insufficient;

        string observationSummary =
            $"Observed condition: {observation.Condition}\n" +
            $"Recorded user-performed action: {observation.Action}\n" +
            $"Outcome: {observation.Outcome}\n" +
            $"Observed at: {FormatTime(observation.ObservedAt)}\n" +
            $"Observation stage: {observation.ObservationStage}";
        string whyItMayMatter =
            $"Recorded qualification: {DiagnosticConclusionExplainer.Explain(observation.Conclusion)}\n" +
            $"Correlation qualification: {DiagnosticConclusionExplainer.Explain(correlation.Qualification)}\n" +
            $"{correlation.Summary}";

        StringBuilder basis = new();
        basis.Append(confidence == DiagnosticRecommendationConfidence.Limited
            ? "Limited confidence: the selected observation has a known outcome and a usable user-recorded evidence source."
            : "Insufficient confidence:");
        if (!knownOutcome)
            basis.Append(" the outcome is unknown or inconclusive;");
        if (identityMismatch)
            basis.Append(" the explicitly linked device does not match the supplied inventory identity;");
        if (mixedOutcomes)
            basis.Append(" the session contains mixed known outcomes for the same condition and device scope;");
        if (controlledTest)
            basis.Append(" the one-variable controlled-test structure improves scope but does not establish causality;");
        if (confidence == DiagnosticRecommendationConfidence.Limited && !controlledTest)
            basis.Append(" this is a scoped observation, not a causal or root-cause determination;");

        string evidence = DescribeEvidence(observation, correlation, input, inventoryInScope);
        string unknowns = DescribeUnknowns(observation, correlation, input, inventoryInScope, identityMismatch, mixedOutcomes);
        string next = ChooseSafestNextObservation(observation, input, inventoryInScope, mixedOutcomes);

        return new LocalDiagnosticRecommendation(
            observationSummary,
            whyItMayMatter,
            confidence,
            basis.ToString().TrimEnd(';'),
            evidence,
            unknowns,
            next);
    }

    private static string DescribeEvidence(
        DiagnosticTimelineEntry observation,
        DiagnosticCorrelationAssessment correlation,
        LocalDiagnosticRecommendationInput input,
        bool inventoryInScope)
    {
        StringBuilder evidence = new();
        evidence.AppendLine($"User evidence: {observation.EvidenceSource}; observed {FormatTime(observation.ObservedAt)}; stage {observation.ObservationStage}; outcome {observation.Outcome}.");
        evidence.AppendLine($"Correlation scope: {correlation.Qualification}. {correlation.EvidenceGap}");

        if (!inventoryInScope)
        {
            evidence.AppendLine(observation.RelatedDevice is null
                ? "Inventory scope: not considered because the observation did not explicitly link a device. Device selection or presence alone establishes no relationship."
                : "Inventory scope: not considered because the supplied local identity did not match the explicit device reference.");
        }
        else
        {
            HardwareInventoryItem item = input.InventoryItem!;
            evidence.AppendLine($"Inventory identity: {item.DisplayName}; source {item.IdentityEvidence.Source}; observed {FormatTime(item.IdentityEvidence.ObservedAt)}.");
            evidence.AppendLine($"Device Manager problem evidence: {DescribeProblemState(item.HasProblem)}");
            evidence.AppendLine(
                $"Installed driver (local evidence only, not update availability): provider {item.Driver.Provider ?? "Unknown"}; " +
                $"version {item.Driver.Version ?? "Unknown"}; source {item.Driver.Evidence.Source}; observed {FormatTime(item.Driver.Evidence.ObservedAt)}.");
            evidence.AppendLine(
                $"Firmware: version {item.Firmware.Version ?? "Unknown"}; source {item.Firmware.Evidence.Source}; observed {FormatTime(item.Firmware.Evidence.ObservedAt)}.");

            if (input.VendorGuidance is null)
            {
                evidence.AppendLine("Vendor evidence: no exact mapping is available; update availability remains Unknown.");
            }
            else if (input.VendorGuidance.Availability == UpdateAvailabilityStatus.Unknown)
            {
                evidence.AppendLine(
                    $"Vendor evidence: {input.VendorGuidance.Evidence.Source}; observed {FormatTime(input.VendorGuidance.Evidence.ObservedAt)}; " +
                    "availability Unknown. This is provenance only, not an update determination or recommendation.");
            }
            else
            {
                evidence.AppendLine("Vendor evidence: outside this recommendation's accepted Unknown scope and not used for a determination.");
            }
        }

        evidence.Append(DescribeTemperatureEvidence(input.TemperatureSamples));
        return evidence.ToString().TrimEnd();
    }

    private static string DescribeTemperatureEvidence(IReadOnlyList<TemperatureSample> samples)
    {
        if (samples.Count == 0)
            return "Temperature context: no sample snapshot was supplied. Temperature does not explain the incident.";

        StringBuilder result = new("Temperature context only; it does not explain the incident:");
        foreach (TemperatureSample sample in samples)
        {
            result.Append($"\n- {sample.DeviceKind} / {sample.SensorName ?? "Unknown source"}: {sample.State}");
            if (sample.ValueCelsius is float value)
                result.Append($"; {value.ToString("F1", CultureInfo.InvariantCulture)} °C");
            result.Append($"; observed {FormatTime(sample.ObservedAt)}");
            if (!string.IsNullOrWhiteSpace(sample.Reason))
                result.Append($"; {sample.Reason}");
        }

        return result.ToString();
    }

    private static string DescribeUnknowns(
        DiagnosticTimelineEntry observation,
        DiagnosticCorrelationAssessment correlation,
        LocalDiagnosticRecommendationInput input,
        bool inventoryInScope,
        bool identityMismatch,
        bool mixedOutcomes)
    {
        List<string> unknowns = [correlation.EvidenceGap];

        if (observation.Outcome is DiagnosticOutcome.Unknown or DiagnosticOutcome.Inconclusive)
            unknowns.Add("The recorded outcome is unknown or inconclusive.");
        if (observation.ObservationStage == DiagnosticObservationStage.Unknown)
            unknowns.Add("The observation stage is unknown.");
        if (observation.ObservationStage == DiagnosticObservationStage.BeforeWindowsStarts)
            unknowns.Add("Windows temperature and Device Manager evidence cannot decide the cause of a pre-Windows event.");
        if (mixedOutcomes)
            unknowns.Add(DescribeMixedOutcomes(observation, input.SessionEntries));
        if (identityMismatch)
            unknowns.Add("The explicit device reference and supplied inventory identity do not match, so inventory and vendor evidence were excluded.");
        else if (observation.RelatedDevice is null)
            unknowns.Add("No device was explicitly linked; selected or present inventory was excluded from relationship assessment.");

        if (inventoryInScope)
        {
            HardwareInventoryItem item = input.InventoryItem!;
            unknowns.Add(item.HasProblem switch
            {
                true => "Device Manager reports a problem, but that scoped Windows state does not establish the incident cause.",
                false => "Device Manager reports no problem; this is not a healthy-state or causality determination.",
                null => "Device Manager problem state is unknown."
            });
            if (string.IsNullOrWhiteSpace(item.Driver.Provider))
                unknowns.Add("Installed driver provider is unknown.");
            if (string.IsNullOrWhiteSpace(item.Driver.Version))
                unknowns.Add("Installed driver version is unknown.");
            if (string.IsNullOrWhiteSpace(item.Firmware.Version))
                unknowns.Add($"Firmware version is unknown. {item.Firmware.Reason}".Trim());
            if (input.VendorGuidance is null)
                unknowns.Add("No exact vendor mapping is available; update availability is unknown.");
            else
                unknowns.Add("Vendor update availability remains Unknown; no current, available, relevant, or recommended status was determined.");
        }

        foreach (TemperatureSample sample in input.TemperatureSamples)
        {
            if (sample.State != TemperatureSampleState.Current)
                unknowns.Add($"{sample.DeviceKind} temperature state remains {sample.State}; no relevance to the incident is inferred.");
        }

        return string.Join("\n", unknowns.Distinct(StringComparer.Ordinal));
    }

    private static string ChooseSafestNextObservation(
        DiagnosticTimelineEntry observation,
        LocalDiagnosticRecommendationInput input,
        bool inventoryInScope,
        bool mixedOutcomes)
    {
        string next;
        if (observation.Outcome is DiagnosticOutcome.Unknown or DiagnosticOutcome.Inconclusive || mixedOutcomes)
        {
            next = "Repeat one user-performed, one-variable controlled observation under the same recorded conditions and compare the result; do not treat either outcome as a cause.";
        }
        else
        {
            TemperatureSample? limitedTemperature = input.TemperatureSamples.FirstOrDefault(sample =>
                sample.State is TemperatureSampleState.Stale or TemperatureSampleState.Unavailable);
            if (limitedTemperature is not null)
            {
                next = $"Refresh or observe the {limitedTemperature.DeviceKind} temperature source before using it as current context; do not infer incident relevance from that reading.";
            }
            else if (inventoryInScope && input.InventoryItem!.HasProblem == true)
            {
                next = "Manually review the existing Windows Device Manager evidence for the reported problem and record what it shows; do not use that Windows-only scope to decide a pre-Windows cause.";
            }
            else if (observation.Outcome == DiagnosticOutcome.Failed && observation.ObservationStage == DiagnosticObservationStage.BeforeWindowsStarts)
            {
                next = "Only if it is safe, make one user-controlled repeat observation under the same condition; Windows evidence may be absent for the pre-Windows stage.";
            }
            else if (observation.Outcome == DiagnosticOutcome.Succeeded && observation.Conclusion == DiagnosticConclusion.ObservedRemediation)
            {
                next = "Repeat the same condition in a later user-controlled observation, or await a natural recurrence, to assess durability; do not describe the result as fixed.";
            }
            else
            {
                next = "Record one user-performed, one-variable observation under the same condition and compare the outcome without making a causal claim.";
            }
        }

        if (inventoryInScope && input.VendorGuidance?.Availability == UpdateAvailabilityStatus.Unknown)
            next += " Vendor evidence remains Unknown and supplies no update determination or recommendation.";

        return next;
    }

    private static bool HasMaterialIdentityMismatch(DiagnosticTimelineEntry observation, HardwareInventoryItem? inventoryItem)
    {
        if (observation.RelatedDevice is null)
            return false;
        if (inventoryItem is null)
            return true;

        return !string.Equals(observation.RelatedDevice.DisplayName, inventoryItem.DisplayName, StringComparison.OrdinalIgnoreCase) ||
            observation.RelatedDevice.ObservedAt != inventoryItem.IdentityEvidence.ObservedAt;
    }

    private static bool HasMixedKnownOutcomes(
        DiagnosticTimelineEntry selected,
        IReadOnlyList<DiagnosticTimelineEntry> entries)
    {
        DiagnosticOutcome[] outcomes = entries
            .Where(entry => SameObservationScope(selected, entry))
            .Select(entry => entry.Outcome)
            .Where(outcome => outcome is DiagnosticOutcome.Succeeded or DiagnosticOutcome.Failed)
            .Distinct()
            .ToArray();
        return outcomes.Contains(DiagnosticOutcome.Succeeded) && outcomes.Contains(DiagnosticOutcome.Failed);
    }

    private static string DescribeMixedOutcomes(
        DiagnosticTimelineEntry selected,
        IReadOnlyList<DiagnosticTimelineEntry> entries)
    {
        string observations = string.Join(
            "; ",
            entries
                .Where(entry => SameObservationScope(selected, entry))
                .Where(entry => entry.Outcome is DiagnosticOutcome.Succeeded or DiagnosticOutcome.Failed)
                .OrderBy(entry => entry.ObservedAt)
                .Select(entry => $"{entry.Outcome} at {entry.ObservationStage}"));
        return $"Mixed outcomes for the same condition and device scope remain unresolved: {observations}. Different Windows and pre-Windows scopes are not silently reconciled.";
    }

    private static bool SameObservationScope(DiagnosticTimelineEntry selected, DiagnosticTimelineEntry candidate)
    {
        if (!string.Equals(selected.Condition.Trim(), candidate.Condition.Trim(), StringComparison.OrdinalIgnoreCase))
            return false;
        if (selected.RelatedDevice is null || candidate.RelatedDevice is null)
            return selected.RelatedDevice is null && candidate.RelatedDevice is null;

        return string.Equals(
            selected.RelatedDevice.DisplayName,
            candidate.RelatedDevice.DisplayName,
            StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsControlledTest(DiagnosticTimelineEntry entry) =>
        entry.Condition.StartsWith("Controlled variable:", StringComparison.Ordinal) &&
        entry.Action.StartsWith("User performed:", StringComparison.Ordinal);

    private static string DescribeProblemState(bool? hasProblem) => hasProblem switch
    {
        true => "Reported problem.",
        false => "No problem reported; this is not a healthy-state determination.",
        null => "Unknown.",
    };

    private static string FormatTime(DateTimeOffset value) => value.ToString("O", CultureInfo.InvariantCulture);
}
