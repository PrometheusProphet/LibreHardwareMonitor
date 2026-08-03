// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System.Windows;

namespace ComputerVitals.Wpf;

public partial class App : Application
{
    private WindowsAppNotificationService? _notifications;

    private void OnStartup(object sender, StartupEventArgs e)
    {
        _notifications = new WindowsAppNotificationService();
        string notificationStatus = _notifications.TryRegister();

        MainWindow window = new(_notifications, notificationStatus);
        _notifications.Activated += (_, _) => Dispatcher.Invoke(() =>
        {
            window.Show();
            window.Activate();
        });

        MainWindow = window;
        window.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _notifications?.Dispose();
        base.OnExit(e);
    }
}
