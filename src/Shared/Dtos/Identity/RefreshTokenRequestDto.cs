﻿namespace OnlineShopModular.Shared.Dtos.Identity;

[DtoResourceType(typeof(AppStrings))]
public partial class RefreshTokenRequestDto
{
    [Required(ErrorMessage = nameof(AppStrings.RequiredAttribute_ValidationError))]
    public string? RefreshToken { get; set; }

    /// <summary>
    /// <inheritdoc cref="AuthPolicies.ELEVATED_ACCESS" />
    /// </summary>
    public string? ElevatedAccessToken { get; set; }
}
