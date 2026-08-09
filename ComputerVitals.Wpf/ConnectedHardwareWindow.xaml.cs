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
    private readonly IReadOnlyList<TemperatureSample> _temperatureSamples;
    private IReadOnlyList<HardwareInventoryViewItem> _allDevices = [];
    private HardwareInventoryNavigator? _navigator;
    private HardwareInventoryViewItem? _selectedDevice;
    private HardwareInventoryItem? _recognizedInventoryItem;
    private HardwareInventorySnapshot? _lastSnapshot;
    private bool _refreshing;

    public ConnectedHardwareWindow(IReadOnlyList<TemperatureSample> temperatureSamples)
    {
        ArgumentNullException.ThrowIfNull(temperatureSamples);
        _temperatureSamples = temperatureSamples.ToArray();
        InitializeComponent();
        Loaded += async (_, _) => await RefreshAsync();
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e) => await RefreshAsync();

    private void ShowAllDevices_Click(object sender, RoutedEventArgs e)
    {
        _navigator?.ShowAll();
        DeviceTree.ItemsSource = _allDevices;
        ShowAllDevicesButton.Visibility = Visibility.Collapsed;
        UpdateFocusSelectedParentButton();
        UpdateStatus();
    }

    private void FocusSelectedParent_Click(object sender, RoutedEventArgs e)
    {
        if (_navigator is null || _selectedDevice is null || !_navigator.Focus(_selectedDevice.Item.InstanceId))
            return;

        ShowFocusedParent();
    }

    private void IncidentTimeline_Click(object sender, RoutedEventArgs e)
    {
        HardwareInventoryItem? contextItem = _selectedDevice?.Item ?? _recognizedInventoryItem;
        new IncidentTimelineWindow(_temperatureSamples.ToArray(), contextItem) { Owner = this }.Show();
    }

    private async Task RefreshAsync()
    {
        if (_refreshing)
            return;

        _refreshing = true;
        try
        {
            HardwareInventorySnapshot snapshot = await _inventoryProbe.ReadAsync();
            IReadOnlyList<HardwareInventoryNode> roots = HardwareInventoryTreeBuilder.Build(snapshot.Items);
            _lastSnapshot = snapshot;
            _navigator = new HardwareInventoryNavigator(roots);
            _allDevices = roots.Select(HardwareInventoryViewItem.FromNode).ToArray();
            _selectedDevice = null;
            HardwareInventoryItem? supportMatchedItem = snapshot.Items.FirstOrDefault(item =>
                OfficialSupportGuidanceResolver.Resolve(item) is not null);
            if (supportMatchedItem is null)
            {
                _recognizedInventoryItem = null;
                DeviceTree.ItemsSource = _allDevices;
                ShowAllDevicesButton.Visibility = Visibility.Collapsed;
                FocusSelectedParentButton.Visibility = Visibility.Collapsed;
                DetailsTitle.Text = "Select a component";
                DetailsRole.Text = string.Empty;
                DetailsText.Text = "Installed driver information is local Windows evidence. Firmware fields remain unknown unless this source exposes them.";
                SetOfficialSupportGuidance(null);
                SetOfficialUpdateGuidance(null);
            }
            else
            {
                _recognizedInventoryItem = supportMatchedItem;
                if (!_navigator.Focus(supportMatchedItem.InstanceId))
                {
                    DeviceTree.ItemsSource = _allDevices;
                    ShowAllDevicesButton.Visibility = Visibility.Collapsed;
                    FocusSelectedParentButton.Visibility = Visibility.Collapsed;
                }
                else
                {
                    ShowFocusedParent();
                }

                DetailsTitle.Text = $"Recognized device: {supportMatchedItem.DisplayName}";
                DetailsRole.Text = HardwareRoleExplainer.Explain(supportMatchedItem);
                DetailsText.Text = _navigator.FocusedParent is null
                    ? "A reliable local match has official support guidance. The full Windows device tree remains available for inspection."
                    : "A reliable local match has official support guidance. This view starts at the recognized device and shows its reported children. You can return to the full Windows device tree at any time.";
                SetOfficialSupportGuidance(OfficialSupportGuidanceResolver.Resolve(supportMatchedItem));
                SetOfficialUpdateGuidance(OfficialVendorUpdateGuidanceResolver.Resolve(supportMatchedItem));
            }
            UpdateStatus();
            EvidenceText.Text = snapshot.Reason ?? $"Identity source: {snapshot.Evidence.Source}.";
        }
        catch (UnauthorizedAccessException)
        {
            PresentNoInventoryResult();
        }
        catch (InvalidOperationException)
        {
            PresentNoInventoryResult();
        }
        catch (System.Runtime.InteropServices.COMException)
        {
            PresentNoInventoryResult();
        }
        finally
        {
            _refreshing = false;
        }
    }

    private void ShowFocusedParent()
    {
        if (_navigator?.FocusedParent is null)
            return;

        DeviceTree.ItemsSource = _navigator.VisibleInventory.Select(HardwareInventoryViewItem.FromNode).ToArray();
        ShowAllDevicesButton.Visibility = Visibility.Visible;
        FocusSelectedParentButton.Visibility = Visibility.Collapsed;
        UpdateStatus();
    }

    private void PresentNoInventoryResult()
    {
        ExpectedFailurePresentation presentation = ExpectedFailurePresentationPolicy.For(ExpectedFailureKind.InventoryRead);
        DeviceTree.ItemsSource = null;
        _allDevices = [];
        _navigator = null;
        _selectedDevice = null;
        _recognizedInventoryItem = null;
        _lastSnapshot = null;
        ShowAllDevicesButton.Visibility = Visibility.Collapsed;
        FocusSelectedParentButton.Visibility = Visibility.Collapsed;
        DetailsTitle.Text = "Inventory unavailable";
        DetailsRole.Text = string.Empty;
        DetailsText.Text = presentation.Detail;
        SetOfficialSupportGuidance(null);
        SetOfficialUpdateGuidance(null);
        StatusText.Text = presentation.Headline;
        EvidenceText.Text = string.Empty;
    }

    private void UpdateStatus()
    {
        if (_lastSnapshot is null)
            return;

        string scope = _navigator?.FocusedParent is null
            ? "Showing the full Windows device tree."
            : $"Showing {_navigator.FocusedParent.Item.DisplayName} and its complete reported subtree.";
        StatusText.Text = $"Read-only inventory refreshed: {_lastSnapshot.ObservedAt.ToLocalTime():T}. {_lastSnapshot.Items.Count} Windows-reported components. {scope}";
    }

    private void DeviceTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (e.NewValue is not HardwareInventoryViewItem item)
            return;

        _selectedDevice = item;
        UpdateFocusSelectedParentButton();
        DetailsTitle.Text = item.DisplayName;
        DetailsRole.Text = item.Role;
        DetailsText.Text = Describe(item.Item);
        SetOfficialSupportGuidance(OfficialSupportGuidanceResolver.Resolve(item.Item));
        SetOfficialUpdateGuidance(OfficialVendorUpdateGuidanceResolver.Resolve(item.Item));
    }

    private void UpdateFocusSelectedParentButton()
    {
        bool canFocus = _selectedDevice is not null &&
            _selectedDevice.Children.Count > 0 &&
            !string.Equals(
                _navigator?.FocusedParent?.Item.InstanceId,
                _selectedDevice.Item.InstanceId,
                StringComparison.OrdinalIgnoreCase);
        FocusSelectedParentButton.Visibility = canFocus ? Visibility.Visible : Visibility.Collapsed;
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
