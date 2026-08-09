// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.

using ComputerVitals.Core;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;

namespace ComputerVitals.Wpf;

public sealed class WindowsAppNotificationService : IDisposable
{
    private bool _registered;

    public event EventHandler? Activated;

    public string TryRegister()
    {
        try
        {
            AppNotificationManager.Default.NotificationInvoked += OnNotificationInvoked;
            AppNotificationManager.Default.Register();
            _registered = true;
            return "Local Windows notifications are available. No alert is enabled until you configure a threshold.";
        }
        catch (UnauthorizedAccessException)
        {
            return PresentRegistrationFailure().Detail;
        }
        catch (InvalidOperationException)
        {
            return PresentRegistrationFailure().Detail;
        }
        catch (System.Runtime.InteropServices.COMException)
        {
            return PresentRegistrationFailure().Detail;
        }
    }

    public ExpectedFailurePresentation? Show(TemperatureAlert alert)
    {
        if (!_registered)
            return ExpectedFailurePresentationPolicy.For(ExpectedFailureKind.NotificationDelivery);

        try
        {
            TemperatureAlertNotificationContent content = TemperatureAlertNotificationContent.FromAlert(alert);
            AppNotification notification = new AppNotificationBuilder()
                .AddArgument("action", "show-temperature")
                .AddText(content.Title)
                .AddText(content.Body)
                .BuildNotification();
            AppNotificationManager.Default.Show(notification);
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return PresentDeliveryFailure();
        }
        catch (InvalidOperationException)
        {
            return PresentDeliveryFailure();
        }
        catch (System.Runtime.InteropServices.COMException)
        {
            return PresentDeliveryFailure();
        }
    }

    public void Dispose()
    {
        if (!_registered)
            return;

        AppNotificationManager.Default.NotificationInvoked -= OnNotificationInvoked;
        AppNotificationManager.Default.Unregister();
        _registered = false;
    }

    private void OnNotificationInvoked(AppNotificationManager sender, AppNotificationActivatedEventArgs args) =>
        Activated?.Invoke(this, EventArgs.Empty);

    private ExpectedFailurePresentation PresentRegistrationFailure()
    {
        AppNotificationManager.Default.NotificationInvoked -= OnNotificationInvoked;
        _registered = false;
        return ExpectedFailurePresentationPolicy.For(ExpectedFailureKind.NotificationRegistration);
    }

    private ExpectedFailurePresentation PresentDeliveryFailure()
    {
        _registered = false;
        return ExpectedFailurePresentationPolicy.For(ExpectedFailureKind.NotificationDelivery);
    }
}
