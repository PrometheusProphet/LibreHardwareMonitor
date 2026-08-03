// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.

using ComputerVitals.Core;

namespace ComputerVitals.Core.Tests;

[TestClass]
public sealed class LocalDiagnosticEvidenceReconciliationEngineTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 3, 20, 0, 0, TimeSpan.Zero);

    [TestMethod]
    public void Reconcile_NoClaims_LeavesEvidenceUnknownWithoutVerdict()
    {
        LocalDiagnosticEvidenceReconciliationResult result = Reconcile([]);

        Assert.IsEmpty(result.ClaimConsiderations);
        Assert.IsEmpty(result.UnresolvedConflicts);
        StringAssert.Contains(Combined(result), "No claims were supplied");
        DoesNotProvideActionOrVerdict(result);
    }

    [TestMethod]
    public void Reconcile_UnknownClaim_IsConsideredAndRemainsExplicit()
    {
        LocalDiagnosticEvidenceClaim claim = Claim(
            LocalDiagnosticEvidenceSourceKind.Unknown,
            LocalDiagnosticEvidenceDisposition.Unknown,
            DiagnosticObservationStage.Unknown);

        LocalDiagnosticEvidenceReconciliationResult result = Reconcile([claim]);

        Assert.AreSame(claim, result.ClaimConsiderations.Single().Claim);
        StringAssert.Contains(Combined(result), "Unknown claims remain explicit");
        DoesNotProvideActionOrVerdict(result);
    }

    [TestMethod]
    public void Reconcile_IncompatibleInstallerPairForExactDevice_IsUnresolvedWithoutRanking()
    {
        DiagnosticDeviceReference device = Device("Synthetic dock");
        LocalDiagnosticEvidenceClaim summary = Claim(
            LocalDiagnosticEvidenceSourceKind.InstallerSummary,
            LocalDiagnosticEvidenceDisposition.Failed,
            DiagnosticObservationStage.WindowsRunning,
            device);
        LocalDiagnosticEvidenceClaim detail = Claim(
            LocalDiagnosticEvidenceSourceKind.InstallerDetail,
            LocalDiagnosticEvidenceDisposition.Succeeded,
            DiagnosticObservationStage.WindowsRunning,
            device,
            Now.AddMinutes(1));

        LocalDiagnosticEvidenceReconciliationResult result = Reconcile([summary, detail], device);

        Assert.HasCount(1, result.UnresolvedConflicts);
        CollectionAssert.Contains(result.UnresolvedConflicts.Single().Claims.ToArray(), summary);
        CollectionAssert.Contains(result.UnresolvedConflicts.Single().Claims.ToArray(), detail);
        StringAssert.Contains(Combined(result), "without selecting or ranking a source");
        DoesNotProvideActionOrVerdict(result);
    }

    [TestMethod]
    public void Reconcile_DeviceManagerProblemPair_IsUnresolvedAndNoProblemIsNotHealthy()
    {
        DiagnosticDeviceReference device = Device("Synthetic dock");
        LocalDiagnosticEvidenceClaim problem = Claim(
            LocalDiagnosticEvidenceSourceKind.DeviceManager,
            LocalDiagnosticEvidenceDisposition.ProblemReported,
            DiagnosticObservationStage.WindowsRunning,
            device);
        LocalDiagnosticEvidenceClaim noProblem = Claim(
            LocalDiagnosticEvidenceSourceKind.DeviceManager,
            LocalDiagnosticEvidenceDisposition.NoProblemReported,
            DiagnosticObservationStage.WindowsRunning,
            device,
            Now.AddMinutes(1));

        LocalDiagnosticEvidenceReconciliationResult result = Reconcile([problem, noProblem], device);

        Assert.HasCount(1, result.UnresolvedConflicts);
        StringAssert.Contains(Combined(result), "not a healthy-state or intervention-success claim");
        DoesNotProvideActionOrVerdict(result);
    }

    [TestMethod]
    public void Reconcile_PreWindowsFailureAndWindowsNoProblem_RemainScopeLimitedNotReconciled()
    {
        DiagnosticDeviceReference device = Device("Synthetic dock");
        LocalDiagnosticEvidenceClaim failed = Claim(
            LocalDiagnosticEvidenceSourceKind.UserObservation,
            LocalDiagnosticEvidenceDisposition.Failed,
            DiagnosticObservationStage.BeforeWindowsStarts,
            device);
        LocalDiagnosticEvidenceClaim noProblem = Claim(
            LocalDiagnosticEvidenceSourceKind.DeviceManager,
            LocalDiagnosticEvidenceDisposition.NoProblemReported,
            DiagnosticObservationStage.WindowsRunning,
            device,
            Now.AddMinutes(1));

        LocalDiagnosticEvidenceReconciliationResult result = Reconcile([failed, noProblem], device);

        Assert.IsEmpty(result.UnresolvedConflicts);
        StringAssert.Contains(Combined(result), "outside decisive Windows scope");
        StringAssert.Contains(Combined(result), "scope-limited");
        DoesNotProvideActionOrVerdict(result);
    }

    [TestMethod]
    public void Reconcile_ConsistentClaims_RemainNonCausal()
    {
        DiagnosticDeviceReference device = Device("Synthetic dock");
        LocalDiagnosticEvidenceClaim first = Claim(
            LocalDiagnosticEvidenceSourceKind.UserObservation,
            LocalDiagnosticEvidenceDisposition.Failed,
            DiagnosticObservationStage.WindowsRunning,
            device);
        LocalDiagnosticEvidenceClaim second = Claim(
            LocalDiagnosticEvidenceSourceKind.InstallerSummary,
            LocalDiagnosticEvidenceDisposition.Failed,
            DiagnosticObservationStage.WindowsRunning,
            device,
            Now.AddMinutes(1));

        LocalDiagnosticEvidenceReconciliationResult result = Reconcile([first, second], device);

        Assert.IsEmpty(result.UnresolvedConflicts);
        DoesNotProvideActionOrVerdict(result);
    }

    [TestMethod]
    public void Reconcile_MismatchedExplicitIdentity_IsExcludedWithoutPresenceInference()
    {
        LocalDiagnosticEvidenceClaim claim = Claim(
            LocalDiagnosticEvidenceSourceKind.UserObservation,
            LocalDiagnosticEvidenceDisposition.Failed,
            DiagnosticObservationStage.WindowsRunning,
            Device("Different device"));

        LocalDiagnosticEvidenceReconciliationResult result = Reconcile([claim], Device("Scoped device"));

        LocalDiagnosticEvidenceConsideration consideration = result.ClaimConsiderations.Single();
        Assert.IsFalse(consideration.IsConsidered);
        Assert.AreEqual(LocalDiagnosticEvidenceClaimScope.ExcludedIdentityMismatch, consideration.Scope);
        StringAssert.Contains(Combined(result), "presence or selection is not used to infer a relationship");
    }

    [TestMethod]
    public void Reconcile_PreservesSourceTimeAndScopeForEveryClaim()
    {
        DiagnosticDeviceReference device = Device("Synthetic dock");
        LocalDiagnosticEvidenceClaim linked = Claim(
            LocalDiagnosticEvidenceSourceKind.InstallerDetail,
            LocalDiagnosticEvidenceDisposition.Succeeded,
            DiagnosticObservationStage.WindowsRunning,
            device,
            Now.AddMinutes(2));
        LocalDiagnosticEvidenceClaim unlinked = Claim(
            LocalDiagnosticEvidenceSourceKind.UserObservation,
            LocalDiagnosticEvidenceDisposition.Unknown,
            DiagnosticObservationStage.Unknown);

        LocalDiagnosticEvidenceReconciliationResult result = Reconcile([linked, unlinked], device);

        Assert.HasCount(2, result.ClaimConsiderations);
        Assert.AreSame(linked, result.ClaimConsiderations[0].Claim);
        Assert.AreSame(unlinked, result.ClaimConsiderations[1].Claim);
        Assert.AreEqual(LocalDiagnosticEvidenceClaimScope.ExactExplicitDeviceLink, result.ClaimConsiderations[0].Scope);
        Assert.AreEqual(LocalDiagnosticEvidenceClaimScope.NoExplicitDeviceLink, result.ClaimConsiderations[1].Scope);
        Assert.AreEqual(Now.AddMinutes(2), result.ClaimConsiderations[0].Claim.ObservedAt);
        Assert.AreEqual(LocalDiagnosticEvidenceSourceKind.InstallerDetail, result.ClaimConsiderations[0].Claim.SourceKind);
    }

    [TestMethod]
    public void Reconcile_RejectsDeviceManagerInterventionDisposition()
    {
        LocalDiagnosticEvidenceClaim invalid = Claim(
            LocalDiagnosticEvidenceSourceKind.DeviceManager,
            LocalDiagnosticEvidenceDisposition.Succeeded,
            DiagnosticObservationStage.WindowsRunning);

        ArgumentException? error = null;
        try
        {
            Reconcile([invalid]);
        }
        catch (ArgumentException exception)
        {
            error = exception;
        }

        Assert.IsNotNull(error);
        StringAssert.Contains(error.Message, "neither healthy nor an intervention success");
    }

    private static LocalDiagnosticEvidenceReconciliationResult Reconcile(
        IReadOnlyList<LocalDiagnosticEvidenceClaim> claims,
        DiagnosticDeviceReference? scope = null) =>
        LocalDiagnosticEvidenceReconciliationEngine.Reconcile(new LocalDiagnosticEvidenceReconciliationInput(claims, scope));

    private static LocalDiagnosticEvidenceClaim Claim(
        LocalDiagnosticEvidenceSourceKind source,
        LocalDiagnosticEvidenceDisposition disposition,
        DiagnosticObservationStage stage,
        DiagnosticDeviceReference? device = null,
        DateTimeOffset? observedAt = null) =>
        new(source, disposition, observedAt ?? Now, stage, device);

    private static DiagnosticDeviceReference Device(string displayName) =>
        new(displayName, "Synthetic explicit device reference", Now);

    private static string Combined(LocalDiagnosticEvidenceReconciliationResult result) =>
        string.Join("\n", result.ScopeLimitations);

    private static void DoesNotProvideActionOrVerdict(LocalDiagnosticEvidenceReconciliationResult result)
    {
        string combined = string.Join("\n", result.ScopeLimitations);
        foreach (string forbidden in new[] { "install", "download", "flash", "restart", "power", "elevat", "vendor", "update available", "current version", "recommended" })
            Assert.IsFalse(combined.Contains(forbidden, StringComparison.OrdinalIgnoreCase), $"Unexpected assertion: {forbidden}");
        Assert.IsFalse(combined.Contains("is the cause", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(combined.Contains("is fixed", StringComparison.OrdinalIgnoreCase));
    }
}
