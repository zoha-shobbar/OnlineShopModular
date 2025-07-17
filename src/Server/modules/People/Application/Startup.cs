using Microsoft.Extensions.DependencyInjection;

namespace OnlineShopModular.Server.Modules.People.Application
{
    public static class Startup
    {
        public static IServiceCollection AddPeopleApplication(this IServiceCollection services)
        {
            services.AddScoped<PersonService>();
            return services;
        }
    }
}
