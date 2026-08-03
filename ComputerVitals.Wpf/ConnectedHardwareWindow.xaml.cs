// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System.Text;
using System.Windows;
using System.Windows.Controls;
using ComputerVitals.Core;

namespace ComputerVitals.Wpf;

public partial class ConnectedHardwareWindow : Window
{
    private readonly IHardwareInventoryProbe _inventoryProbe = new WindowsDeviceInventoryProvider();
    private bool _refreshing;

    public ConnectedHardwareWindow()
    {
        InitializeComponent();
        Loaded += async (_, _) => await RefreshAsync();
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e) => await RefreshAsync();

    private async Task RefreshAsync()
    {
        if (_refreshing)
            return;

        _refreshing = true;
        try
        {
            HardwareInventorySnapshot snapshot = await _inventoryProbe.ReadAsync();
            IReadOnlyList<HardwareInventoryNode> roots = HardwareInventoryTreeBuilder.Build(snapshot.Items);
            DeviceTree.ItemsSource = roots.Select(HardwareInventoryViewItem.FromNode).ToArray();
            DetailsTitle.Text = "Select a component";
            DetailsRole.Text = string.Empty;
            DetailsText.Text = "Installed driver information is local Windows evidence. Firmware fields remain unknown unless this source exposes them.";
            StatusText.Text = $"Read-only inventory refreshed: {snapshot.ObservedAt.ToLocalTime():T}. {snapshot.Items.Count} Windows-reported components.";
            EvidenceText.Text = snapshot.Reason ?? $"Identity source: {snapshot.IdentityEvidence.Source}.";
        }
        catch (Exception exception)
        {
            DeviceTree.ItemsSource = null;
            DetailsTitle.Text = "Inventory unavailable";
            DetailsRole.Text = string.Empty;
            DetailsText.Text = "No inventory result is presented because the read failed.";
            StatusText.Text = $"Read-only inventory failed: {exception.Message}";
            EvidenceText.Text = string.Empty;
        }
        finally
        {
            _refreshing = false;
        }
    }

    private void DeviceTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (e.NewValue is not HardwareInventoryViewItem item)
            return;

        DetailsTitle.Text = item.DisplayName;
        DetailsRole.Text = item.Role;
        DetailsText.Text = Describe(item.Item);
    }

    private static string Describe(HardwareInventoryItem item)
    {
        StringBuilder details = new();
        details.AppendLine($"Manufacturer: {item.Manufacturer ?? "Unknown"}");
        details.AppendLine($"Presence: {DescribeBoolean(item.IsPresent, "Present", "Not currently present")}");
        details.AppendLine($"Device Manager problem state: {DescribeBoolean(item.HasProblem, "Reported", "Not reported")}");
        details.AppendLine();
        details.AppendLine("Installed driver");
        details.AppendLine($"Provider: {item.Driver.Provider ?? "Unknown"}");
        details.AppendLine($"Version: {item.Driver.Version ?? "Unknown"}");
        details.AppendLine($"Date: {item.Driver.Date?.ToLocalTime().ToString("d") ?? "Unknown"}");
        details.AppendLine($"Evidence: {item.Driver.Evidence.Source}{FormatNote(item.Driver.Evidence.Note)}");
        details.AppendLine();
        details.AppendLine("Firmware");
        details.AppendLine($"Version: {item.Firmware.Version ?? "Unknown"}");
        details.AppendLine($"Status: {item.Firmware.Reason ?? "No firmware status was reported."}");
        details.AppendLine($"Evidence: {item.Firmware.Evidence.Source}{FormatNote(item.Firmware.Evidence.Note)}");
        details.AppendLine();
        details.Append($"Component identity evidence: {item.IdentityEvidence.Source} observed {item.IdentityEvidence.ObservedAt.ToLocalTime():g}.");
        return details.ToString();
    }

    private static string DescribeBoolean(bool? value, string trueText, string falseText) => value switch
    {
        true => trueText,
        false => falseText,
        _ => "Unknown"
    };

    private static string FormatNote(string? note) => string.IsNullOrWhiteSpace(note) ? string.Empty : $" ({note})";
}

public sealed class HardwareInventoryViewItem
{
    private HardwareInventoryViewItem(HardwareInventoryItem item, IReadOnlyList<HardwareInventoryViewItem> children)
    {
        Item = item;
        Children = children;
        DisplayName = item.DisplayName;
        Role = HardwareRoleExplainer.Explain(item);
        Summary = item.HasProblem switch
        {
            true => "Device Manager reports a problem",
            false => Role,
            _ => $"Problem state unknown. {Role}"
        };
    }

    public HardwareInventoryItem Item { get; }

    public IReadOnlyList<HardwareInventoryViewItem> Children { get; }

    public string DisplayName { get; }

    public string Role { get; }

    public string Summary { get; }

    public static HardwareInventoryViewItem FromNode(HardwareInventoryNode node) =>
        new(node.Item, node.Children.Select(FromNode).ToArray());
}
