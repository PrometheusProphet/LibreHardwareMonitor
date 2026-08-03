// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.

using ComputerVitals.Core;

namespace ComputerVitals.Core.Tests;

[TestClass]
public sealed class HardwareRoleExplainerTests
{
    [TestMethod]
    public void Explain_DescribesUsbEthernetWithoutInferringCableState()
    {
        string explanation = HardwareRoleExplainer.Explain(Item("Realtek USB Ethernet"));

        StringAssert.Contains(explanation, "network interface");
        StringAssert.Contains(explanation, "no cable is connected");
    }

    [TestMethod]
    public void Explain_DescribesKnownDockChildren()
    {
        StringAssert.Contains(HardwareRoleExplainer.Explain(Item("MST controller")), "external displays");
        StringAssert.Contains(HardwareRoleExplainer.Explain(Item("USB hub")), "distributes USB devices");
        StringAssert.Contains(HardwareRoleExplainer.Explain(Item("Embedded controller")), "Dock controller");
    }

    [TestMethod]
    public void Explain_KeepsUnknownComponentsGeneral()
    {
        string explanation = HardwareRoleExplainer.Explain(Item("Unclassified component"));

        StringAssert.Contains(explanation, "Windows-reported device component");
        StringAssert.Contains(explanation, "parent relationship");
    }

    private static HardwareInventoryItem Item(string name) =>
        new(
            "SYNTHETIC\\1",
            null,
            name,
            "Synthetic",
            true,
            false,
            [],
            new InstalledDriverInfo(null, null, null, Evidence()),
            new FirmwareVersionInfo(null, Evidence()),
            Evidence());

    private static InventoryEvidence Evidence() => new("Synthetic fixture", DateTimeOffset.UnixEpoch);
}
