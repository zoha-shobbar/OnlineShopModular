﻿namespace OnlineShopModular.Server.Api.Models.Identity;

public class UserClaim : IdentityUserClaim<Guid>
{
    public User? User { get; set; }
}
