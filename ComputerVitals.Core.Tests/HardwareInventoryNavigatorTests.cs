// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.

using ComputerVitals.Core;

namespace ComputerVitals.Core.Tests;

[TestClass]
public sealed class HardwareInventoryNavigatorTests
{
    [TestMethod]
    public void Focus_ShowsAnySelectedParentsCompleteReportedSubtree_AndRestoresUnchangedInventory()
    {
        HardwareInventoryItem parent = Item("USB\\COMPOSITE", null, "Generic composite device", false, null);
        HardwareInventoryItem failedChild = Item("USB\\FAILED", parent.InstanceId, "Failed child", false, true);
        HardwareInventoryItem unknownChild = Item("USB\\UNKNOWN", parent.InstanceId, "Unknown child", null, null);
        HardwareInventoryItem orphan = Item("USB\\ORPHAN", "USB\\MISSING", "Orphaned device", true, false);
        IReadOnlyList<HardwareInventoryNode> roots = HardwareInventoryTreeBuilder.Build([parent, failedChild, unknownChild, orphan]);
        HardwareInventoryNavigator navigator = new(roots);

        bool focused = navigator.Focus(parent.InstanceId);

        Assert.IsTrue(focused);
        Assert.AreSame(parent, navigator.FocusedParent!.Item);
        Assert.HasCount(1, navigator.VisibleInventory);
        Assert.HasCount(2, navigator.VisibleInventory[0].Children);
        Assert.IsFalse(navigator.VisibleInventory[0].Item.IsPresent);
        Assert.IsTrue(navigator.VisibleInventory[0].Children.Single(child => child.Item == failedChild).Item.HasProblem);
        Assert.IsNull(navigator.VisibleInventory[0].Children.Single(child => child.Item == unknownChild).Item.IsPresent);

        navigator.ShowAll();

        Assert.IsNull(navigator.FocusedParent);
        Assert.AreSame(roots, navigator.VisibleInventory);
        Assert.IsTrue(navigator.VisibleInventory.Any(root => root.Item == orphan));
    }

    [TestMethod]
    public void Focus_PreservesTheBoundedRepresentationOfCyclicData()
    {
        IReadOnlyList<HardwareInventoryNode> roots = HardwareInventoryTreeBuilder.Build(
        [
            Item("USB\\A", "USB\\B", "A", true, false),
            Item("USB\\B", "USB\\A", "B", true, false)
        ]);
        HardwareInventoryNavigator navigator = new(roots);

        Assert.IsTrue(navigator.Focus("USB\\A"));
        Assert.HasCount(1, navigator.VisibleInventory[0].Children);
        Assert.AreEqual("USB\\B", navigator.VisibleInventory[0].Children[0].Item.InstanceId);
        Assert.IsEmpty(navigator.VisibleInventory[0].Children[0].Children);
    }

    [TestMethod]
    public void Focus_CanStartAtANestedParentWithoutIncludingItsAncestors()
    {
        HardwareInventoryItem root = Item("PCI\\ROOT", null, "Host", true, false);
        HardwareInventoryItem nestedParent = Item("USB\\PARENT", root.InstanceId, "Selected parent", true, false);
        HardwareInventoryItem child = Item("USB\\CHILD", nestedParent.InstanceId, "Reported child", true, false);
        IReadOnlyList<HardwareInventoryNode> roots = HardwareInventoryTreeBuilder.Build([root, nestedParent, child]);
        HardwareInventoryNavigator navigator = new(roots);

        Assert.IsTrue(navigator.Focus(nestedParent.InstanceId));
        Assert.AreSame(nestedParent, navigator.VisibleInventory.Single().Item);
        Assert.AreSame(child, navigator.VisibleInventory.Single().Children.Single().Item);
    }

    [TestMethod]
    public void Focus_DoesNotReplaceTheCurrentViewForLeafOrMissingSelections()
    {
        IReadOnlyList<HardwareInventoryNode> roots = HardwareInventoryTreeBuilder.Build([Item("USB\\LEAF", null, "Leaf", true, false)]);
        HardwareInventoryNavigator navigator = new(roots);

        Assert.IsFalse(navigator.Focus("USB\\LEAF"));
        Assert.IsFalse(navigator.Focus("USB\\MISSING"));
        Assert.AreSame(roots, navigator.VisibleInventory);
        Assert.IsNull(navigator.FocusedParent);
    }

    private static HardwareInventoryItem Item(
        string instanceId,
        string? parentInstanceId,
        string name,
        bool? isPresent,
        bool? hasProblem) =>
        new(
            instanceId,
            parentInstanceId,
            name,
            "Synthetic vendor",
            isPresent,
            hasProblem,
            ["SYNTHETIC\\COMPOSITE"],
            new InstalledDriverInfo(null, null, null, Evidence("Driver evidence unavailable.")),
            new FirmwareVersionInfo(null, Evidence("Firmware evidence unavailable."), "Firmware version is unknown."),
            Evidence("Synthetic identity fixture."));

    private static InventoryEvidence Evidence(string note) => new("Synthetic fixture", DateTimeOffset.UnixEpoch, note);
}
