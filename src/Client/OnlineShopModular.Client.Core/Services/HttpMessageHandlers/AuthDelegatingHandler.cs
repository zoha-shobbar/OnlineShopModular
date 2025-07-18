﻿using System.Net.Http.Headers;
using OnlineShopModular.Shared.Controllers.Identity;

namespace OnlineShopModular.Client.Core.Services.HttpMessageHandlers;

public partial class AuthDelegatingHandler(IJSRuntime jsRuntime,
                                           IStorageService storageService,
                                           IServiceProvider serviceProvider,
                                           IAuthTokenProvider tokenProvider,
                                           IStringLocalizer<AppStrings> localizer,
                                           HttpMessageHandler handler) : DelegatingHandler(handler)
{

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var logScopeData = (Dictionary<string, object?>)request.Options.GetValueOrDefault(RequestOptionNames.LogScopeData)!;
        var isInternalRequest = request.HasExternalApiAttribute() is false;

        try
        {
            if (isInternalRequest && /* The access token will be sent exclusively to the application's own server. */
                request.Headers.Authorization is null)
            {
                var accessToken = await tokenProvider.GetAccessToken();
                if (string.IsNullOrEmpty(accessToken) is false && request.HasAuthorizedApiAttribute())
                {
                    if (IAuthTokenProvider.ParseAccessToken(accessToken, validateExpiry: true).IsAuthenticated() is false)
                    {
                        logScopeData["ClientSideAccessTokenValidationFailed"] = true;
                        throw new UnauthorizedException(localizer[nameof(AppStrings.YouNeedToSignIn)]);
                    }
                }
                request.Headers.Authorization = string.IsNullOrEmpty(accessToken) ? null : new AuthenticationHeaderValue("Bearer", accessToken);
            }

            return await base.SendAsync(request, cancellationToken);
        }
        catch (KnownException _) when (_ is ForbiddenException or UnauthorizedException)
        {
            // Notes about ForbiddenException (403):
            // Let's update the access token by refreshing it when a refresh token is available.
            // Following this procedure, the newly acquired access token may now include the necessary roles or claims.

            if (isInternalRequest is false)
                throw;

            if (AppPlatform.IsBlazorHybrid is false && jsRuntime.IsInitialized() is false)
                throw; // The `refreshToken` is not accessible during the pre-rendering phase.

            var isRefreshTokenRequest = request.RequestUri?.LocalPath?.Contains(IIdentityController.RefreshUri, StringComparison.InvariantCultureIgnoreCase) is true;

            if (isRefreshTokenRequest)
                throw; // To prevent refresh token loop

            var refreshToken = await storageService.GetItem("refresh_token");
            if (string.IsNullOrEmpty(refreshToken)) throw;

            var authManager = serviceProvider.GetRequiredService<AuthManager>();

            logScopeData["RefreshTokenRequested"] = true;
            var accessToken = await authManager.RefreshToken(requestedBy: nameof(AuthDelegatingHandler));

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
