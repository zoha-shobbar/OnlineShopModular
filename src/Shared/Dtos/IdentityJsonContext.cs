﻿using OnlineShopModular.Shared.Dtos.Identity;

namespace OnlineShopModular.Shared.Dtos;

/// <summary>
/// https://devblogs.microsoft.com/dotnet/try-the-new-system-text-json-source-generator/
/// </summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(UserDto))]
[JsonSerializable(typeof(IdentityRequestDto))]
[JsonSerializable(typeof(SignInRequestDto))]
[JsonSerializable(typeof(SignInResponseDto))]
[JsonSerializable(typeof(TokenResponseDto))]
[JsonSerializable(typeof(RefreshTokenRequestDto))]
[JsonSerializable(typeof(SignUpRequestDto))]
[JsonSerializable(typeof(EditUserRequestDto))]
[JsonSerializable(typeof(SendEmailTokenRequestDto))]
[JsonSerializable(typeof(SendPhoneTokenRequestDto))]
[JsonSerializable(typeof(ConfirmEmailRequestDto))]
[JsonSerializable(typeof(ChangeEmailRequestDto))]
[JsonSerializable(typeof(ConfirmPhoneRequestDto))]
[JsonSerializable(typeof(ChangePhoneNumberRequestDto))]
[JsonSerializable(typeof(SendResetPasswordTokenRequestDto))]
[JsonSerializable(typeof(ResetPasswordRequestDto))]
[JsonSerializable(typeof(TwoFactorAuthRequestDto))]
[JsonSerializable(typeof(TwoFactorAuthResponseDto))]
[JsonSerializable(typeof(List<UserSessionDto>))]
[JsonSerializable(typeof(RoleDto))]
[JsonSerializable(typeof(List<RoleDto>))]
[JsonSerializable(typeof(List<UserDto>))]
[JsonSerializable(typeof(List<ClaimDto>))]
[JsonSerializable(typeof(UserRoleDto))]
[JsonSerializable(typeof(UpdateUserSessionRequestDto))]
public partial class IdentityJsonContext : JsonSerializerContext
{
}
