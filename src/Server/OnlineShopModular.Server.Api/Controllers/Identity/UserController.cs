﻿using System.Text;
using System.Text.Encodings.Web;
using QRCoder;
using Humanizer;
using OnlineShopModular.Server.Api.Services;
using OnlineShopModular.Shared.Dtos.Identity;
using OnlineShopModular.Server.Api.Models.Identity;
using OnlineShopModular.Shared.Controllers.Identity;
using Microsoft.AspNetCore.SignalR;
using OnlineShopModular.Server.Api.SignalR;

namespace OnlineShopModular.Server.Api.Controllers.Identity;

[ApiController, Route("api/[controller]/[action]")]
public partial class UserController : AppControllerBase, IUserController
{
    [AutoInject] private UrlEncoder urlEncoder = default!;
    [AutoInject] private PhoneService phoneService = default!;
    [AutoInject] private EmailService emailService = default!;
    [AutoInject] private IUserStore<User> userStore = default!;
    [AutoInject] private UserManager<User> userManager = default!;
    [AutoInject] private SignInManager<User> signInManager = default;
    [AutoInject] private IUserEmailStore<User> userEmailStore = default!;

    [AutoInject] private PushNotificationService pushNotificationService = default!;

    [AutoInject] private IHubContext<AppHub> appHubContext = default!;

    [HttpGet]
    public async Task<UserDto> GetCurrentUser(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new ResourceNotFoundException();

        return user.Map();
    }

    [HttpGet, EnableQuery]
    public IQueryable<UserSessionDto> GetUserSessions()
    {
        var userId = User.GetUserId();

        return DbContext.UserSessions
            .Where(us => us.UserId == userId)
            .Project()
            .OrderByDescending(us => us.RenewedOn);
    }

    [HttpPost]
    public async Task SignOut(CancellationToken cancellationToken)
    {
        var currentSessionId = User.GetSessionId();

        var userSession = await DbContext.UserSessions
            .FirstOrDefaultAsync(us => us.Id == currentSessionId, cancellationToken) ?? throw new ResourceNotFoundException();

        DbContext.UserSessions.Remove(userSession);
        await DbContext.SaveChangesAsync(cancellationToken);

        await signInManager.SignOutAsync();
    }

    [HttpPost("{id}"), Authorize(Policy = AuthPolicies.ELEVATED_ACCESS)]
    public async Task RevokeSession(Guid id, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        var currentSessionId = User.GetSessionId();

        if (id == currentSessionId)
            throw new BadRequestException(); // "Call SignOut instead"

        var userSession = await DbContext.UserSessions
            .FirstOrDefaultAsync(us => us.Id == id && us.UserId == userId, cancellationToken) ?? throw new ResourceNotFoundException();

        DbContext.UserSessions.Remove(userSession);
        await DbContext.SaveChangesAsync(cancellationToken);

        // Check out AppHub's comments for more info.
        if (userSession.SignalRConnectionId is not null)
        {
            await appHubContext.Clients.Client(userSession.SignalRConnectionId).SendAsync(SignalREvents.PUBLISH_MESSAGE, SharedPubSubMessages.SESSION_REVOKED, null, cancellationToken);
        }
    }

    [HttpPost]
    public async Task UpdateSession(UpdateUserSessionRequestDto request, CancellationToken cancellationToken)
    {
        var affectedRows = await DbContext.UserSessions.Where(us => us.Id == User.GetSessionId()).ExecuteUpdateAsync(us =>
            us.SetProperty(x => x.AppVersion, request.AppVersion)
                .SetProperty(x => x.DeviceInfo, request.DeviceInfo)
                .SetProperty(x => x.PlatformType, request.PlatformType)
                .SetProperty(x => x.CultureName, request.CultureName), cancellationToken);

        if (affectedRows == 0)
            throw new ResourceNotFoundException();
    }

    [HttpPut]
    public async Task<UserDto> Update(EditUserRequestDto userDto, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new ResourceNotFoundException();

        userDto.Patch(user);

        var result = await userManager.UpdateAsync(user);
        if (result.Succeeded is false)
            throw new ResourceValidationException(result.Errors.Select(err => new LocalizedString(err.Code, err.Description)).ToArray());

        var updatedUser = await GetCurrentUser(cancellationToken);

        // Notify other sessions of the user that user's info has been updated, so they'll update their UI.
        var currentUserSessionId = User.GetSessionId();
        var userSessionIdsExceptCurrentUserSessionId = await DbContext.UserSessions
            .Where(us => us.UserId == user.Id && us.Id != currentUserSessionId && us.SignalRConnectionId != null)
            .Select(us => us.SignalRConnectionId!)
            .ToArrayAsync(cancellationToken);
        await appHubContext.Clients.Clients(userSessionIdsExceptCurrentUserSessionId).SendAsync(SignalREvents.PUBLISH_MESSAGE, SharedPubSubMessages.PROFILE_UPDATED, updatedUser, cancellationToken);

        return updatedUser;
    }

    [HttpPost]
    public async Task ChangePassword(ChangePasswordRequestDto request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(User.GetUserId().ToString());

        if (await userManager.IsLockedOutAsync(user!))
        {
            var tryAgainIn = (user!.LockoutEnd! - DateTimeOffset.UtcNow).Value;
            throw new BadRequestException(Localizer[nameof(AppStrings.UserLockedOut), tryAgainIn.Humanize(culture: CultureInfo.CurrentUICulture)]).WithExtensionData("TryAgainIn", tryAgainIn);
        }

        var result = await userManager.ChangePasswordAsync(user!, request.OldPassword!, request.NewPassword!);

        if (result.Succeeded is false)
        {
            await userManager.AccessFailedAsync(user!);

            throw new ResourceValidationException(result.Errors.Select(err => new LocalizedString(err.Code, err.Description)).ToArray());
        }
    }

    [HttpPost]
    public async Task ChangeUserName(ChangeUserNameRequestDto request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(User.GetUserId().ToString());
        var result = await userManager.SetUserNameAsync(user!, request.UserName);
        if (result.Succeeded is false)
            throw new ResourceValidationException(result.Errors.Select(err => new LocalizedString(err.Code, err.Description)).ToArray());
    }

    [HttpPost]
    public async Task SendChangeEmailToken(SendEmailTokenRequestDto request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(User.GetUserId().ToString());

        var resendDelay = (DateTimeOffset.Now - user!.EmailTokenRequestedOn) - AppSettings.Identity.EmailTokenLifetime;

        if (resendDelay < TimeSpan.Zero)
            throw new TooManyRequestsExceptions(Localizer[nameof(AppStrings.WaitForEmailTokenRequestResendDelay), resendDelay.Value.Humanize(culture: CultureInfo.CurrentUICulture)]).WithExtensionData("TryAgainIn", resendDelay);

        user.EmailTokenRequestedOn = DateTimeOffset.Now;
        var result = await userManager.UpdateAsync(user);
        if (result.Succeeded is false)
            throw new ResourceValidationException(result.Errors.Select(e => new LocalizedString(e.Code, e.Description)).ToArray());

        var token = await userManager.GenerateUserTokenAsync(
            user,
            TokenOptions.DefaultPhoneProvider,
            FormattableString.Invariant($"ChangeEmail:{request.Email},{user.EmailTokenRequestedOn?.ToUniversalTime()}"));

        var link = new Uri(
            HttpContext.Request.GetWebAppUrl(),
            $"{Urls.SettingsPage}/{Urls.SettingsSections.Account}?email={Uri.EscapeDataString(request.Email!)}&emailToken={Uri.EscapeDataString(token)}&culture={CultureInfo.CurrentUICulture.Name}");

        await emailService.SendEmailToken(user, request.Email!, token, link, cancellationToken);
    }

    [HttpPost]
    public async Task ChangeEmail(ChangeEmailRequestDto request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(User.GetUserId().ToString());

        var expired = (DateTimeOffset.Now - user!.EmailTokenRequestedOn) > AppSettings.Identity.EmailTokenLifetime;

        if (expired)
            throw new BadRequestException(nameof(AppStrings.ExpiredToken));

        var tokenIsValid = await userManager.VerifyUserTokenAsync(
            user,
            TokenOptions.DefaultPhoneProvider,
            FormattableString.Invariant($"ChangeEmail:{request.Email},{user.EmailTokenRequestedOn?.ToUniversalTime()}"),
            request.Token!);

        if (tokenIsValid is false)
            throw new BadRequestException(nameof(AppStrings.InvalidToken));

        await userEmailStore.SetEmailAsync(user, request.Email, cancellationToken);
        var result = await userManager.UpdateAsync(user);

        if (result.Succeeded is false)
            throw new ResourceValidationException(result.Errors.Select(e => new LocalizedString(e.Code, e.Description)).ToArray());

        await ((IUserLockoutStore<User>)userStore).ResetAccessFailedCountAsync(user, cancellationToken);
        user.EmailTokenRequestedOn = null; // invalidates email token
        var updateResult = await userManager.UpdateAsync(user);

        if (updateResult.Succeeded is false)
            throw new ResourceValidationException(updateResult.Errors.Select(e => new LocalizedString(e.Code, e.Description)).ToArray());
    }

    [HttpPost]
    public async Task SendChangePhoneNumberToken(SendPhoneTokenRequestDto request, CancellationToken cancellationToken)
    {
        request.PhoneNumber = phoneService.NormalizePhoneNumber(request.PhoneNumber);
        var user = await userManager.FindByIdAsync(User.GetUserId().ToString());

        var resendDelay = (DateTimeOffset.Now - user!.PhoneNumberTokenRequestedOn) - AppSettings.Identity.PhoneNumberTokenLifetime;

        if (resendDelay < TimeSpan.Zero)
            throw new TooManyRequestsExceptions(Localizer[nameof(AppStrings.WaitForPhoneNumberTokenRequestResendDelay), resendDelay.Value.Humanize(culture: CultureInfo.CurrentUICulture)]).WithExtensionData("TryAgainIn", resendDelay);

        user.PhoneNumberTokenRequestedOn = DateTimeOffset.Now;
        var result = await userManager.UpdateAsync(user);

        if (result.Succeeded is false)
            throw new ResourceValidationException(result.Errors.Select(e => new LocalizedString(e.Code, e.Description)).ToArray());

        var token = await userManager.GenerateChangePhoneNumberTokenAsync(user!, request.PhoneNumber!);

        var message = Localizer[nameof(AppStrings.ChangePhoneNumberTokenShortText), token];
        var smsMessage = $"{message}{Environment.NewLine}@{HttpContext.Request.GetWebAppUrl().Host} #{token}" /* Web OTP */;

        await phoneService.SendSms(smsMessage, request.PhoneNumber!);
    }

    [HttpPost]
    public async Task ChangePhoneNumber(ChangePhoneNumberRequestDto request, CancellationToken cancellationToken)
    {
        request.PhoneNumber = phoneService.NormalizePhoneNumber(request.PhoneNumber);
        var user = await userManager.FindByIdAsync(User.GetUserId().ToString());

        var expired = (DateTimeOffset.Now - user!.PhoneNumberTokenRequestedOn) > AppSettings.Identity.PhoneNumberTokenLifetime;

        if (expired)
            throw new BadRequestException(nameof(AppStrings.ExpiredToken));

        var result = await userManager.ChangePhoneNumberAsync(user!, request.PhoneNumber!, request.Token!);

        if (result.Succeeded is false)
            throw new ResourceValidationException(result.Errors.Select(e => new LocalizedString(e.Code, e.Description)).ToArray());

        await ((IUserLockoutStore<User>)userStore).ResetAccessFailedCountAsync(user, cancellationToken);
        user.PhoneNumberTokenRequestedOn = null; // invalidates phone token
        var updateResult = await userManager.UpdateAsync(user);

        if (updateResult.Succeeded is false)
            throw new ResourceValidationException(updateResult.Errors.Select(e => new LocalizedString(e.Code, e.Description)).ToArray());
    }

    [HttpDelete, Authorize(Policy = AuthPolicies.ELEVATED_ACCESS)]
    public async Task Delete(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        var user = await userManager.FindByIdAsync(userId.ToString())
                    ?? throw new ResourceNotFoundException();

        var currentSessionId = User.GetSessionId();

        foreach (var userSession in await GetUserSessions().ToArrayAsync(cancellationToken))
        {
            if (userSession.Id == currentSessionId)
            {
                await SignOut(cancellationToken);
            }
            else
            {
                await RevokeSession(userSession.Id, cancellationToken);
            }
        }

        var result = await userManager.DeleteAsync(user);

        if (result.Succeeded is false)
            throw new ResourceValidationException(result.Errors.Select(err => new LocalizedString(err.Code, err.Description)).ToArray());
    }

    [HttpPost, Route("~/api/[controller]/2fa")]
    public async Task<TwoFactorAuthResponseDto> TwoFactorAuth(TwoFactorAuthRequestDto request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var user = await userManager.FindByIdAsync(userId.ToString()) ?? throw new ResourceNotFoundException();

        if (request.Enable is true)
        {
            if (request.ResetSharedKey)
                throw new BadRequestException(Localizer[nameof(AppStrings.TfaResetSharedKeyError)]);
            else if (string.IsNullOrEmpty(request.TwoFactorCode))
                throw new BadRequestException(Localizer[nameof(AppStrings.TfaEmptyCodeError)]);
            else if (await userManager.VerifyTwoFactorTokenAsync(user, userManager.Options.Tokens.AuthenticatorTokenProvider, request.TwoFactorCode) is false)
                throw new BadRequestException(Localizer[nameof(AppStrings.TfaInvalidCodeError)]);

            await userManager.SetTwoFactorEnabledAsync(user, true);
        }
        else if (request.Enable is false || request.ResetSharedKey)
        {
            await userManager.SetTwoFactorEnabledAsync(user, false);
        }

        if (request.ResetSharedKey)
        {
            await userManager.ResetAuthenticatorKeyAsync(user);
        }

        string[]? recoveryCodes = null;
        if (request.ResetRecoveryCodes || (request.Enable == true && await userManager.CountRecoveryCodesAsync(user) == 0))
        {
            var recoveryCodesEnumerable = await userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
            recoveryCodes = recoveryCodesEnumerable?.ToArray();
        }

        //if (tfaRequest.ForgetMachine)
        //{
        //    await signInManager.ForgetTwoFactorClientAsync();
        //}

        var unformattedKey = await userManager.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrEmpty(unformattedKey))
        {
            IUserAuthenticatorKeyStore<User> userAuthenticatorKeyStore = (IUserAuthenticatorKeyStore<User>)userStore;
            await userAuthenticatorKeyStore.SetAuthenticatorKeyAsync(user,
                userManager.GenerateNewAuthenticatorKey(), cancellationToken);
            await userStore.UpdateAsync(user, cancellationToken);
            unformattedKey = await userManager.GetAuthenticatorKeyAsync(user);

            if (string.IsNullOrEmpty(unformattedKey))
            {
                throw new NotSupportedException("The user manager must produce an authenticator key after reset.");
            }
        }

        var sharedKey = FormatKey(unformattedKey);
        var authenticatorUri = GenerateQrCodeUri(user.DisplayName!, unformattedKey);

        var qrCodeBase64 = "";
        var isTwoFactorEnabled = await userManager.GetTwoFactorEnabledAsync(user);
        if (isTwoFactorEnabled is false)
        {
            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(authenticatorUri, QRCodeGenerator.ECCLevel.Q);

            var qrCode = new Base64QRCode(qrCodeData);
            qrCodeBase64 = qrCode.GetGraphic(20);
        }

        return new TwoFactorAuthResponseDto
        {
            SharedKey = sharedKey,
            AuthenticatorUri = authenticatorUri,
            RecoveryCodes = recoveryCodes,
            RecoveryCodesLeft = recoveryCodes?.Length ?? await userManager.CountRecoveryCodesAsync(user),
            IsTwoFactorEnabled = isTwoFactorEnabled,
            //IsMachineRemembered = await signInManager.IsTwoFactorClientRememberedAsync(user),
            QrCode = qrCodeBase64
        };
    }

    [HttpPost]
    public async Task SendElevatedAccessToken(CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(User.GetUserId().ToString());

        var resendDelay = (DateTimeOffset.Now - user!.ElevatedAccessTokenRequestedOn) - AppSettings.Identity.BearerTokenExpiration;
        // Elevated access token claim gets added to access token upon refresh token request call, so their lifetime would be the same

        if (resendDelay < TimeSpan.Zero)
            throw new TooManyRequestsExceptions(Localizer[nameof(AppStrings.WaitForElevatedAccessTokenRequestResendDelay), resendDelay.Value.Humanize(culture: CultureInfo.CurrentUICulture)]).WithExtensionData("TryAgainIn", resendDelay);

        user.ElevatedAccessTokenRequestedOn = DateTimeOffset.Now;
        var result = await userManager.UpdateAsync(user);
        if (result.Succeeded is false)
            throw new ResourceValidationException(result.Errors.Select(e => new LocalizedString(e.Code, e.Description)).ToArray());

        var currentUserSessionId = User.GetSessionId();

        var token = await userManager.GenerateUserTokenAsync(
            user,
            TokenOptions.DefaultPhoneProvider,
            FormattableString.Invariant($"ElevatedAccess:{currentUserSessionId},{user.ElevatedAccessTokenRequestedOn?.ToUniversalTime()}"));

        List<Task> sendMessagesTasks = [];

        var message = Localizer[nameof(AppStrings.ElevatedAccessTokenShortText), token].ToString();

        if (await userManager.IsEmailConfirmedAsync(user))
        {
            sendMessagesTasks.Add(emailService.SendElevatedAccessToken(user, token, cancellationToken));
        }

        if (await userManager.IsPhoneNumberConfirmedAsync(user))
        {
            var smsMessage = $"{message}{Environment.NewLine}@{HttpContext.Request.GetWebAppUrl().Host} #{token}" /* Web OTP */;
            sendMessagesTasks.Add(phoneService.SendSms(smsMessage, user.PhoneNumber!));
        }

        if (user.TwoFactorEnabled || (user.EmailConfirmed is false && user.PhoneNumberConfirmed is false /* Users signed-in through social sign-in */))
        {
            // Check out AppHub's comments for more info.
            var userSessionIdsExceptCurrentUserSessionId = await DbContext.UserSessions
                .Where(us => us.NotificationStatus == UserSessionNotificationStatus.Allowed && us.UserId == user.Id && us.Id != currentUserSessionId && us.SignalRConnectionId != null)
                .Select(us => us.SignalRConnectionId!)
                .ToArrayAsync(cancellationToken);
            sendMessagesTasks.Add(appHubContext.Clients.Clients(userSessionIdsExceptCurrentUserSessionId).SendAsync(SignalREvents.SHOW_MESSAGE, message, null, cancellationToken));

            sendMessagesTasks.Add(pushNotificationService.RequestPush(message: message, userRelatedPush: true, customSubscriptionFilter: us => us.UserSession!.UserId == user.Id && us.UserSessionId != currentUserSessionId, cancellationToken: cancellationToken));
        }

        await Task.WhenAll(sendMessagesTasks);
    }

    [HttpPost("{userSessionId}")]
    public async Task<UserSessionNotificationStatus> ToggleNotification(Guid userSessionId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        var userSession = await DbContext.UserSessions
            .FirstOrDefaultAsync(us => us.Id == userSessionId && us.UserId == userId, cancellationToken) ?? throw new ResourceNotFoundException();

        userSession.NotificationStatus = userSession.NotificationStatus is UserSessionNotificationStatus.NotConfigured ? UserSessionNotificationStatus.Allowed :
            userSession.NotificationStatus is UserSessionNotificationStatus.Allowed ? UserSessionNotificationStatus.Muted : UserSessionNotificationStatus.Allowed;

        await DbContext.SaveChangesAsync(cancellationToken);

        if (userSession.NotificationStatus is UserSessionNotificationStatus.Allowed)
        {
            await pushNotificationService.RequestPush(message: Localizer[nameof(AppStrings.TestNotificationMessage1)], userRelatedPush: true, customSubscriptionFilter: us => us.UserSessionId == userSessionId, cancellationToken: cancellationToken);
            if (userSession.SignalRConnectionId != null)
            {
                await appHubContext.Clients.Client(userSession.SignalRConnectionId).SendAsync(SignalREvents.SHOW_MESSAGE, (string)Localizer[nameof(AppStrings.TestNotificationMessage2)], null, cancellationToken);
            }
        }

        return userSession.NotificationStatus;
    }

    private static string FormatKey(string unformattedKey)
    {
        var result = new StringBuilder();
        int currentPosition = 0;
        while (currentPosition + 4 < unformattedKey.Length)
        {
            result.Append(unformattedKey.AsSpan(currentPosition, 4)).Append(' ');
            currentPosition += 4;
        }
        if (currentPosition < unformattedKey.Length)
        {
            result.Append(unformattedKey.AsSpan(currentPosition));
        }

        return result.ToString().ToLowerInvariant();
    }

    private const string AUTHENTICATOR_URI_FORMAT = "otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits=6";
    private string GenerateQrCodeUri(string user, string unformattedKey)
    {
        return string.Format(CultureInfo.InvariantCulture,
        AUTHENTICATOR_URI_FORMAT,
        urlEncoder.Encode("bit platform OnlineShopModular"),
                             urlEncoder.Encode(user),
                             unformattedKey);
    }
}
