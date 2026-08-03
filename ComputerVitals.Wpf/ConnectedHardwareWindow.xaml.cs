// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System.Text;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using ComputerVitals.Core;

namespace ComputerVitals.Wpf;

public partial class ConnectedHardwareWindow : Window
{
    private readonly IHardwareInventoryProbe _inventoryProbe = new WindowsDeviceInventoryProvider();
    private IReadOnlyList<HardwareInventoryViewItem> _allDevices = [];
    private HardwareInventoryViewItem? _recognizedDevice;
    private HardwareInventoryItem? _recognizedInventoryItem;
    private bool _refreshing;

    public ConnectedHardwareWindow()
    {
        InitializeComponent();
        Loaded += async (_, _) => await RefreshAsync();
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e) => await RefreshAsync();

    private void ShowAllDevices_Click(object sender, RoutedEventArgs e)
    {
        DeviceTree.ItemsSource = _allDevices;
        ShowAllDevicesButton.Visibility = Visibility.Collapsed;
        FocusRecognizedDeviceButton.Visibility = _recognizedDevice is null ? Visibility.Collapsed : Visibility.Visible;
        UpdateStatusForAllDevices();
    }

    private void FocusRecognizedDevice_Click(object sender, RoutedEventArgs e) => ShowRecognizedDevice();

    private void IncidentTimeline_Click(object sender, RoutedEventArgs e) =>
        new IncidentTimelineWindow(_recognizedInventoryItem) { Owner = this }.Show();

    private async Task RefreshAsync()
    {
        if (_refreshing)
            return;

        _refreshing = true;
        try
        {
            HardwareInventorySnapshot snapshot = await _inventoryProbe.ReadAsync();
            IReadOnlyList<HardwareInventoryNode> roots = HardwareInventoryTreeBuilder.Build(snapshot.Items);
            _allDevices = roots.Select(HardwareInventoryViewItem.FromNode).ToArray();
            HardwareInventoryItem? supportMatchedItem = snapshot.Items.FirstOrDefault(item =>
                OfficialSupportGuidanceResolver.Resolve(item) is not null);
            if (supportMatchedItem is null)
            {
                _recognizedDevice = null;
                _recognizedInventoryItem = null;
                DeviceTree.ItemsSource = _allDevices;
                ShowAllDevicesButton.Visibility = Visibility.Collapsed;
                FocusRecognizedDeviceButton.Visibility = Visibility.Collapsed;
                DetailsTitle.Text = "Select a component";
                DetailsRole.Text = string.Empty;
                DetailsText.Text = "Installed driver information is local Windows evidence. Firmware fields remain unknown unless this source exposes them.";
                SetOfficialSupportGuidance(null);
                SetOfficialUpdateGuidance(null);
            }
            else
            {
                _recognizedInventoryItem = supportMatchedItem;
                HardwareInventoryNode? recognizedNode = HardwareInventoryTreeSearch.FindByInstanceId(roots, supportMatchedItem.InstanceId);
                _recognizedDevice = recognizedNode is null ? null : HardwareInventoryViewItem.FromNode(recognizedNode);
                if (_recognizedDevice is null)
                {
                    DeviceTree.ItemsSource = _allDevices;
                    ShowAllDevicesButton.Visibility = Visibility.Collapsed;
                    FocusRecognizedDeviceButton.Visibility = Visibility.Collapsed;
                }
                else
                {
                    ShowRecognizedDevice();
                }

                DetailsTitle.Text = $"Recognized device: {supportMatchedItem.DisplayName}";
                DetailsRole.Text = HardwareRoleExplainer.Explain(supportMatchedItem);
                DetailsText.Text = _recognizedDevice is null
                    ? "A reliable local match has official support guidance. The full Windows device tree remains available for inspection."
                    : "A reliable local match has official support guidance. This view starts at the recognized device and shows its reported children. You can return to the full Windows device tree at any time.";
                SetOfficialSupportGuidance(OfficialSupportGuidanceResolver.Resolve(supportMatchedItem));
                SetOfficialUpdateGuidance(OfficialVendorUpdateGuidanceResolver.Resolve(supportMatchedItem));
            }
            UpdateStatus(snapshot);
            EvidenceText.Text = snapshot.Reason ?? $"Identity source: {snapshot.Evidence.Source}.";
        }
        catch (Exception exception)
        {
            DeviceTree.ItemsSource = null;
            _allDevices = [];
            _recognizedDevice = null;
            _recognizedInventoryItem = null;
            ShowAllDevicesButton.Visibility = Visibility.Collapsed;
            FocusRecognizedDeviceButton.Visibility = Visibility.Collapsed;
            DetailsTitle.Text = "Inventory unavailable";
            DetailsRole.Text = string.Empty;
            DetailsText.Text = "No inventory result is presented because the read failed.";
            SetOfficialSupportGuidance(null);
            SetOfficialUpdateGuidance(null);
            StatusText.Text = $"Read-only inventory failed: {exception.Message}";
            EvidenceText.Text = string.Empty;
        }
        finally
        {
            _refreshing = false;
        }
    }

    private void ShowRecognizedDevice()
    {
        if (_recognizedDevice is null)
            return;

        DeviceTree.ItemsSource = new[] { _recognizedDevice };
        ShowAllDevicesButton.Visibility = Visibility.Visible;
        FocusRecognizedDeviceButton.Visibility = Visibility.Collapsed;
    }

    private void UpdateStatus(HardwareInventorySnapshot snapshot)
    {
        string scope = _recognizedDevice is null
            ? "Showing the full Windows device tree."
            : $"Showing {_recognizedDevice.DisplayName} and its reported children.";
        StatusText.Text = $"Read-only inventory refreshed: {snapshot.ObservedAt.ToLocalTime():T}. {snapshot.Items.Count} Windows-reported components. {scope}";
    }

    private void UpdateStatusForAllDevices()
    {
        if (StatusText.Text.StartsWith("Read-only inventory refreshed:", StringComparison.Ordinal))
            StatusText.Text = StatusText.Text.Replace($"Showing {_recognizedDevice?.DisplayName} and its reported children.", "Showing the full Windows device tree.", StringComparison.Ordinal);
    }

    private void DeviceTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (e.NewValue is not HardwareInventoryViewItem item)
            return;

        DetailsTitle.Text = item.DisplayName;
        DetailsRole.Text = item.Role;
        DetailsText.Text = Describe(item.Item);
        SetOfficialSupportGuidance(OfficialSupportGuidanceResolver.Resolve(item.Item));
        SetOfficialUpdateGuidance(OfficialVendorUpdateGuidanceResolver.Resolve(item.Item));
    }

    private void SetOfficialSupportGuidance(OfficialSupportGuidance? guidance)
    {
        if (guidance is null)
        {
            OfficialSupportText.Text = "Official support guidance: no link is shown because a reliable manufacturer-and-model match has not been established. Update availability is unknown.";
            OpenOfficialSupportButton.Tag = null;
            OpenOfficialSupportButton.Visibility = Visibility.Collapsed;
            return;
        }

        OfficialSupportText.Text = $"Official support guidance: {guidance.Title}\n{guidance.MatchReason}\n{guidance.SupportUri}\n{guidance.Evidence.Note}";
        OpenOfficialSupportButton.Content = $"Open {guidance.Provider} support";
        OpenOfficialSupportButton.Tag = guidance.SupportUri;
        OpenOfficialSupportButton.Visibility = Visibility.Visible;
    }

    private void OpenOfficialSupport_Click(object sender, RoutedEventArgs e)
    {
        if (OpenOfficialSupportButton.Tag is not Uri supportUri)
            return;

        OpenUri(supportUri, "Official support");
    }

    private void SetOfficialUpdateGuidance(OfficialVendorUpdateGuidance? guidance)
    {
        if (guidance is null)
        {
            OfficialUpdateText.Text = "Official update guidance: update status is unknown because no reliable manufacturer-and-model catalog match and comparable local version evidence have both been established.";
            OpenOfficialUpdateButton.Tag = null;
            OpenOfficialUpdateButton.Visibility = Visibility.Collapsed;
            return;
        }

        OfficialUpdateText.Text = $"Official update guidance: {guidance.Title}\nStatus: {guidance.Availability}\n{guidance.AvailabilityReason}\n{guidance.SafetyGuidance}\n{guidance.Evidence.Note}";
        OpenOfficialUpdateButton.Content = $"Open {guidance.Provider} drivers and downloads";
        OpenOfficialUpdateButton.Tag = guidance.CatalogUri;
        OpenOfficialUpdateButton.Visibility = Visibility.Visible;
    }

    private void OpenOfficialUpdate_Click(object sender, RoutedEventArgs e)
    {
        if (OpenOfficialUpdateButton.Tag is not Uri catalogUri)
            return;

        OpenUri(catalogUri, "Official drivers and downloads");
    }

    private void OpenUri(Uri uri, string label)
    {
        try
        {
            Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
        }
        catch (Exception exception)
        {
            StatusText.Text = $"{label} could not be opened: {exception.Message}";
        }
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
