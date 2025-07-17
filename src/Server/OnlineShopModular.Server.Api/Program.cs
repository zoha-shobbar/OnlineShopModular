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

        builder.Services.AddControllers()
            .AddApplicationPart(typeof(OnlineShopModular.Server.Modules.People.Api.PeopleController).Assembly)
            .AddApplicationPart(typeof(OnlineShopModular.Server.Modules.Library.Api.LibraryController).Assembly);

        OnlineShopModular.Server.Modules.People.Infrastructure.Startup.AddPeopleModule(builder.Services);
        OnlineShopModular.Server.Modules.People.Application.Startup.AddPeopleApplication(builder.Services);
        OnlineShopModular.Server.Modules.Library.Infrastructure.Startup.AddLibraryModule(builder.Services);
        OnlineShopModular.Server.Modules.Library.Application.Startup.AddLibraryApplication(builder.Services);

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
