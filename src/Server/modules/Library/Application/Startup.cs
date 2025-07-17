using Microsoft.Extensions.DependencyInjection;

namespace OnlineShopModular.Server.Modules.Library.Application
{
    public static class Startup
    {
        public static IServiceCollection AddLibraryApplication(this IServiceCollection services)
        {
            services.AddScoped<LibraryService>();
            return services;
        }
    }
}
