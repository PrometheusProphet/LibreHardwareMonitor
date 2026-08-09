// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.

namespace ComputerVitals.Core;

public static class OfficialSupportGuidanceResolver
{
    private const string DellWd19sDisplayName = "Dell Dock WD19S";
    private static readonly Uri DellWd19sSupportUri = new(
        "https://www.dell.com/support/product-details/en-us/product/dell-wd19s-130w-dock/manuals");

    public static OfficialSupportGuidance? Resolve(HardwareInventoryItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (!string.Equals(item.DisplayName, DellWd19sDisplayName, StringComparison.OrdinalIgnoreCase))
            return null;

        return new OfficialSupportGuidance(
            "Dell",
            "Dell Dock – WD19S support",
            DellWd19sSupportUri,
            "Exact Windows display name matches the Dell WD19S model.",
            new InventoryEvidence(
                "Exact Dell model-to-support mapping",
                item.IdentityEvidence.ObservedAt,
                "This link is official support guidance only; it does not establish update availability or relevance to a symptom."));
    }
}
