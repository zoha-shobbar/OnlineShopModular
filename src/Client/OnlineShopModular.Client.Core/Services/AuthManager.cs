﻿using OnlineShopModular.Shared.Dtos.Identity;
using OnlineShopModular.Shared.Controllers.Identity;
using OnlineShopModular.Client.Core.Services.HttpMessageHandlers;

namespace OnlineShopModular.Client.Core.Services;

public partial class AuthManager : AuthenticationStateProvider, IAsyncDisposable
{
    private Action? unsubscribe;

    [AutoInject] private Cookie cookie = default!;
    [AutoInject] private IJSRuntime jsRuntime = default!;
    [AutoInject] private PubSubService pubSubService = default!;
    [AutoInject] private PromptService promptService = default!;
    [AutoInject] private IStorageService storageService = default!;
    [AutoInject] private IUserController userController = default!;
    [AutoInject] private ILogger<AuthManager> authLogger = default!;
    [AutoInject] private IAuthTokenProvider tokenProvider = default!;
    [AutoInject] private ITelemetryContext telemetryContext = default!;
    [AutoInject] private IExceptionHandler exceptionHandler = default!;
    [AutoInject] private IStringLocalizer<AppStrings> localizer = default!;
    [AutoInject] private IIdentityController identityController = default!;
    [AutoInject] private IAuthorizationService authorizationService = default!;

    public void OnInit()
    {
        // Example for method call after object instantiation with dependency injection.

        unsubscribe = pubSubService.Subscribe(SharedPubSubMessages.SESSION_REVOKED, _ => SignOut(default));
    }

    /// <summary>
    /// Sign in and return whether the user requires two-factor authentication.
    /// </summary>
    /// <returns>true if the user requires two-factor authentication; otherwise, false.</returns>
    public async Task<bool> SignIn(SignInRequestDto request, CancellationToken cancellationToken)
    {
        var response = await identityController.SignIn(request, cancellationToken);

        if (response.RequiresTwoFactor) return true;

        await StoreTokens(response!, request.RememberMe);

        return false;
    }

    public async Task StoreTokens(TokenResponseDto response, bool? rememberMe = null)
    {
        rememberMe ??= await storageService.IsPersistent("refresh_token");

        await storageService.SetItem("access_token", response!.AccessToken, rememberMe is true);
        await storageService.SetItem("refresh_token", response!.RefreshToken, rememberMe is true);

        if (AppPlatform.IsBlazorHybrid is false && jsRuntime.IsInitialized())
        {
            await cookie.Set(new()
            {
                Name = "access_token",
                Value = response.AccessToken,
                MaxAge = rememberMe is true ? response.ExpiresIn : null, // to create a session cookie
                Path = "/",
                SameSite = SameSite.Strict,
                Secure = AppEnvironment.IsDev() is false
            });
        }

        NotifyAuthenticationStateChanged(Task.FromResult(await GetAuthenticationStateAsync()));
    }

    public async Task SignOut(CancellationToken cancellationToken)
    {
        try
        {
            await userController.SignOut(cancellationToken);
        }
        catch (Exception exp) when (exp is ServerConnectionException or UnauthorizedException or ResourceNotFoundException or ClientNotSupportedException)
        {
            // If the client's access token is expired, the client would attempt to refresh it,
            // but if the client is offline or outdated, the refresh token request will fail.
            // These exceptions are intentionally ignored in these cases.
        }
        finally
        {
            await ClearTokens();
        }
    }

    /// <summary>
    /// To prevent multiple simultaneous refresh token requests.
    /// </summary>
    private SemaphoreSlim semaphore = new(1, 1);
    private TaskCompletionSource<string?>? accessTokenTsc = null;

    public Task<string?> RefreshToken(string requestedBy, string? elevatedAccessToken = null)
    {
        if (accessTokenTsc is null)
        {
            accessTokenTsc = new();
            _ = RefreshTokenImplementation();
        }

        return accessTokenTsc.Task;

        async Task RefreshTokenImplementation()
        {
            try
            {
                await semaphore.WaitAsync();
                authLogger.LogInformation("Refreshing access token requested by {RequestedBy}", requestedBy);
                string? refreshToken = await storageService.GetItem("refresh_token");
                try
                {
                    if (string.IsNullOrEmpty(refreshToken))
                        throw new UnauthorizedException(localizer[nameof(AppStrings.YouNeedToSignIn)]);

                    var refreshTokenResponse = await identityController.Refresh(new()
                    {
                        RefreshToken = refreshToken,
                        ElevatedAccessToken = elevatedAccessToken
                    }, default);
                    await StoreTokens(refreshTokenResponse);
                    accessTokenTsc.SetResult(refreshTokenResponse.AccessToken!);
                }
                catch (Exception exp)
                {
                    exceptionHandler.Handle(exp, parameters: new()
                    {
                        { "AdditionalData", "Refreshing access token failed." },
                        { "RefreshTokenRequestedBy", requestedBy }
                    }, displayKind: ExceptionDisplayKind.NonInterrupting);

                    if (exp is UnauthorizedException) // refresh token is also invalid
                    {
                        await ClearTokens();
                    }

                    accessTokenTsc.SetResult(null);
                }
            }
            finally
            {
                accessTokenTsc = null;
                semaphore.Release();
            }
        }
    }

    /// <summary>
    /// Handles the process of determining the user's authentication state based on the availability of access and refresh tokens.
    /// 
    /// - If no access / refresh token exists, an anonymous user object is returned to Blazor.
    /// - If an access token exists, a ClaimsPrincipal is created from it regardless of its expiration status. This ensures:
    ///   - Users can access anonymous-allowed pages without unnecessary delays caused by token refresh attempts **during app startup**.
    ///   - For protected pages, it is typical for these pages to make HTTP requests to secured APIs. In such cases, the `<see cref="AuthDelegatingHandler"/>`
    ///     validates the access token and refreshes it if necessary, keeping Blazor updated with the latest authentication state.
    /// </summary>
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var accessToken = await tokenProvider.GetAccessToken();

            return new AuthenticationState(IAuthTokenProvider.ParseAccessToken(accessToken, validateExpiry: false));
        }
        catch (Exception exp)
        {
            exceptionHandler.Handle(exp); // Do not throw exceptions in GetAuthenticationStateAsync. This will fault CascadingAuthenticationState's state unless NotifyAuthenticationStateChanged is called again.
            return new AuthenticationState(IAuthTokenProvider.Anonymous());
        }
    }

    public async Task<bool> TryEnterElevatedAccessMode(CancellationToken cancellationToken)
    {
        var user = IAuthTokenProvider.ParseAccessToken(await tokenProvider.GetAccessToken(), validateExpiry: true);
        var hasElevatedAccess = await authorizationService.IsAuthorizedAsync(user, AuthPolicies.ELEVATED_ACCESS);
        if (hasElevatedAccess)
            return true;

        try
        {
            await userController.SendElevatedAccessToken(cancellationToken);
        }
        catch (TooManyRequestsExceptions exp)
        {
            exceptionHandler.Handle(exp, displayKind: ExceptionDisplayKind.NonInterrupting); // Let's show prompt anyway.
        }

        var token = await promptService.Show(localizer[AppStrings.EnterElevatedAccessToken], title: "OnlineShopModular", otpInput: true);
        if (string.IsNullOrEmpty(token))
            return false;

        if (accessTokenTsc != null)
        {
            await accessTokenTsc.Task; // Wait for any ongoing token refresh to complete.
        }
        var accessToken = await RefreshToken(requestedBy: "RequestElevatedAccess", token);
        return string.IsNullOrEmpty(accessToken) is false;
    }

    public async Task<string?> GetFreshAccessToken(string requestedBy)
    {
        var accessToken = await tokenProvider.GetAccessToken();

        if (string.IsNullOrEmpty(accessToken))
            return null;

        var isValid = IAuthTokenProvider.ParseAccessToken(accessToken, validateExpiry: true).IsAuthenticated();

        if (isValid) return accessToken;

        return await RefreshToken(requestedBy);
    }

    private async Task ClearTokens()
    {
        await storageService.RemoveItem("access_token");
        await storageService.RemoveItem("refresh_token");
        if (AppPlatform.IsBlazorHybrid is false)
        {
            await cookie.Remove(new ButilCookie()
            {
                Name = "access_token",
                Path = "/",
                SameSite = SameSite.Strict,
                Secure = AppEnvironment.IsDev() is false
            });
        }
        NotifyAuthenticationStateChanged(Task.FromResult(await GetAuthenticationStateAsync()));
    }

    public async ValueTask DisposeAsync()
    {
        semaphore.Dispose();
        unsubscribe?.Invoke();
    }
}
