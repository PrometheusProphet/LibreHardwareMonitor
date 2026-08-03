// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.

using ComputerVitals.Core;

namespace ComputerVitals.Core.Tests;

[TestClass]
public sealed class HardwareInventoryTreeSearchTests
{
    [TestMethod]
    public void FindByInstanceId_ReturnsNestedRecognizedDeviceCaseInsensitively()
    {
        HardwareInventoryItem dock = Item("USB\\DOCK", "PCI\\HOST", "Dell Dock WD19S");
        IReadOnlyList<HardwareInventoryNode> roots = HardwareInventoryTreeBuilder.Build(
        [
            Item("PCI\\HOST", null, "USB host controller"),
            dock,
            Item("USB\\HUB", "USB\\DOCK", "USB hub")
        ]);

        HardwareInventoryNode? result = HardwareInventoryTreeSearch.FindByInstanceId(roots, "usb\\dock");

        Assert.IsNotNull(result);
        Assert.AreSame(dock, result.Item);
        Assert.HasCount(1, result.Children);
        Assert.AreEqual("USB hub", result.Children[0].Item.DisplayName);
    }

    [TestMethod]
    public void FindByInstanceId_ReturnsNullWhenTheDeviceIsNotInTheTree()
    {
        IReadOnlyList<HardwareInventoryNode> roots = HardwareInventoryTreeBuilder.Build([Item("USB\\DOCK", null, "Dell Dock WD19S")]);

        HardwareInventoryNode? result = HardwareInventoryTreeSearch.FindByInstanceId(roots, "USB\\MISSING");

        Assert.IsNull(result);
    }

    private static HardwareInventoryItem Item(string instanceId, string? parentInstanceId, string name) =>
        new(
            instanceId,
            parentInstanceId,
            name,
            "Synthetic vendor",
            true,
            false,
            [],
            new InstalledDriverInfo("Synthetic vendor", "1.0", null, Evidence()),
            new FirmwareVersionInfo(null, Evidence(), "Firmware version is not exposed by this fixture."),
            Evidence());

    private static InventoryEvidence Evidence() => new("Synthetic fixture", DateTimeOffset.UnixEpoch);
}
