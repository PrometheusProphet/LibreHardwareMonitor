// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.

namespace ComputerVitals.Core;

public sealed class HardwareInventoryNavigator
{
    private readonly IReadOnlyList<HardwareInventoryNode> _fullInventory;

    public HardwareInventoryNavigator(IReadOnlyList<HardwareInventoryNode> fullInventory)
    {
        ArgumentNullException.ThrowIfNull(fullInventory);
        _fullInventory = fullInventory;
        VisibleInventory = fullInventory;
    }

    public IReadOnlyList<HardwareInventoryNode> FullInventory => _fullInventory;

    public IReadOnlyList<HardwareInventoryNode> VisibleInventory { get; private set; }

    public HardwareInventoryNode? FocusedParent { get; private set; }

    public bool Focus(string instanceId)
    {
        HardwareInventoryNode? selected = HardwareInventoryTreeSearch.FindByInstanceId(_fullInventory, instanceId);
        if (selected is null || selected.Children.Count == 0)
            return false;

        FocusedParent = selected;
        VisibleInventory = new[] { selected };
        return true;
    }

    public void ShowAll()
    {
        FocusedParent = null;
        VisibleInventory = _fullInventory;
    }
}
