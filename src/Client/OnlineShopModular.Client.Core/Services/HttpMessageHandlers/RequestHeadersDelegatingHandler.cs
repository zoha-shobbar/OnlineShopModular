﻿using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace OnlineShopModular.Client.Core.Services.HttpMessageHandlers;

public partial class RequestHeadersDelegatingHandler(ITelemetryContext telemetryContext, HttpMessageHandler handler)
    : DelegatingHandler(handler)
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.SetBrowserRequestCredentials(BrowserRequestCredentials.Omit);
        request.SetBrowserResponseStreamingEnabled(true);

        request.Version = HttpVersion.Version20;
        request.VersionPolicy = HttpVersionPolicy.RequestVersionOrLower;

        if (request.Headers.UserAgent.Any() is false)
        {
            request.Headers.UserAgent.TryParseAdd(telemetryContext.Platform);
        }

        if (CultureInfoManager.InvariantGlobalization is false && string.IsNullOrEmpty(CultureInfo.CurrentUICulture.Name) is false)
        {
            request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue(CultureInfo.CurrentUICulture.Name));
        }

        request.Headers.Add("X-App-Version", telemetryContext.AppVersion);
        request.Headers.Add("X-App-Platform", AppPlatform.Type.ToString());

        return await base.SendAsync(request, cancellationToken);
    }
}
