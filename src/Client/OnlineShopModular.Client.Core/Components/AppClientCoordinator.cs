﻿using System.Web;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.Components.Routing;
using OnlineShopModular.Shared.Controllers.Identity;
using OnlineShopModular.Client.Core.Services.DiagnosticLog;

namespace OnlineShopModular.Client.Core.Components;

/// <summary>
/// Manages the initialization and coordination of core services and settings within the client application.
/// This includes authentication state handling, telemetry setup, culture configuration, and optional
/// services such as SignalR connections, push notifications, and application insights.
/// </summary>
public partial class AppClientCoordinator : AppComponentBase
{
    [AutoInject] private Notification notification = default!;
    [AutoInject] private HubConnection hubConnection = default!;
    [AutoInject] private UserAgent userAgent = default!;
    [AutoInject] private IJSRuntime jsRuntime = default!;
    [AutoInject] private IUserController userController = default!;
    [AutoInject] private IStorageService storageService = default!;
    [AutoInject] private ILogger<AuthManager> authLogger = default!;
    [AutoInject] private ILogger<Navigator> navigatorLogger = default!;
    [AutoInject] private ILogger<AppClientCoordinator> logger = default!;
    [AutoInject] private IBitDeviceCoordinator bitDeviceCoordinator = default!;
    [AutoInject] private IPushNotificationService pushNotificationService = default!;

    private Action? unsubscribe;

    protected override async Task OnInitAsync()
    {
        await base.OnInitAsync();

        if (AppPlatform.IsBlazorHybrid)
        {
            await ConfigureUISetup();
        }

        if (InPrerenderSession is false)
        {
            unsubscribe = PubSubService.Subscribe(ClientPubSubMessages.NAVIGATE_TO, async (uri) =>
            {
                var uriValue = uri?.ToString()!;
                var replace = uriValue.Contains("replace=true", StringComparison.InvariantCultureIgnoreCase);
                var forceLoad = uriValue.Contains("forceLoad=true", StringComparison.InvariantCultureIgnoreCase);
                NavigationManager.NavigateTo(uriValue.Replace("replace=true", "", StringComparison.InvariantCultureIgnoreCase).Replace("forceLoad=true", "", StringComparison.InvariantCultureIgnoreCase).TrimEnd('&'), forceLoad, replace);
            });
            if (AppPlatform.IsBlazorHybrid is false)
            {
                try
                {
                    BitButil.UseFastInvoke(); // Ensures that `TelemetryContext.Platform` is available to components using this value in their `OnInitAsync` method, such as `SignInPage.razor.cs`.
                    var userAgentData = await userAgent.Extract();
                    TelemetryContext.Platform = string.Join(' ', [userAgentData.Manufacturer, userAgentData.OsName, userAgentData.Name, "browser"]);
                }
                finally
                {
                    BitButil.UseNormalInvoke();
                }
            }
            TelemetryContext.TimeZone = await jsRuntime.GetTimeZone();
            TelemetryContext.Culture = CultureInfo.CurrentCulture.Name;
            TelemetryContext.PageUrl = HttpUtility.UrlDecode(NavigationManager.Uri);


            NavigationManager.LocationChanged += NavigationManager_LocationChanged;
            AuthManager.AuthenticationStateChanged += AuthenticationStateChanged;
            SubscribeToSignalREventsMessages();
            await PropagateAuthState(firstRun: true, AuthenticationStateTask);
        }
    }

    private void NavigationManager_LocationChanged(object? sender, LocationChangedEventArgs e)
    {
        TelemetryContext.PageUrl = HttpUtility.UrlDecode(e.Location);
        navigatorLogger.LogInformation("Navigator's location changed to {Location}", TelemetryContext.PageUrl);
    }

    private Guid? lastPropagatedUserId = Guid.Empty;
    /// <summary>
    /// This code manages the association of a user with sensitive services, such as SignalR, push notifications, App Insights, and others, 
    /// ensuring the user is correctly set or cleared as needed.
    /// </summary>
    public async Task PropagateAuthState(bool firstRun, Task<AuthenticationState> task)
    {
        try
        {
            var user = (await task).User;
            var isAuthenticated = user.IsAuthenticated();
            var userId = isAuthenticated ? user.GetUserId() : (Guid?)null;
            if (lastPropagatedUserId == userId)
                return;
            await Abort(); // Cancels ongoing user id propagation, because the new authentication state is available.
            TelemetryContext.UserId = userId;
            TelemetryContext.UserSessionId = isAuthenticated ? user.GetSessionId() : null;

            // Typically, we use the logger directly without utilizing logger.BeginScope.
            // While many loggers provide specific methods to set userId and other context-related information,
            // we use this method to propagate the user ID and other telemetry contexts via Microsoft.Extensions.Logging's Scope feature.
            // PropagateUserId method is invoked both during app startup and when the authentication state changes.
            // Additionally, this is a convenient place to manage user-specific contexts for services like:
            // - App Insights: Set or clear the user ID for tracking purposes.
            // - Push Notifications: Update subscriptions to ensure user-specific notifications are routed to the correct devices.
            // - SignalR: Map connection IDs to a user's group of connections for message targeting.
            // By leveraging this method during authentication state changes, we streamline the propagation of user-specific contexts across these systems.


            var data = TelemetryContext.ToDictionary();
            using var scope = authLogger.BeginScope(data);
            {
                authLogger.LogInformation("Propagating {AuthStateType} {AuthState} authentication state.", firstRun ? "Initial" : "Updated", user.IsAuthenticated() ? "Authenticated" : "Anonymous");
            }

            await EnsureSignalRStarted();

            await pushNotificationService.Subscribe(CurrentCancellationToken);

            if (isAuthenticated)
            {
                await UpdateUserSession();
            }

            lastPropagatedUserId = userId;
        }
        catch (Exception exp)
        {
            ExceptionHandler.Handle(exp);
        }
    }

    private void AuthenticationStateChanged(Task<AuthenticationState> task)
    {
        _ = PropagateAuthState(firstRun: false, task);
    }

    private void SubscribeToSignalREventsMessages()
    {
        signalROnDisposables.Add(hubConnection.On<string, object, bool>(SignalREvents.SHOW_MESSAGE, async (message, data) =>
        {
            logger.LogInformation("SignalR Message {Message} received from server to show.", message);
            if (await notification.IsNotificationAvailable())
            {
                // Show local notification
                // Note that this code has nothing to do with push notification.
                await notification.Show("OnlineShopModular SignalR", new()
                {
                    Icon = "/images/icons/bit-icon-512.png",
                    Body = message,
                    Data = data
                });
            }
            else
            {
                if (data is not null) return false; // Snack bar service does not support payload data. It would be a good idea to return false to the server so server knows that the message was not shown.

                SnackBarService.Show("OnlineShopModular", message);
            }

            return true; // Message gets shown successfully. You CAN (not implemented yet) use this in server side in order to not to send push notifications for messages that are already shown in the client side.

            // The following code block is not required for Bit.BlazorUI components to perform UI changes. However, it may be necessary in other scenarios.
            /*await InvokeAsync(async () =>
            {
                StateHasChanged();
            });*/

            // You can also leverage IPubSubService to notify other components in the application.
        }));

        signalROnDisposables.Add(hubConnection.On<string, object?>(SignalREvents.PUBLISH_MESSAGE, async (message, payload) =>
        {
            logger.LogInformation("SignalR Message {Message} received from server to publish.", message);
            PubSubService.Publish(message, payload);
        }));

        signalROnDisposables.Add(hubConnection.On<AppProblemDetails>(SignalREvents.EXCEPTION_THROWN, async (appProblemDetails) =>
        {
            ExceptionHandler.Handle(appProblemDetails, displayKind: ExceptionDisplayKind.NonInterrupting);
        }));

        signalROnDisposables.Add(hubConnection.On(SignalRMethods.UPLOAD_DIAGNOSTIC_LOGGER_STORE, async () =>
        {
            return DiagnosticLogger.Store.ToArray();
        }));

        hubConnection.Closed += HubConnectionStateChange;
        hubConnection.Reconnected += HubConnectionConnected;
        hubConnection.Reconnecting += HubConnectionStateChange;
    }

    private async Task EnsureSignalRStarted()
    {
        try
        {
            if (hubConnection.State is not HubConnectionState.Connected or HubConnectionState.Connecting)
            {
                await hubConnection.StartAsync(CurrentCancellationToken);
                await HubConnectionConnected(null);
            }
            else
            {
                await hubConnection.InvokeAsync("ChangeAuthenticationState", await AuthTokenProvider.GetAccessToken(), CurrentCancellationToken);
            }
        }
        catch (Exception exp)
        {
            await HubConnectionStateChange(exp);
        }
    }

    private async Task HubConnectionConnected(string? _)
    {
        PubSubService.Publish(ClientPubSubMessages.IS_ONLINE_CHANGED, true);
        logger.LogInformation("SignalR connection established.");
    }

    private async Task HubConnectionStateChange(Exception? exception)
    {
        PubSubService.Publish(ClientPubSubMessages.IS_ONLINE_CHANGED, exception is null && hubConnection!.State is HubConnectionState.Connected);

        if (exception is null)
        {
            logger.LogInformation("SignalR state changed to {State}", hubConnection!.State);
        }
        else
        {
            logger.LogWarning(exception, "SignalR connection lost.");

            if (exception is HubException && exception.Message.EndsWith(nameof(AppStrings.UnauthorizedException)))
            {
                await AuthManager.RefreshToken(requestedBy: nameof(HubException));
            }
        }
    }


    private async Task UpdateUserSession()
    {
        await userController.UpdateSession(new()
        {
            AppVersion = TelemetryContext.AppVersion,
            DeviceInfo = TelemetryContext.Platform,
            CultureName = CultureInfoManager.InvariantGlobalization ? null : CultureInfo.CurrentUICulture.Name,
            PlatformType = AppPlatform.Type
        }, CurrentCancellationToken);
    }

    private async Task ConfigureUISetup()
    {
        if (CultureInfoManager.InvariantGlobalization is false)
        {
            CultureInfoManager.SetCurrentCulture(new Uri(NavigationManager.Uri).GetCulture() ??  // 1- Culture query string OR Route data request culture
                                                 await storageService.GetItem("Culture") ?? // 2- User settings
                                                 CultureInfo.CurrentUICulture.Name); // 3- OS settings
        }
    }

    private List<IDisposable> signalROnDisposables = [];
    protected override async ValueTask DisposeAsync(bool disposing)
    {
        await base.DisposeAsync(disposing);

        unsubscribe?.Invoke();

        NavigationManager.LocationChanged -= NavigationManager_LocationChanged;
        AuthManager.AuthenticationStateChanged -= AuthenticationStateChanged;

        hubConnection.Closed -= HubConnectionStateChange;
        hubConnection.Reconnected -= HubConnectionConnected;
        hubConnection.Reconnecting -= HubConnectionStateChange;
        signalROnDisposables.ForEach(d => d.Dispose());
    }
}
