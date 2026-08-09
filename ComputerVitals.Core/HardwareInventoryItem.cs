// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.

namespace ComputerVitals.Core;

public sealed record HardwareInventoryItem(
    string InstanceId,
    string? ParentInstanceId,
    string DisplayName,
    string? Manufacturer,
    bool? IsPresent,
    bool? HasProblem,
    IReadOnlyList<string> HardwareIds,
    InstalledDriverInfo Driver,
    FirmwareVersionInfo Firmware,
    InventoryEvidence IdentityEvidence);
