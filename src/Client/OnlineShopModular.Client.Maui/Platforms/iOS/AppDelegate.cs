﻿using UIKit;
using Foundation;
using OnlineShopModular.Client.Maui.Platforms.iOS.Services;

namespace OnlineShopModular.Client.Maui.Platforms.iOS;

[Register(nameof(AppDelegate))]
public partial class AppDelegate : MauiUIApplicationDelegate
{
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    private IPushNotificationService NotificationService => IPlatformApplication.Current!.Services.GetRequiredService<IPushNotificationService>();

    [Export("application:didFinishLaunchingWithOptions:")]
    public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
    {
        NotificationService.IsAvailable(default).ContinueWith(async task =>
        {
            if (task.Result)
            {
                await iOSPushNotificationService.Configure();
            }
        });

        // Use the following code the get the action value from the push notification when the app is launched by tapping on the push notification.
        using var userInfo = launchOptions?.ObjectForKey(UIApplication.LaunchOptionsRemoteNotificationKey) as NSDictionary;
        // var actionValue = userInfo?.ObjectForKey(new NSString("action")) as NSString;
        var pageUrl = userInfo?.ObjectForKey(new NSString("pageUrl")) as NSString;
        if (pageUrl != null)
        {
            _ = Core.Components.Routes.OpenUniversalLink(pageUrl);
        }

        return base.FinishedLaunching(application, launchOptions!);
    }

    [Export("application:didRegisterForRemoteNotificationsWithDeviceToken:")]
    public async void RegisteredForRemoteNotifications(UIApplication application, NSData deviceToken)
    {
        try
        {
            NotificationService.Token = deviceToken.ToHexString()!;
            await NotificationService.Subscribe(default);
        }
        catch (Exception exp)
        {
            IPlatformApplication.Current!.Services.GetRequiredService<IExceptionHandler>().Handle(exp);
        }
    }

    [Export("application:didFailToRegisterForRemoteNotificationsWithError:")]
    public void FailedToRegisterForRemoteNotifications(UIApplication application, NSError error)
    {
        IPlatformApplication.Current!.Services.GetRequiredService<IExceptionHandler>().Handle(new InvalidOperationException(error.Description.ToString()));
    }
}
