﻿using Hangfire.Dashboard;
using Hangfire.Annotations;

namespace OnlineShopModular.Server.Api.Filters;

public class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize([NotNull] DashboardContext context)
    {
        return context.GetHttpContext().User.HasClaim(AppClaimTypes.FEATURES, AppFeatures.System.ManageJobs);
    }
}
