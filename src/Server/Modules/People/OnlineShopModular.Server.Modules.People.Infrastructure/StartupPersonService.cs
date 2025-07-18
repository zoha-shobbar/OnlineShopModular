using Microsoft.EntityFrameworkCore;
using OnlineShopModular.Server.Modules.People.Infrastructure.Persistence;
using OnlineShopModular.Server.Modules.People.Infrastructure.Services;

namespace OnlineShopModular.Server.Modules.People.Infrastructure;

public static class StartupPersonService
{
    public static IServiceCollection AddPeopleModule(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<PersonDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<PersonService>();
        return services;
    }
}
