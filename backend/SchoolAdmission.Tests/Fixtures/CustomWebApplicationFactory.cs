using System.Data.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SchoolAdmission.Domain.Common;
using SchoolAdmission.Infrastructure.Data;

namespace SchoolAdmission.Tests.Fixtures;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private DbConnection? _connection;
    public TestSystemClock Clock { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove real DbContext registration
            var dbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (dbContextDescriptor != null)
            {
                services.Remove(dbContextDescriptor);
            }

            // Remove real SystemClock
            var clockDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(ISystemClock));
            if (clockDescriptor != null)
            {
                services.Remove(clockDescriptor);
            }

            // Register TestSystemClock
            services.AddSingleton<ISystemClock>(Clock);

            // Keep one master connection open to maintain the shared in-memory database lifetime
            _connection = new SqliteConnection("Data Source=AdmissionTestDb;Mode=Memory;Cache=Shared");
            _connection.Open();

            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseSqlite("Data Source=AdmissionTestDb;Mode=Memory;Cache=Shared");
            });

            // Ensure schema is created in SQLite
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Database.EnsureCreated();
        });

        builder.UseEnvironment("Testing");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _connection?.Dispose();
    }
}
