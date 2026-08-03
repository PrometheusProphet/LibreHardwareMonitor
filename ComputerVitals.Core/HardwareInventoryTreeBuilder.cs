// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.

namespace ComputerVitals.Core;

public static class HardwareInventoryTreeBuilder
{
    public static IReadOnlyList<HardwareInventoryNode> Build(IEnumerable<HardwareInventoryItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        HardwareInventoryItem[] materialized = items
            .OrderBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.InstanceId, StringComparer.Ordinal)
            .ToArray();
        Dictionary<string, HardwareInventoryItem> byId = materialized
            .GroupBy(item => item.InstanceId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        ILookup<string, HardwareInventoryItem> children = materialized
            .Where(item => item.ParentInstanceId is not null && !string.Equals(item.ParentInstanceId, item.InstanceId, StringComparison.OrdinalIgnoreCase))
            .ToLookup(item => item.ParentInstanceId!, StringComparer.OrdinalIgnoreCase);
        HashSet<string> rendered = new(StringComparer.OrdinalIgnoreCase);

        List<HardwareInventoryNode> roots = materialized
            .Where(item => item.ParentInstanceId is null || !byId.ContainsKey(item.ParentInstanceId) || string.Equals(item.ParentInstanceId, item.InstanceId, StringComparison.OrdinalIgnoreCase))
            .Select(item => BuildNode(item, children, rendered, new HashSet<string>(StringComparer.OrdinalIgnoreCase)))
            .ToList();

        foreach (HardwareInventoryItem item in materialized.Where(item => !rendered.Contains(item.InstanceId)))
            roots.Add(BuildNode(item, children, rendered, new HashSet<string>(StringComparer.OrdinalIgnoreCase)));

        return roots;
    }

    private static HardwareInventoryNode BuildNode(
        HardwareInventoryItem item,
        ILookup<string, HardwareInventoryItem> children,
        ISet<string> rendered,
        ISet<string> path)
    {
        rendered.Add(item.InstanceId);
        path.Add(item.InstanceId);

        List<HardwareInventoryNode> nodes = [];
        foreach (HardwareInventoryItem child in children[item.InstanceId])
        {
            if (!path.Contains(child.InstanceId) && !rendered.Contains(child.InstanceId))
                nodes.Add(BuildNode(child, children, rendered, new HashSet<string>(path, StringComparer.OrdinalIgnoreCase)));
        }

        return new HardwareInventoryNode(item, nodes);
    }
}
