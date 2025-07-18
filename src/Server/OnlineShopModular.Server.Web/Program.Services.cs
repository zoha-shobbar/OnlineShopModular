﻿using Microsoft.Net.Http.Headers;
using OnlineShopModular.Server.Api;
using OnlineShopModular.Client.Web;
using OnlineShopModular.Server.Web.Services;
using Microsoft.AspNetCore.Antiforgery;
using OnlineShopModular.Client.Core.Services.Contracts;

namespace OnlineShopModular.Server.Web;

public static partial class Program
{
    public static void AddServerWebProjectServices(this WebApplicationBuilder builder)
    {
        // Services being registered here can get injected in server project only.
        var services = builder.Services;
        var configuration = builder.Configuration;

        if (AppEnvironment.IsDev())
        {
            builder.Logging.AddDiagnosticLogger();
        }

        services.AddClientWebProjectServices(configuration);

        builder.AddServerApiProjectServices();

        services.AddSingleton(sp =>
        {
            ServerWebSettings settings = new();
            configuration.Bind(settings);
            return settings;
        });

        services.AddOptions<ServerWebSettings>()
            .Bind(configuration)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        AddBlazor(builder);
    }

    private static void AddBlazor(WebApplicationBuilder builder)
    {
        var services = builder.Services;
        var configuration = builder.Configuration;

        services.AddTransient<IAntiforgery, NoOpAntiforgery>();
        services.AddTransient<IPrerenderStateService, WebServerPrerenderStateService>();
        services.AddScoped<IExceptionHandler, WebServerExceptionHandler>();
        services.AddScoped<IAuthTokenProvider, ServerSideAuthTokenProvider>();
        services.AddScoped(sp =>
        {
            // This HTTP client is utilized during pre-rendering and within Blazor Auto/Server sessions for API calls. 
            // Key headers such as Authorization and AcceptLanguage headers are added in Client/Core/Services/HttpMessageHandlers. 
            // Additionally, forwarded headers are handled to ensure proper forwarding, if the backend is hosted behind a CDN. 
            // User agent and referrer headers are also included to provide the API with necessary request context. 

            var serverSettings = sp.GetRequiredService<ServerWebSettings>();
            var serverAddressString = string.IsNullOrEmpty(serverSettings.ServerSideHttpClientBaseAddress) is false ?
                serverSettings.ServerSideHttpClientBaseAddress : configuration.GetServerAddress();

            Uri.TryCreate(serverAddressString, UriKind.RelativeOrAbsolute, out var serverAddress);
            var currentRequest = sp.GetRequiredService<IHttpContextAccessor>().HttpContext!.Request;
            if (serverAddress!.IsAbsoluteUri is false)
            {
                serverAddress = new Uri(currentRequest.GetBaseUrl(), serverAddress);
            }

            var httpClient = new HttpClient(sp.GetRequiredService<HttpMessageHandler>())
            {
                BaseAddress = serverAddress
            };

            var forwardedHeadersOptions = sp.GetRequiredService<ServerWebSettings>().ForwardedHeaders;

            foreach (var xHeader in currentRequest.Headers.Where(h => h.Key.StartsWith("X-", StringComparison.InvariantCultureIgnoreCase)))
            {
                httpClient.DefaultRequestHeaders.Add(xHeader.Key, string.Join(',', xHeader.Value.AsEnumerable()));
            }

            if (forwardedHeadersOptions is not null && httpClient.DefaultRequestHeaders.Contains(forwardedHeadersOptions.ForwardedForHeaderName) is false &&
                currentRequest.HttpContext.Connection.RemoteIpAddress is not null)
            {
                httpClient.DefaultRequestHeaders.Add(forwardedHeadersOptions.ForwardedForHeaderName,
                                                     currentRequest.HttpContext.Connection.RemoteIpAddress.ToString());
            }

            if (currentRequest.Headers.TryGetValue(HeaderNames.UserAgent, out var headerValues))
            {
                foreach (var ua in currentRequest.Headers.UserAgent)
                {
                    httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd(ua);
                }
            }

            if (currentRequest.Headers.TryGetValue(HeaderNames.Referer, out headerValues))
            {
                httpClient.DefaultRequestHeaders.Add(HeaderNames.Referer, string.Join(',', headerValues.AsEnumerable()));
            }

            httpClient.DefaultRequestHeaders.Add("X-Origin", currentRequest.GetBaseUrl().ToString());

            return httpClient;
        });
        services.AddKeyedScoped<HttpMessageHandler, SocketsHttpHandler>("PrimaryHttpMessageHandler", (sp, key) => new()
        {
            EnableMultipleHttp2Connections = true,
            EnableMultipleHttp3Connections = true
        });

        services.AddRazorComponents()
            .AddInteractiveServerComponents()
            .AddInteractiveWebAssemblyComponents();

        services.AddMvc();
    }
}
