﻿namespace OnlineShopModular.Shared.Services;

public class AuthPolicies
{
    /// <summary>
    /// Having this policy means the user has enabled 2 factor authentication.
    /// </summary>
    public const string TFA_ENABLED = nameof(TFA_ENABLED);

    /// <summary>
    /// By default, each user is limited to 3 active sessions.
    /// This policy can be disabled or configured to adjust the session limit dynamically, 
    /// such as by reading from application settings, the user's subscription plan, or other criteria.
    /// Currently, this policy applies only to the Todo and AdminPanel specific pages like dashboard page. 
    /// However, it can be extended to cover additional pages as needed. 
    /// 
    /// Important: Do not apply this policy to the settings page, as users need access to manage and revoke their sessions there.
    /// </summary>
    public const string PRIVILEGED_ACCESS = nameof(PRIVILEGED_ACCESS);

    /// <summary>
    /// Enables the user to execute potentially harmful operations, like account removal. 
    /// This limited-time policy is activated upon successful verification via a secure 6-digit code or
    /// during the initial minutes of a sign-in session of users with 2fa enabled.
    /// </summary>
    public const string ELEVATED_ACCESS = nameof(ELEVATED_ACCESS);
}
