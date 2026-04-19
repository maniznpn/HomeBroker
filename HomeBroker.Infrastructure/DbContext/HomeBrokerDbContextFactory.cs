using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace HomeBroker.Infrastructure.DbContext
{
    // Design-time factory for EF Core tools (migrations, update-database).
    // This ensures the tools can create HomeBrokerDbContext even when the
    // application's DI container is not available.
    public class HomeBrokerDbContextFactory : IDesignTimeDbContextFactory<HomeBrokerDbContext>
    {
        public HomeBrokerDbContext CreateDbContext(string[] args)
        {
            // Try multiple locations for appsettings.json so the factory works
            // whether called from the Infrastructure project or from the WebApi project.
            var baseDir = Directory.GetCurrentDirectory();
            var candidatePaths = new[]
            {
                baseDir,
                Path.Combine(baseDir, "..", "HomeBroker.WebApi"),
                Path.Combine(baseDir, "..", "..", "HomeBroker.WebApi"),
            };

            IConfigurationRoot configuration = null;
            foreach (var p in candidatePaths)
            {
                try
                {
                    var full = Path.GetFullPath(p);
                    var configPath = Path.Combine(full, "appsettings.json");
                    if (File.Exists(configPath))
                    {
                        // AddJsonFile accepts a path; use the full path to avoid needing SetBasePath extension.
                        configuration = new ConfigurationBuilder()
                            .AddJsonFile(configPath, optional: false, reloadOnChange: false)
                            .AddEnvironmentVariables()
                            .Build();

                        break;
                    }
                }
                catch
                {
                    // ignore and try next
                }
            }

            // Fallback to environment or a reasonable default if configuration not found
            var connectionString = configuration?.GetConnectionString("DefaultConnection")
                                   ?? Environment.GetEnvironmentVariable("DefaultConnection")
                                   ?? "Server=(localdb)\\mssqllocaldb;Database=HomeBroker;Trusted_Connection=true;";

            var optionsBuilder = new DbContextOptionsBuilder<HomeBrokerDbContext>();
            optionsBuilder.UseSqlServer(connectionString, b => b.MigrationsAssembly(typeof(HomeBrokerDbContext).Assembly.FullName));

            return new HomeBrokerDbContext(optionsBuilder.Options);
        }
    }
}
