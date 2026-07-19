using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ResilienceOps.Api.Data;

namespace ResilienceOps.Api.IntegrationTests;

public sealed class ResilienceOpsApiFactory
    : WebApplicationFactory<Program>
{
    private readonly SqliteConnection databaseConnection;

    public ResilienceOpsApiFactory()
    {
        databaseConnection =
            new SqliteConnection(
                "Data Source=:memory:");

        databaseConnection.Open();
    }

    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        builder.UseEnvironment(
            "IntegrationTesting");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<
                DbContextOptions<
                    ResilienceOpsDbContext>>();

            services.RemoveAll<
                ResilienceOpsDbContext>();

            services.AddDbContext<
                ResilienceOpsDbContext>(
                options =>
                {
                    options.UseSqlite(
                        databaseConnection);
                });
        });
    }

    protected override void Dispose(
        bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            databaseConnection.Dispose();
        }
    }
}