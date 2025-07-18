using OnlineShopModular.Server.Modules.Library.Infrastructure;
using OnlineShopModular.Server.Modules.People.Infrastructure;

namespace OnlineShopModular.Server.Api;

public static partial class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        AppEnvironment.Set(builder.Environment.EnvironmentName);

        builder.Configuration.AddSharedConfigurations();

        builder.WebHost.UseSentry(configureOptions: options => builder.Configuration.GetRequiredSection("Logging:Sentry").Bind(options));

        builder.Services.AddSharedProjectServices(builder.Configuration);
        builder.AddServerApiProjectServices();

        var configurationBuilder = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile("Modules/People/appsettings.people.json", optional: true, reloadOnChange: true)
    .AddJsonFile("Modules/Library/appsettings.library.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

        var configuration = configurationBuilder.Build();

        builder.Services.AddPeopleModule(configuration.GetConnectionString("PeopleConnection"));
        builder.Services.AddLibraryModule(configuration.GetConnectionString("LibraryConnection"));

        builder.Services.AddControllers()
    .AddApplicationPart(typeof(OnlineShopModular.Server.Modules.Library.Api.Controllers.BookController).Assembly)
    .AddApplicationPart(typeof(OnlineShopModular.Server.Modules.People.Api.Controllers.PersonController).Assembly);


        var app = builder.Build();

        if (builder.Environment.IsDevelopment())
        {
            await using var scope = app.Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await dbContext.Database.EnsureCreatedAsync(); // It's recommended to start using ef-core migrations.
        }

        app.ConfigureMiddlewares();

        await app.RunAsync();
    }
}
