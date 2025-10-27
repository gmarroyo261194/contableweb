using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ContableWeb.Data;

public class ContableWebDbContextFactory : IDesignTimeDbContextFactory<ContableWebDbContext>
{
    public ContableWebDbContext CreateDbContext(string[] args)
    {
        ContableWebGlobalFeatureConfigurator.Configure();
        ContableWebModuleExtensionConfigurator.Configure();
        
        var configuration = BuildConfiguration();

        var builder = new DbContextOptionsBuilder<ContableWebDbContext>()
            .UseSqlServer(configuration.GetConnectionString("Default"));

        return new ContableWebDbContext(builder.Options);
    }

    private static IConfigurationRoot BuildConfiguration()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false);

        return builder.Build();
    }
}