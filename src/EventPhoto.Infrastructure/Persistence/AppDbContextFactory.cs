using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EventPhoto.Infrastructure.Persistence;

/// <summary>
/// Design-time factory for AppDbContext.
/// Used by EF Core CLI tools (dotnet ef migrations add / database update) at design time.
/// </summary>
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    /// <inheritdoc/>
    public AppDbContext CreateDbContext(string[] args)
    {
        // Design-time connection string — used only by EF CLI tools (migrations).
        // Change before running 'dotnet ef database update' if your credentials differ.
        const string designTimeConnection =
            "Host=localhost;Database=pixbridge_dev;Username=postgres;Password=Gnanavel@2026;";

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql(
            designTimeConnection,
            npgsql => npgsql.MigrationsAssembly(typeof(AppDbContextFactory).Assembly.GetName().Name));

        return new AppDbContext(optionsBuilder.Options);
    }
}
