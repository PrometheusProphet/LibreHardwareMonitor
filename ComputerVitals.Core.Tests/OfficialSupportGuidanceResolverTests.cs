// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.

using ComputerVitals.Core;

namespace ComputerVitals.Core.Tests;

[TestClass]
public sealed class OfficialSupportGuidanceResolverTests
{
    [TestMethod]
    public void Resolve_ReturnsDellSupportOnlyForTheExactWd19sDisplayName()
    {
        OfficialSupportGuidance? guidance = OfficialSupportGuidanceResolver.Resolve(Item("Dell Dock WD19S"));

        Assert.IsNotNull(guidance);
        Assert.AreEqual("Dell", guidance.Provider);
        Assert.AreEqual("https://www.dell.com/support/product-details/en-us/product/dell-wd19s-130w-dock/manuals", guidance.SupportUri.AbsoluteUri);
        StringAssert.Contains(guidance.MatchReason, "Exact Windows display name");
    }

    [TestMethod]
    public void Resolve_RefusesPartialOrDifferentModelMatches()
    {
        Assert.IsNull(OfficialSupportGuidanceResolver.Resolve(Item("Dell Dock WD19")));
        Assert.IsNull(OfficialSupportGuidanceResolver.Resolve(Item("Dell Dock WD19S compatible device")));
        Assert.IsNull(OfficialSupportGuidanceResolver.Resolve(Item("Generic USB Hub")));
    }

    private static HardwareInventoryItem Item(string displayName) =>
        new(
            "SYNTHETIC\\1",
            null,
            displayName,
            "Synthetic",
            true,
            false,
            [],
            new InstalledDriverInfo(null, null, null, Evidence()),
            new FirmwareVersionInfo(null, Evidence()),
            Evidence());

    private static InventoryEvidence Evidence() => new("Synthetic fixture", DateTimeOffset.UnixEpoch);
}
