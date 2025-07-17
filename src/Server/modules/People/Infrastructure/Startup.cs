
namespace OnlineShopModular.Server.Modules.People.Infrastructure
{
    public static class Startup
    {
        public static IServiceCollection AddPeopleModule(this IServiceCollection services)
        {
            services.AddDbContext<PeopleDbContext>(options =>
                options.UseInMemoryDatabase("PeopleDatabase"));

            services.AddScoped<IPersonRepository, PersonRepository>();

            return services;
        }
    }
}
