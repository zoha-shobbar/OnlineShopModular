﻿using Foundation;
using UserNotifications;

namespace OnlineShopModular.Client.Maui.Platforms.MacCatalyst.Services;
public partial class AppUNUserNotificationCenterDelegate : UNUserNotificationCenterDelegate
{
    public override void DidReceiveNotificationResponse(UNUserNotificationCenter center, UNNotificationResponse response, Action completionHandler)
    {
        // Runs when user taps on push notification.
        // Use the following code to get the action value from the tapped push notification.
        // var actionValue = response.Notification.Request.Content.UserInfo.ObjectForKey(new NSString("action")) as NSString;
        var pageUrl = response.Notification.Request.Content.UserInfo.ObjectForKey(new NSString("pageUrl")) as NSString;
        if (pageUrl != null)
        {
            _ = Core.Components.Routes.OpenUniversalLink(pageUrl);
        }
    }

    public override void WillPresentNotification(UNUserNotificationCenter center, UNNotification notification, Action<UNNotificationPresentationOptions> completionHandler)
    {
        // Displays the notification when the app is in the foreground.
        completionHandler(UNNotificationPresentationOptions.Alert |
            UNNotificationPresentationOptions.Badge |
            UNNotificationPresentationOptions.Sound);

        // Use the following code to get the action value from the push notification.
        // var actionValue = notification.Request.Content.UserInfo.ObjectForKey(new NSString("action")) as NSString;
    }
}
