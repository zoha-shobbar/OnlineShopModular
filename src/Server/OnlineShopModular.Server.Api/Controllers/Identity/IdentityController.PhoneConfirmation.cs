﻿using Humanizer;
using OnlineShopModular.Server.Api.Services;
using OnlineShopModular.Shared.Dtos.Identity;
using OnlineShopModular.Server.Api.Models.Identity;

namespace OnlineShopModular.Server.Api.Controllers.Identity;

public partial class IdentityController
{
    [AutoInject] private PhoneService phoneService = default!;

    [HttpPost]
    public async Task SendConfirmPhoneToken(SendPhoneTokenRequestDto request, CancellationToken cancellationToken)
    {
        request.PhoneNumber = phoneService.NormalizePhoneNumber(request.PhoneNumber);
        var user = await userManager.FindByPhoneNumber(request.PhoneNumber!)
            ?? throw new BadRequestException(Localizer[nameof(AppStrings.UserNotFound)]).WithData("PhoneNumber", request.PhoneNumber);

        if (await userManager.IsPhoneNumberConfirmedAsync(user))
            throw new BadRequestException(Localizer[nameof(AppStrings.PhoneNumberAlreadyConfirmed)]).WithData("UserId", user.Id);

        await SendConfirmPhoneToken(user, cancellationToken);
    }

    [HttpPost, Produces<TokenResponseDto>()]
    public async Task ConfirmPhone(ConfirmPhoneRequestDto request, CancellationToken cancellationToken)
    {
        request.PhoneNumber = phoneService.NormalizePhoneNumber(request.PhoneNumber);
        var user = await userManager.FindByPhoneNumber(request.PhoneNumber!)
            ?? throw new BadRequestException(Localizer[nameof(AppStrings.UserNotFound)]).WithData("PhoneNumber", request.PhoneNumber);

        var expired = (DateTimeOffset.Now - user.PhoneNumberTokenRequestedOn) > AppSettings.Identity.PhoneNumberTokenLifetime;

        if (expired)
            throw new BadRequestException(nameof(AppStrings.ExpiredToken)).WithData("UserId", user.Id);

        if (await userManager.IsLockedOutAsync(user))
        {
            var tryAgainIn = (user.LockoutEnd! - DateTimeOffset.UtcNow).Value;
            throw new BadRequestException(Localizer[nameof(AppStrings.UserLockedOut), (DateTimeOffset.UtcNow - user.LockoutEnd!).Value.Humanize(culture: CultureInfo.CurrentUICulture)]).WithData("UserId", user.Id).WithExtensionData("TryAgainIn", tryAgainIn);
        }

        var tokenIsValid = await userManager.VerifyUserTokenAsync(user, TokenOptions.DefaultPhoneProvider, FormattableString.Invariant($"VerifyPhoneNumber:{request.PhoneNumber},{user.PhoneNumberTokenRequestedOn?.ToUniversalTime()}"), request.Token!);

        if (tokenIsValid is false)
        {
            await userManager.AccessFailedAsync(user);
            throw new BadRequestException(nameof(AppStrings.InvalidToken)).WithData("UserId", user.Id);
        }
        await ((IUserPhoneNumberStore<User>)userStore).SetPhoneNumberConfirmedAsync(user, true, cancellationToken);
        var result = await userManager.UpdateAsync(user);
        if (result.Succeeded is false)
            throw new ResourceValidationException(result.Errors.Select(e => new LocalizedString(e.Code, e.Description)).ToArray()).WithData("UserId", user.Id);

        await ((IUserLockoutStore<User>)userStore).ResetAccessFailedCountAsync(user, cancellationToken);
        user.PhoneNumberTokenRequestedOn = null; // invalidates phone token
        var updateResult = await userManager.UpdateAsync(user);
        if (updateResult.Succeeded is false)
            throw new ResourceValidationException(updateResult.Errors.Select(e => new LocalizedString(e.Code, e.Description)).ToArray()).WithData("UserId", user.Id);

        var token = await userManager.GenerateUserTokenAsync(user, TokenOptions.DefaultPhoneProvider, FormattableString.Invariant($"Otp_Sms,{user.OtpRequestedOn?.ToUniversalTime()}"));

        await SignIn(new() { PhoneNumber = request.PhoneNumber, Otp = token }, cancellationToken);
    }


    private async Task SendConfirmPhoneToken(User user, CancellationToken cancellationToken)
    {
        var resendDelay = (DateTimeOffset.Now - user.PhoneNumberTokenRequestedOn) - AppSettings.Identity.PhoneNumberTokenLifetime;

        if (resendDelay < TimeSpan.Zero)
            throw new TooManyRequestsExceptions(Localizer[nameof(AppStrings.WaitForPhoneNumberTokenRequestResendDelay), resendDelay.Value.Humanize(culture: CultureInfo.CurrentUICulture)]).WithData("UserId", user.Id).WithExtensionData("TryAgainIn", resendDelay);

        user.PhoneNumberTokenRequestedOn = DateTimeOffset.Now;
        var result = await userManager.UpdateAsync(user);

        if (result.Succeeded is false)
            throw new ResourceValidationException(result.Errors.Select(e => new LocalizedString(e.Code, e.Description)).ToArray()).WithData("UserId", user.Id);

        var phoneNumber = user.PhoneNumber!;
        var token = await userManager.GenerateUserTokenAsync(user, TokenOptions.DefaultPhoneProvider, FormattableString.Invariant($"VerifyPhoneNumber:{phoneNumber},{user.PhoneNumberTokenRequestedOn?.ToUniversalTime()}"));

        var message = Localizer[nameof(AppStrings.ConfirmPhoneTokenShortText), token];
        var smsMessage = $"{message}{Environment.NewLine}@{HttpContext.Request.GetWebAppUrl().Host} #{token}" /* Web OTP */;
        await phoneService.SendSms(smsMessage, phoneNumber);
    }
}
