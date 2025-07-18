﻿using OnlineShopModular.Server.Api.Models.Identity;

namespace OnlineShopModular.Server.Api.Models.PushNotification;

public class PushNotificationSubscription
{
    public int Id { get; set; }

    [Required]
    public string? DeviceId { get; set; }

    [Required, AllowedValues("apns", "fcmV1", "browser")]
    public string? Platform { get; set; }

    [Required]
    public string? PushChannel { get; set; }

    public string? P256dh { get; set; }
    public string? Auth { get; set; }
    public string? Endpoint { get; set; }

    public Guid? UserSessionId { get; set; }

    [ForeignKey(nameof(UserSessionId))]
    public UserSession? UserSession { get; set; }

    public string[] Tags { get; set; } = [];

    /// <summary>
    /// Unix Time Seconds
    /// </summary>
    public long ExpirationTime { get; set; }

    /// <summary>
    /// Unix Time Seconds
    /// </summary>
    public long RenewedOn { get; set; }
}
