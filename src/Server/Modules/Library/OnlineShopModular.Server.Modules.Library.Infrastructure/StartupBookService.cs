using Microsoft.EntityFrameworkCore;
using OnlineShopModular.Server.Modules.Library.Infrastructure.Persistence;
using OnlineShopModular.Server.Modules.Library.Infrastructure.Services;
 
namespace OnlineShopModular.Server.Modules.Library.Infrastructure;

public static class StartupBookService
{
    public static IServiceCollection AddLibraryModule(this IServiceCollection services, string connectionString)
    {


        services.AddDbContext<LibraryDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<BookService>();
        return services;
    }
}
