// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.

namespace ComputerVitals.Core;

public static class HardwareRoleExplainer
{
    public static string Explain(HardwareInventoryItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        string value = string.Join(' ', new[] { item.DisplayName, item.Manufacturer }.Where(text => !string.IsNullOrWhiteSpace(text)));
        if (value.Contains("USB hub", StringComparison.OrdinalIgnoreCase))
            return "USB hub — distributes USB devices connected through this parent device.";
        if (value.Contains("Realtek USB Ethernet", StringComparison.OrdinalIgnoreCase))
            return "USB Ethernet adapter — exposes a network interface even when no cable is connected.";
        if (value.Contains("MST", StringComparison.OrdinalIgnoreCase))
            return "Display controller — routes one connection to one or more external displays.";
        if (value.Contains("embedded controller", StringComparison.OrdinalIgnoreCase))
            return "Dock controller — coordinates power, ports, and other dock functions.";

        return "Windows-reported device component. Its parent relationship shows which hardware group it belongs to.";
    }
}
