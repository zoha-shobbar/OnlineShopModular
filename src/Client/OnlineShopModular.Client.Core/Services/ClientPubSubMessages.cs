﻿using OnlineShopModular.Client.Core.Components;

namespace OnlineShopModular.Client.Core.Services;

public partial class ClientPubSubMessages
    : SharedPubSubMessages
{
    public const string SHOW_SNACK = nameof(SHOW_SNACK);
    public const string SHOW_MODAL = nameof(SHOW_MODAL);
    public const string CLOSE_MODAL = nameof(CLOSE_MODAL);

    public const string THEME_CHANGED = nameof(THEME_CHANGED);
    public const string OPEN_NAV_PANEL = nameof(OPEN_NAV_PANEL);
    public const string CLOSE_NAV_PANEL = nameof(CLOSE_NAV_PANEL);
    public const string CULTURE_CHANGED = nameof(CULTURE_CHANGED);
    /// <summary>
    /// <inheritdoc cref="Parameters.IsOnline"/>
    /// </summary>
    public const string IS_ONLINE_CHANGED = nameof(IS_ONLINE_CHANGED);
    public const string PAGE_DATA_CHANGED = nameof(PAGE_DATA_CHANGED);
    public const string ROUTE_DATA_UPDATED = nameof(ROUTE_DATA_UPDATED);

    /// <summary>
    /// Supposed to be called using JavaScript to navigate between pages without reloading the app.
    /// </summary>
    public const string NAVIGATE_TO = nameof(NAVIGATE_TO);
    public const string SHOW_DIAGNOSTIC_MODAL = nameof(SHOW_DIAGNOSTIC_MODAL);




    public const string SOCIAL_SIGN_IN = nameof(SOCIAL_SIGN_IN);

    public const string FORCE_UPDATE = nameof(FORCE_UPDATE);
}
