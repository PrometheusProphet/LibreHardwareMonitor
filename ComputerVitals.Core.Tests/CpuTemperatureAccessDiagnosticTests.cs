// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.

using ComputerVitals.Core;

namespace ComputerVitals.Core.Tests;

[TestClass]
public sealed class CpuTemperatureAccessDiagnosticTests
{
    [TestMethod]
    public void ExplainNoReadableSource_IdentifiesMissingPawnIoWithoutPromisingAutomaticMutation()
    {
        string reason = CpuTemperatureAccessDiagnostic.ExplainNoReadableSource(isAmdCpu: true, isPawnIoInstalled: false);

        StringAssert.Contains(reason, "PawnIO is not installed");
        StringAssert.Contains(reason, "will not install");
        StringAssert.Contains(reason, "request elevation automatically");
    }

    [TestMethod]
    public void ExplainNoReadableSource_KeepsInstalledDriverAndSupportAmbiguityVisible()
    {
        string reason = CpuTemperatureAccessDiagnostic.ExplainNoReadableSource(isAmdCpu: true, isPawnIoInstalled: true);

        StringAssert.Contains(reason, "PawnIO is installed");
        StringAssert.Contains(reason, "may be unavailable");
        StringAssert.Contains(reason, "may be unsupported");
    }

    [TestMethod]
    public void ExplainNoReadableSource_DoesNotAttributeNonAmdFailureToPawnIo()
    {
        string reason = CpuTemperatureAccessDiagnostic.ExplainNoReadableSource(isAmdCpu: false, isPawnIoInstalled: false);

        Assert.IsFalse(reason.Contains("PawnIO", StringComparison.Ordinal));
        StringAssert.Contains(reason, "low-level access may be unavailable");
        StringAssert.Contains(reason, "may be unsupported");
    }
}
