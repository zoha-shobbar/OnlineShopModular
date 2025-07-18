﻿using OnlineShopModular.Server.Api.Services;
using Microsoft.AspNetCore.Authentication;

namespace OnlineShopModular.Server.Api.Controllers.Identity;

public partial class IdentityController
{
    [AutoInject] private ServerExceptionHandler serverExceptionHandler = default!;
    [AutoInject] private IAuthenticationSchemeProvider authenticationSchemeProvider = default!;

    [HttpGet]
    [AppResponseCache(SharedMaxAge = 3600 * 24 * 7, MaxAge = 60 * 5)]
    public async Task<string> GetSocialSignInUri(string provider, string? returnUrl = null, int? localHttpPort = null, CancellationToken cancellationToken = default)
    {
        var uri = Url.Action(nameof(SocialSignIn), new { provider, returnUrl, localHttpPort, origin = Request.GetWebAppUrl() })!;
        return new Uri(Request.GetBaseUrl(), uri).ToString();
    }

    [HttpGet]
    public async Task<ActionResult> SocialSignIn(string provider,
        string? returnUrl = null, /* Specifies the relative page address to navigate to after completion. */
        int? localHttpPort = null, /* Defines the local HTTP server port awaiting the social sign-in result on Windows/macOS/iOS versions of the app. */
        [FromQuery] string? origin = null /* Indicates the base address URL for redirection after the process completes. */ )
    {
        var redirectUrl = Url.Action(nameof(SocialSignInCallback), "Identity", new { returnUrl, localHttpPort, origin });
        var properties = signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
        return new ChallengeResult(provider, properties);
    }

    [HttpGet]
    public async Task<ActionResult> SocialSignInCallback(string? returnUrl = null, int? localHttpPort = null, CancellationToken cancellationToken = default)
    {
        string? signInPageUri;
        ExternalLoginInfo? info = null;

        try
        {
            info = await signInManager.GetExternalLoginInfoAsync() ?? throw new BadRequestException();
            var email = info.Principal.GetEmail();
            var phoneNumber = phoneService.NormalizePhoneNumber(info.Principal.Claims.FirstOrDefault(c => c.Type is ClaimTypes.HomePhone or ClaimTypes.MobilePhone or ClaimTypes.OtherPhone)?.Value);

            var user = await userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);

            if (user is null && (string.IsNullOrEmpty(email) is false || string.IsNullOrEmpty(phoneNumber) is false))
            {
                user = await userManager.FindUserAsync(new() { Email = email, PhoneNumber = phoneNumber });
            }

            if (user is null)
            {
                var name = info.Principal.FindFirstValue("preferred_username") ?? info.Principal.FindFirstValue(ClaimTypes.Name) ?? info.Principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? info.Principal.FindFirstValue("name");
                // Instead of automatically creating a user here, you can navigate to the sign-up page and pass the email and phone number in the query string.

                user = new()
                {
                    FullName = name,
                    LockoutEnabled = true
                };

                await userStore.SetUserNameAsync(user, Guid.NewGuid().ToString(), cancellationToken);

                if (string.IsNullOrEmpty(email) is false)
                {
                    await userEmailStore.SetEmailAsync(user, email, cancellationToken);
                }

                if (string.IsNullOrEmpty(phoneNumber) is false)
                {
                    await userPhoneNumberStore.SetPhoneNumberAsync(user, phoneNumber!, cancellationToken);
                }

                await userManager.CreateUserWithDemoRole(user);

                await userManager.AddLoginAsync(user, info);
            }

            if (string.IsNullOrEmpty(email) is false && email == user.Email && await userManager.IsEmailConfirmedAsync(user) is false)
            {
                await userEmailStore.SetEmailConfirmedAsync(user, true, cancellationToken);
                await userManager.UpdateAsync(user);
            }

            if (string.IsNullOrEmpty(phoneNumber) is false && phoneNumber == user.PhoneNumber && await userManager.IsPhoneNumberConfirmedAsync(user) is false)
            {
                await userPhoneNumberStore.SetPhoneNumberConfirmedAsync(user, true, cancellationToken);
                await userManager.UpdateAsync(user);
            }

            (_, signInPageUri) = await GenerateAutomaticSignInLink(user, returnUrl, originalAuthenticationMethod: "Social"); // Sign in with a magic link, and 2FA will be prompted if already enabled.
        }
        catch (Exception exp)
        {
            serverExceptionHandler.Handle(exp, new() { { "LoginProvider", info?.LoginProvider }, { "Principal", info?.Principal?.GetDisplayName() } });
            signInPageUri = $"{Urls.SignInPage}?error={Uri.EscapeDataString(exp is KnownException ? Localizer[exp.Message] : Localizer[nameof(AppStrings.UnknownException)])}";
        }
        finally
        {
            await Request.HttpContext.SignOutAsync(IdentityConstants.ExternalScheme); // We'll handle sign-in with the following redirects, so no external identity cookie is needed.
        }

        var redirectRelativeUrl = $"web-interop-app?actionName=SocialSignInCallback&url={Uri.EscapeDataString(signInPageUri!)}&localHttpPort={localHttpPort}";

        if (localHttpPort is not null) 
            return Redirect(new Uri(new Uri($"http://localhost:{localHttpPort}"), redirectRelativeUrl).ToString()); // Check out WebInteropApp.razor's comments.

        return Redirect(new Uri(Request.HttpContext.Request.GetWebAppUrl(), redirectRelativeUrl).ToString());
    }

    [HttpGet]
    [AppResponseCache(SharedMaxAge = 3600 * 24 * 7, MaxAge = 60 * 5)]
    public async Task<string[]> GetSupportedSocialAuthSchemes(CancellationToken cancellationToken = default)
    {
        var schemes = await authenticationSchemeProvider.GetAllSchemesAsync();

        var providers = schemes
            .Where(s => string.IsNullOrEmpty(s.DisplayName) is false && s.Name != IdentityConstants.ExternalScheme)
            .Select(s => s.Name)
            .ToArray();

        return providers;
    }
}
