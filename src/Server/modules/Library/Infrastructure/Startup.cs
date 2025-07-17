
namespace OnlineShopModular.Server.Modules.Library.Infrastructure
{
    public static class Startup
    {
        public static IServiceCollection AddLibraryModule(this IServiceCollection services)
        {
            services.AddDbContext<LibraryDbContext>(options =>
                options.UseInMemoryDatabase("LibraryDatabase"));

            services.AddScoped<IBookRepository, BookRepository>();
            services.AddScoped<ILendingBookRepository, LendingBookRepository>();

            return services;
        }
    }
}
