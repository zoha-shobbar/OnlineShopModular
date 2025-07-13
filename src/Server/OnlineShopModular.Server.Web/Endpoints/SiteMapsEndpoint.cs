﻿using System.Reflection;
using System.Runtime.Loader;
using Microsoft.AspNetCore.Authorization;
using System.Text.RegularExpressions;
using OnlineShopModular.Shared;
using OnlineShopModular.Shared.Attributes;
using OnlineShopModular.Client.Core.Services;

public static partial class SiteMapEndpoint
{
    public static WebApplication UseSiteMap(this WebApplication app)
    {
        const string siteMapHeader = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<urlset xmlns=""http://www.sitemaps.org/schemas/sitemap/0.9"">";

        app.MapGet("/sitemap_index.xml", [AppResponseCache(SharedMaxAge = 3600 * 24 * 7)] async (context) =>
        {
            const string SITEMAP_INDEX_FORMAT = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<sitemapindex xmlns=""http://www.sitemaps.org/schemas/sitemap/0.9"">
   <sitemap>
      <loc>{0}sitemap.xml</loc>
   </sitemap>
</sitemapindex>";

            var baseUrl = context.Request.GetBaseUrl();

            context.Response.Headers.ContentType = "application/xml";

            await context.Response.WriteAsync(string.Format(SITEMAP_INDEX_FORMAT, baseUrl), context.RequestAborted);
        }).CacheOutput("AppResponseCachePolicy").WithTags("Sitemaps");

        app.MapGet("/sitemap.xml", [AppResponseCache(SharedMaxAge = 3600 * 24 * 7)] async (context) =>
        {
            var urls = AssemblyLoadContext.Default.Assemblies.Where(asm => asm.GetName().Name?.Contains("OnlineShopModular.Client") is true)
                 .SelectMany(asm => asm.ExportedTypes)
                 .Where(att => att.GetCustomAttributes<AuthorizeAttribute>(inherit: true).Any() is false)
                 .SelectMany(t => t.GetCustomAttributes<Microsoft.AspNetCore.Components.RouteAttribute>())
                 .Where(att => RouteRegex().IsMatch(att.Template) is false)
                 .Select(att => att.Template)
                 .Except([Urls.NotFoundPage, Urls.NotAuthorizedPage])
                 .ToArray();

            urls = CultureInfoManager.InvariantGlobalization is false
                    ? urls.Union(CultureInfoManager.SupportedCultures.SelectMany(sc => urls.Select(url => $"{sc.Culture.Name}{url}"))).ToArray()
                    : urls;

            var baseUrl = context.Request.GetBaseUrl();

            var siteMap = @$"{siteMapHeader}
    {string.Join(Environment.NewLine, urls.Select(u => $"<url><loc>{new Uri(baseUrl, u)}</loc></url>"))}
</urlset>";

            context.Response.Headers.ContentType = "application/xml";

            await context.Response.WriteAsync(siteMap, context.RequestAborted);
        }).CacheOutput("AppResponseCachePolicy").WithTags("Sitemaps");


        return app;
    }

    [GeneratedRegex(@"\{.*?\}")]
    private static partial Regex RouteRegex();
}
