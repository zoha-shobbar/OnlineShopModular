using Projects;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

var builder = DistributedApplication.CreateBuilder(args);

var sqlDatabase = builder.AddSqlServer("sqlserver")
        .WithLifetime(ContainerLifetime.Persistent)
        .WithVolume("/var/lib/sql-server/OnlineShopModular/data")
        .WithImage("mssql/server", "2025-latest")
        .AddDatabase("sqldb"); // Sql server 2025 supports embedded vector search.


var serverWebProject = builder.AddProject<OnlineShopModular_Server_Web>("serverweb") // Replace . with _ if needed to ensure the project builds successfully.
    .WithExternalHttpEndpoints();


serverWebProject.WithReference(sqlDatabase, "SqlServerConnectionString").WaitFor(sqlDatabase);


await builder
    .Build()
    .RunAsync();
