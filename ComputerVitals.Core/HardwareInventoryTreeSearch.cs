// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.

namespace ComputerVitals.Core;

public static class HardwareInventoryTreeSearch
{
    public static HardwareInventoryNode? FindByInstanceId(IEnumerable<HardwareInventoryNode> roots, string instanceId)
    {
        ArgumentNullException.ThrowIfNull(roots);
        ArgumentException.ThrowIfNullOrWhiteSpace(instanceId);

        foreach (HardwareInventoryNode root in roots)
        {
            HardwareInventoryNode? match = FindByInstanceId(root, instanceId);
            if (match is not null)
                return match;
        }

        return null;
    }

    private static HardwareInventoryNode? FindByInstanceId(HardwareInventoryNode node, string instanceId)
    {
        if (string.Equals(node.Item.InstanceId, instanceId, StringComparison.OrdinalIgnoreCase))
            return node;

        foreach (HardwareInventoryNode child in node.Children)
        {
            HardwareInventoryNode? match = FindByInstanceId(child, instanceId);
            if (match is not null)
                return match;
        }

        return null;
    }
}
