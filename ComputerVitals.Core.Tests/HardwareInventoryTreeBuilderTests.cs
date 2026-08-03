// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.

using ComputerVitals.Core;

namespace ComputerVitals.Core.Tests;

[TestClass]
public sealed class HardwareInventoryTreeBuilderTests
{
    [TestMethod]
    public void Build_GroupsKnownChildrenBelowTheirParent()
    {
        IReadOnlyList<HardwareInventoryNode> roots = HardwareInventoryTreeBuilder.Build(
        [
            Item("USB\\ROOT", null, "Dell WD19S"),
            Item("USB\\MST", "USB\\ROOT", "MST controller"),
            Item("USB\\HUB", "USB\\ROOT", "USB hub")
        ]);

        Assert.HasCount(1, roots);
        Assert.AreEqual("Dell WD19S", roots[0].Item.DisplayName);
        CollectionAssert.AreEquivalent(new[] { "MST controller", "USB hub" }, roots[0].Children.Select(child => child.Item.DisplayName).ToArray());
    }

    [TestMethod]
    public void Build_LeavesOrphanedDevicesVisibleAtTheRoot()
    {
        IReadOnlyList<HardwareInventoryNode> roots = HardwareInventoryTreeBuilder.Build([Item("USB\\ORPHAN", "USB\\MISSING", "Realtek USB Ethernet")]);

        Assert.HasCount(1, roots);
        Assert.AreEqual("Realtek USB Ethernet", roots[0].Item.DisplayName);
    }

    [TestMethod]
    public void Build_DoesNotRecurseForeverForCyclicParentData()
    {
        IReadOnlyList<HardwareInventoryNode> roots = HardwareInventoryTreeBuilder.Build(
        [
            Item("USB\\A", "USB\\B", "A"),
            Item("USB\\B", "USB\\A", "B")
        ]);

        Assert.HasCount(1, roots);
        Assert.AreEqual("A", roots[0].Item.DisplayName);
        Assert.HasCount(1, roots[0].Children);
        Assert.AreEqual("B", roots[0].Children[0].Item.DisplayName);
        Assert.IsEmpty(roots[0].Children[0].Children);
    }

    private static HardwareInventoryItem Item(string instanceId, string? parentInstanceId, string name) =>
        new(
            instanceId,
            parentInstanceId,
            name,
            "Dell",
            true,
            false,
            [],
            new InstalledDriverInfo("Dell", "1.0", null, Evidence()),
            new FirmwareVersionInfo(null, Evidence(), "Firmware version is not exposed by Windows."),
            Evidence());

    private static InventoryEvidence Evidence() => new("Synthetic fixture", DateTimeOffset.UnixEpoch);
}
