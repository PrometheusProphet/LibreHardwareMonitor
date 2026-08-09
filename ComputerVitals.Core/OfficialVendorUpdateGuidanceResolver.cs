// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.

namespace ComputerVitals.Core;

public static class OfficialVendorUpdateGuidanceResolver
{
    private const string DellWd19sDisplayName = "Dell Dock WD19S";
    private static readonly Uri DellWd19sDriversUri = new(
        "https://www.dell.com/support/product-details/en-us/product/dell-wd19s-130w-dock/drivers");

    public static OfficialVendorUpdateGuidance? Resolve(HardwareInventoryItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (!string.Equals(item.DisplayName, DellWd19sDisplayName, StringComparison.OrdinalIgnoreCase))
            return null;

        return new OfficialVendorUpdateGuidance(
            "Dell",
            "Dell Dock – WD19S drivers and downloads",
            DellWd19sDriversUri,
            UpdateAvailabilityStatus.Unknown,
            "Windows PnP did not expose comparable installed dock firmware component versions. Computer Vitals does not query or compare Dell's catalog, so it cannot say an update is available, recommended for this configuration, or relevant to a symptom.",
            "Review Dell's live catalog for exact applicability, release notes, prerequisites, power, restart, encryption, and interruption risks. Any download or firmware update must be initiated by you; Computer Vitals does not install or flash firmware.",
            new InventoryEvidence(
                "Exact Dell model-to-drivers mapping",
                item.IdentityEvidence.ObservedAt,
                "The link opens Dell's live product catalog. It is guidance, not an update-availability determination."));
    }
}
