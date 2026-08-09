// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.

using ComputerVitals.Core;

namespace ComputerVitals.Core.Tests;

[TestClass]
public sealed class LocalDiagnosticRecommendationEngineTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 3, 18, 0, 0, TimeSpan.Zero);

    [TestMethod]
    public void Create_WithoutObservation_IsInsufficientAndInventsNoAdvice()
    {
        LocalDiagnosticRecommendation result = Create(null);

        Assert.AreEqual(DiagnosticRecommendationConfidence.Insufficient, result.Confidence);
        StringAssert.Contains(result.Observation, "No observation is selected");
        StringAssert.Contains(result.ConflictsAndUnknowns, "unknown");
        StringAssert.Contains(result.SafestNextObservation, "Select or record");
        DoesNotClaimCauseFixSafetyOrExecutableDirection(result);
    }

    [TestMethod]
    public void Create_WithUnknownOrInconclusiveOutcome_IsInsufficient()
    {
        foreach (DiagnosticOutcome outcome in new[] { DiagnosticOutcome.Unknown, DiagnosticOutcome.Inconclusive })
        {
            DiagnosticTimelineEntry entry = Entry(outcome);
            LocalDiagnosticRecommendation result = Create(entry, entries: [entry]);

            Assert.AreEqual(DiagnosticRecommendationConfidence.Insufficient, result.Confidence);
            StringAssert.Contains(result.ConfidenceBasis, "outcome is unknown or inconclusive");
            StringAssert.Contains(result.SafestNextObservation, "one-variable controlled observation");
        }
    }

    [TestMethod]
    public void Create_FailedPreWindowsLinkedDevice_ReconcilesScopesWithoutCauseOrUpdateClaim()
    {
        HardwareInventoryItem item = Inventory(hasProblem: false);
        DiagnosticTimelineEntry entry = Entry(
            DiagnosticOutcome.Failed,
            DiagnosticConclusion.PotentiallyRelevant,
            DiagnosticObservationStage.BeforeWindowsStarts,
            Device(item));
        OfficialVendorUpdateGuidance vendor = VendorUnknown(item);

        LocalDiagnosticRecommendation result = Create(
            entry,
            entries: [entry],
            temperatures: [Temperature(TemperatureSampleState.Current, 54f)],
            inventory: item,
            vendor: vendor);

        Assert.AreEqual(DiagnosticRecommendationConfidence.Limited, result.Confidence);
        StringAssert.Contains(result.WhyItMayMatter, "potentially relevant");
        StringAssert.Contains(result.WhyItMayMatter, "not a confirmed cause");
        StringAssert.Contains(result.EvidenceConsidered, "No problem reported; this is not a healthy-state determination");
        StringAssert.Contains(result.EvidenceConsidered, "availability Unknown");
        StringAssert.Contains(result.EvidenceConsidered, "Current");
        StringAssert.Contains(result.ConflictsAndUnknowns, "cannot decide the cause of a pre-Windows event");
        StringAssert.Contains(result.SafestNextObservation, "Only if it is safe");
        StringAssert.Contains(result.SafestNextObservation, "Windows evidence may be absent");
        Assert.IsFalse(result.SafestNextObservation.Contains("restart", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void Create_ObservedRemediation_RequestsDurabilityEvidenceWithoutSayingFixed()
    {
        DiagnosticTimelineEntry entry = Entry(
            DiagnosticOutcome.Succeeded,
            DiagnosticConclusion.ObservedRemediation);

        LocalDiagnosticRecommendation result = Create(entry, entries: [entry]);

        Assert.AreEqual(DiagnosticRecommendationConfidence.Limited, result.Confidence);
        StringAssert.Contains(result.WhyItMayMatter, "Observed remediation");
        StringAssert.Contains(result.WhyItMayMatter, "not proof");
        StringAssert.Contains(result.SafestNextObservation, "assess durability");
        StringAssert.Contains(result.SafestNextObservation, "do not describe the result as fixed");
        Assert.IsFalse(result.WhyItMayMatter.Contains("root cause is", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void Create_WithoutExplicitDeviceLink_IgnoresSelectedInventoryAndVendorPresence()
    {
        HardwareInventoryItem item = Inventory(hasProblem: true);
        DiagnosticTimelineEntry entry = Entry(DiagnosticOutcome.Failed, relatedDevice: null);

        LocalDiagnosticRecommendation result = Create(
            entry,
            entries: [entry],
            inventory: item,
            vendor: VendorUnknown(item));

        Assert.AreEqual(DiagnosticRecommendationConfidence.Limited, result.Confidence);
        StringAssert.Contains(result.EvidenceConsidered, "not considered because the observation did not explicitly link a device");
        StringAssert.Contains(result.ConflictsAndUnknowns, "selected or present inventory was excluded");
        Assert.IsFalse(result.EvidenceConsidered.Contains(item.DisplayName, StringComparison.Ordinal));
        Assert.IsFalse(result.EvidenceConsidered.Contains("Exact vendor fixture", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Create_PreservesAllDeviceManagerProblemStatesWithoutCallingFalseHealthy()
    {
        (bool? state, string expected)[] cases =
        [
            (true, "Reported problem"),
            (false, "No problem reported; this is not a healthy-state determination"),
            (null, "Unknown")
        ];

        foreach ((bool? state, string expected) in cases)
        {
            HardwareInventoryItem item = Inventory(state);
            DiagnosticTimelineEntry entry = Entry(DiagnosticOutcome.Failed, relatedDevice: Device(item));
            LocalDiagnosticRecommendation result = Create(entry, [entry], inventory: item);

            StringAssert.Contains(result.EvidenceConsidered, expected);
            if (state is null)
                StringAssert.Contains(result.ConflictsAndUnknowns, "problem state is unknown");
        }
    }

    [TestMethod]
    public void Create_PreservesAllTemperatureStatesAsContextOnly()
    {
        DiagnosticTimelineEntry entry = Entry(DiagnosticOutcome.Failed);
        TemperatureSample[] samples =
        [
            Temperature(TemperatureSampleState.Current, 55f),
            Temperature(TemperatureSampleState.Stale, 56f),
            Temperature(TemperatureSampleState.Unavailable, null),
            Temperature(TemperatureSampleState.Unsupported, null)
        ];

        LocalDiagnosticRecommendation result = Create(entry, [entry], samples);

        foreach (TemperatureSampleState state in Enum.GetValues<TemperatureSampleState>())
            StringAssert.Contains(result.EvidenceConsidered, state.ToString());
        StringAssert.Contains(result.EvidenceConsidered, "context only; it does not explain the incident");
        StringAssert.Contains(result.ConflictsAndUnknowns, "Stale");
        StringAssert.Contains(result.ConflictsAndUnknowns, "Unavailable");
        StringAssert.Contains(result.ConflictsAndUnknowns, "Unsupported");
        Assert.IsFalse(result.EvidenceConsidered.Contains("safe", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(result.EvidenceConsidered.Contains("healthy", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void Create_StaleOrUnavailableTemperature_ProposesRefreshWithoutRelevanceInference()
    {
        DiagnosticTimelineEntry entry = Entry(DiagnosticOutcome.Failed);
        LocalDiagnosticRecommendation result = Create(
            entry,
            [entry],
            [Temperature(TemperatureSampleState.Unavailable, null)]);

        StringAssert.Contains(result.SafestNextObservation, "Refresh or observe");
        StringAssert.Contains(result.SafestNextObservation, "do not infer incident relevance");
    }

    [TestMethod]
    public void Create_MissingDriverFirmwareAndVendorMapping_RemainUnknown()
    {
        HardwareInventoryItem item = Inventory(
            hasProblem: null,
            driverProvider: null,
            driverVersion: null,
            firmwareVersion: null);
        DiagnosticTimelineEntry entry = Entry(DiagnosticOutcome.Failed, relatedDevice: Device(item));

        LocalDiagnosticRecommendation result = Create(entry, [entry], inventory: item, vendor: null);

        StringAssert.Contains(result.EvidenceConsidered, "provider Unknown");
        StringAssert.Contains(result.EvidenceConsidered, "version Unknown");
        StringAssert.Contains(result.EvidenceConsidered, "Firmware: version Unknown");
        StringAssert.Contains(result.EvidenceConsidered, "no exact mapping is available");
        StringAssert.Contains(result.ConflictsAndUnknowns, "Installed driver provider is unknown");
        StringAssert.Contains(result.ConflictsAndUnknowns, "Firmware version is unknown");
        StringAssert.Contains(result.ConflictsAndUnknowns, "update availability is unknown");
    }

    [TestMethod]
    public void Create_VendorUnknown_NeverBecomesAvailableCurrentRelevantOrRecommended()
    {
        HardwareInventoryItem item = Inventory(false);
        DiagnosticTimelineEntry entry = Entry(DiagnosticOutcome.Succeeded, relatedDevice: Device(item));

        LocalDiagnosticRecommendation result = Create(entry, [entry], inventory: item, vendor: VendorUnknown(item));

        StringAssert.Contains(result.EvidenceConsidered, "availability Unknown");
        StringAssert.Contains(result.ConflictsAndUnknowns, "no current, available, relevant, or recommended status was determined");
        Assert.IsFalse(result.EvidenceConsidered.Contains("http", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(result.EvidenceConsidered.Contains("Status: Available", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(result.EvidenceConsidered.Contains("Status: Current", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void Create_MixedSameConditionAndDeviceOutcomes_AreVisibleAndInsufficientAcrossStages()
    {
        HardwareInventoryItem item = Inventory(false);
        DiagnosticDeviceReference device = Device(item);
        DiagnosticTimelineEntry failed = Entry(
            DiagnosticOutcome.Failed,
            DiagnosticConclusion.PotentiallyRelevant,
            DiagnosticObservationStage.BeforeWindowsStarts,
            device);
        DiagnosticTimelineEntry succeeded = Entry(
            DiagnosticOutcome.Succeeded,
            DiagnosticConclusion.NoConclusion,
            DiagnosticObservationStage.WindowsRunning,
            device) with { ObservedAt = Now.AddMinutes(1) };

        LocalDiagnosticRecommendation result = Create(failed, [succeeded, failed], inventory: item);

        Assert.AreEqual(DiagnosticRecommendationConfidence.Insufficient, result.Confidence);
        StringAssert.Contains(result.ConfidenceBasis, "mixed known outcomes");
        StringAssert.Contains(result.ConflictsAndUnknowns, "Failed at BeforeWindowsStarts");
        StringAssert.Contains(result.ConflictsAndUnknowns, "Succeeded at WindowsRunning");
        StringAssert.Contains(result.ConflictsAndUnknowns, "not silently reconciled");
        StringAssert.Contains(result.SafestNextObservation, "one-variable controlled observation");
    }

    [TestMethod]
    public void Create_MismatchedExplicitIdentity_IsInsufficientAndExcludesInventoryEvidence()
    {
        HardwareInventoryItem supplied = Inventory(false) with { DisplayName = "Different local device" };
        HardwareInventoryItem linked = Inventory(true);
        DiagnosticTimelineEntry entry = Entry(DiagnosticOutcome.Failed, relatedDevice: Device(linked));

        LocalDiagnosticRecommendation result = Create(entry, [entry], inventory: supplied);

        Assert.AreEqual(DiagnosticRecommendationConfidence.Insufficient, result.Confidence);
        StringAssert.Contains(result.ConfidenceBasis, "does not match");
        StringAssert.Contains(result.EvidenceConsidered, "not considered because the supplied local identity did not match");
        StringAssert.Contains(result.ConflictsAndUnknowns, "inventory and vendor evidence were excluded");
    }

    [TestMethod]
    public void Create_ControlledTestImprovesScopeNotCausality_AndNextTextHasNoExecutableDirection()
    {
        DiagnosticTimelineEntry entry = Entry(DiagnosticOutcome.Failed) with
        {
            Condition = "Controlled variable: dock connection\nBaseline: attached\nConstants: same host",
            Action = "User performed: changed the recorded variable"
        };

        LocalDiagnosticRecommendation result = Create(entry, [entry]);

        StringAssert.Contains(result.ConfidenceBasis, "improves scope but does not establish causality");
        DoesNotClaimCauseFixSafetyOrExecutableDirection(result);
    }

    private static LocalDiagnosticRecommendation Create(
        DiagnosticTimelineEntry? selected,
        IReadOnlyList<DiagnosticTimelineEntry>? entries = null,
        IReadOnlyList<TemperatureSample>? temperatures = null,
        HardwareInventoryItem? inventory = null,
        OfficialVendorUpdateGuidance? vendor = null) =>
        LocalDiagnosticRecommendationEngine.Create(new LocalDiagnosticRecommendationInput(
            selected,
            entries ?? (selected is null ? [] : [selected]),
            temperatures ?? [],
            inventory,
            vendor));

    private static DiagnosticTimelineEntry Entry(
        DiagnosticOutcome outcome,
        DiagnosticConclusion conclusion = DiagnosticConclusion.NoConclusion,
        DiagnosticObservationStage stage = DiagnosticObservationStage.WindowsRunning,
        DiagnosticDeviceReference? relatedDevice = null) =>
        new(
            Now,
            "Dock attached",
            "Observed the same recorded condition",
            outcome,
            "Synthetic user observation",
            conclusion,
            "Synthetic note",
            stage,
            relatedDevice);

    private static HardwareInventoryItem Inventory(
        bool? hasProblem,
        string? driverProvider = "Synthetic driver provider",
        string? driverVersion = "1.0",
        string? firmwareVersion = "2.0") =>
        new(
            "SYNTHETIC\\DEVICE",
            null,
            "Synthetic dock",
            "Synthetic vendor",
            true,
            hasProblem,
            ["SYNTHETIC\\DEVICE"],
            new InstalledDriverInfo(driverProvider, driverVersion, Now.AddDays(-1), Evidence("Synthetic driver evidence")),
            new FirmwareVersionInfo(firmwareVersion, Evidence("Synthetic firmware evidence"), firmwareVersion is null ? "Firmware was not exposed." : null),
            Evidence("Synthetic identity evidence"));

    private static DiagnosticDeviceReference Device(HardwareInventoryItem item) =>
        new(item.DisplayName, "an explicit synthetic match", item.IdentityEvidence.ObservedAt);

    private static OfficialVendorUpdateGuidance VendorUnknown(HardwareInventoryItem item) =>
        new(
            "Synthetic vendor",
            "Synthetic vendor guidance",
            new Uri("https://example.invalid/vendor"),
            UpdateAvailabilityStatus.Unknown,
            "Synthetic unknown availability.",
            "No action.",
            new InventoryEvidence("Exact vendor fixture", item.IdentityEvidence.ObservedAt));

    private static TemperatureSample Temperature(TemperatureSampleState state, float? value) =>
        new(
            TemperatureDeviceKind.Cpu,
            "Synthetic CPU",
            "Package",
            state,
            value,
            value,
            value,
            Now,
            state == TemperatureSampleState.Current ? null : $"Synthetic {state} state.");

    private static InventoryEvidence Evidence(string source) => new(source, Now);

    private static void DoesNotClaimCauseFixSafetyOrExecutableDirection(LocalDiagnosticRecommendation result)
    {
        string combined = string.Join(
            "\n",
            result.WhyItMayMatter,
            result.ConfidenceBasis,
            result.ConflictsAndUnknowns,
            result.SafestNextObservation);
        Assert.IsFalse(combined.Contains("confirmed root cause", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(combined.Contains("will fix", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(combined.Contains("safe temperature", StringComparison.OrdinalIgnoreCase));

        string next = result.SafestNextObservation;
        foreach (string forbidden in new[] { "install", "download", "run ", "flash", "restart", "shut down", "power cycle", "unplug", "elevate" })
            Assert.IsFalse(next.Contains(forbidden, StringComparison.OrdinalIgnoreCase), $"Unexpected executable direction: {forbidden}");
    }
}
