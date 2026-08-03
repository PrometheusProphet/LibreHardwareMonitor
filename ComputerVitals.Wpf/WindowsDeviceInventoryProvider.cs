// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System.Management;
using ComputerVitals.Core;
using Windows.Devices.Enumeration;

namespace ComputerVitals.Wpf;

public sealed class WindowsDeviceInventoryProvider : IHardwareInventoryProbe
{
    private static readonly string[] RequestedProperties =
    [
        "System.Devices.DeviceManufacturer",
        "System.Devices.Parent",
        "System.Devices.HardwareIds",
        "System.Devices.Present",
        "System.Devices.DeviceHasProblem"
    ];

    public async Task<HardwareInventorySnapshot> ReadAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        DateTimeOffset observedAt = DateTimeOffset.UtcNow;
        DriverInventoryResult driverInventory = await Task.Run(ReadInstalledDrivers, cancellationToken);
        DeviceInformationCollection devices = await DeviceInformation.FindAllAsync(
            string.Empty,
            RequestedProperties,
            DeviceInformationKind.Device);
        cancellationToken.ThrowIfCancellationRequested();

        InventoryEvidence pnpEvidence = new("Windows PnP inventory", observedAt);
        List<HardwareInventoryItem> items = new(devices.Count);
        foreach (DeviceInformation device in devices)
        {
            string instanceId = device.Id;
            driverInventory.ByInstanceId.TryGetValue(instanceId, out DriverRecord? driver);
            InventoryEvidence driverEvidence = new(
                "Windows signed-driver inventory",
                observedAt,
                driverInventory.Reason);
            InventoryEvidence firmwareEvidence = new(
                "Windows PnP inventory",
                observedAt,
                "Firmware version was not exposed by the Windows PnP inventory source.");

            items.Add(new HardwareInventoryItem(
                instanceId,
                ReadString(device.Properties, "System.Devices.Parent"),
                string.IsNullOrWhiteSpace(device.Name) ? "Unnamed Windows device" : device.Name,
                ReadString(device.Properties, "System.Devices.DeviceManufacturer"),
                ReadBoolean(device.Properties, "System.Devices.Present"),
                ReadBoolean(device.Properties, "System.Devices.DeviceHasProblem"),
                ReadStrings(device.Properties, "System.Devices.HardwareIds"),
                new InstalledDriverInfo(driver?.Provider, driver?.Version, driver?.Date, driverEvidence),
                new FirmwareVersionInfo(null, firmwareEvidence, firmwareEvidence.Note),
                pnpEvidence));
        }

        string? reason = driverInventory.Reason is null
            ? null
            : "Some installed-driver fields are unavailable. " + driverInventory.Reason;
        return new HardwareInventorySnapshot(observedAt, items, pnpEvidence, reason);
    }

    private static DriverInventoryResult ReadInstalledDrivers()
    {
        try
        {
            Dictionary<string, DriverRecord> result = new(StringComparer.OrdinalIgnoreCase);
            using ManagementObjectSearcher searcher = new("SELECT DeviceID, DriverProviderName, DriverVersion, DriverDate FROM Win32_PnPSignedDriver");
            using ManagementObjectCollection drivers = searcher.Get();
            foreach (ManagementObject driver in drivers)
            {
                string? deviceId = ReadString(driver["DeviceID"]);
                if (string.IsNullOrWhiteSpace(deviceId))
                    continue;

                result[deviceId] = new DriverRecord(
                    ReadString(driver["DriverProviderName"]),
                    ReadString(driver["DriverVersion"]),
                    ReadDate(driver["DriverDate"]));
            }

            return new DriverInventoryResult(result, null);
        }
        catch (Exception exception)
        {
            return new DriverInventoryResult(
                new Dictionary<string, DriverRecord>(StringComparer.OrdinalIgnoreCase),
                $"Windows signed-driver inventory could not be read: {exception.Message}");
        }
    }

    private static string? ReadString(IReadOnlyDictionary<string, object> properties, string key) =>
        properties.TryGetValue(key, out object? value) ? ReadString(value) : null;

    private static string? ReadString(object? value) => value switch
    {
        string text when !string.IsNullOrWhiteSpace(text) => text,
        _ => null
    };

    private static bool? ReadBoolean(IReadOnlyDictionary<string, object> properties, string key) =>
        properties.TryGetValue(key, out object? value) && value is bool result ? result : null;

    private static IReadOnlyList<string> ReadStrings(IReadOnlyDictionary<string, object> properties, string key) =>
        properties.TryGetValue(key, out object? value) && value is string[] values ? values : [];

    private static DateTimeOffset? ReadDate(object? value)
    {
        if (value is not string dmtfDate || string.IsNullOrWhiteSpace(dmtfDate))
            return null;

        try
        {
            return new DateTimeOffset(ManagementDateTimeConverter.ToDateTime(dmtfDate));
        }
        catch (Exception)
        {
            return null;
        }
    }

    private sealed record DriverRecord(string? Provider, string? Version, DateTimeOffset? Date);

    private sealed record DriverInventoryResult(
        IReadOnlyDictionary<string, DriverRecord> ByInstanceId,
        string? Reason);
}
