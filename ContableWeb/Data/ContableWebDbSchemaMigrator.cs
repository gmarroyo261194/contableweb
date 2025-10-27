using Volo.Abp.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace ContableWeb.Data;

public class ContableWebDbSchemaMigrator : ITransientDependency
{
    private readonly IServiceProvider _serviceProvider;

    public ContableWebDbSchemaMigrator(
        IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task MigrateAsync()
    {
        
        /* We intentionally resolving the ContableWebDbContext
         * from IServiceProvider (instead of directly injecting it)
         * to properly get the connection string of the current tenant in the
         * current scope.
         */

        await _serviceProvider
            .GetRequiredService<ContableWebDbContext>()
            .Database
            .MigrateAsync();

    }
}
