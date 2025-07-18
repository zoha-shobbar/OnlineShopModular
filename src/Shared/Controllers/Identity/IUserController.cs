﻿using OnlineShopModular.Shared.Dtos.Identity;

namespace OnlineShopModular.Shared.Controllers.Identity;

[Route("api/[controller]/[action]/"), AuthorizedApi]
public interface IUserController : IAppController
{
    [HttpGet]
    Task<UserDto> GetCurrentUser(CancellationToken cancellationToken);

    [HttpGet]
    Task<List<UserSessionDto>> GetUserSessions(CancellationToken cancellationToken) => default!;

    [HttpPost, NoRetryPolicy]
    Task SignOut(CancellationToken cancellationToken);

    [HttpPost("{id}")]
    Task RevokeSession(Guid id, CancellationToken cancellationToken);

    [HttpPost]
    Task UpdateSession(UpdateUserSessionRequestDto request, CancellationToken cancellationToken);

    [HttpPut]
    Task<UserDto> Update(EditUserRequestDto userDto, CancellationToken cancellationToken);

    [HttpPost]
    Task ChangePassword(ChangePasswordRequestDto request, CancellationToken cancellationToken);

    [HttpPost]
    Task ChangeUserName(ChangeUserNameRequestDto request, CancellationToken cancellationToken);

    [HttpPost]
    Task SendChangeEmailToken(SendEmailTokenRequestDto request, CancellationToken cancellationToken);

    [HttpPost]
    Task ChangeEmail(ChangeEmailRequestDto request, CancellationToken cancellationToken);

    [HttpPost]
    Task SendChangePhoneNumberToken(SendPhoneTokenRequestDto request, CancellationToken cancellationToken);

    [HttpPost]
    Task ChangePhoneNumber(ChangePhoneNumberRequestDto request, CancellationToken cancellationToken);

    [HttpDelete]
    Task Delete(CancellationToken cancellationToken);

    [HttpPost]
    [Route("~/api/[controller]/2fa")]
    Task<TwoFactorAuthResponseDto> TwoFactorAuth(TwoFactorAuthRequestDto request, CancellationToken cancellationToken) => default!;

    [HttpPost]
    Task SendElevatedAccessToken(CancellationToken cancellationToken);

    [HttpGet]
    Task<JsonElement> GetWebAuthnCredentialOptions(CancellationToken cancellationToken) => default!;

    [HttpPut]
    Task CreateWebAuthnCredential(JsonElement attestationResponse, CancellationToken cancellationToken) => default!;

    [HttpDelete]
    Task DeleteWebAuthnCredential(JsonElement clientResponse, CancellationToken cancellationToken) => default!;

    [HttpDelete]
    Task DeleteAllWebAuthnCredentials(CancellationToken cancellationToken);

    [HttpPost("{userSessionId}")]
    Task<UserSessionNotificationStatus> ToggleNotification(Guid userSessionId, CancellationToken cancellationToken);
}
