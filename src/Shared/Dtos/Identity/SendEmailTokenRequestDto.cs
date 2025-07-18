namespace OnlineShopModular.Shared.Dtos.Identity;

[DtoResourceType(typeof(AppStrings))]
public partial class SendEmailTokenRequestDto
{
    [Required(ErrorMessage = nameof(AppStrings.RequiredAttribute_ValidationError))]
    [EmailAddress(ErrorMessage = nameof(AppStrings.EmailAddressAttribute_ValidationError))]
    [Display(Name = nameof(AppStrings.Email))]
    public string? Email { get; set; }

    public string? ReturnUrl { get; set; }
}
