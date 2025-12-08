using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace AElfScanServer.Domain.Common.Data;

/* This is used if database provider does't define
 * IAElfScanServerDbSchemaMigrator implementation.
 */
public class NullAElfScanServerDbSchemaMigrator : IAElfScanServerDbSchemaMigrator, ITransientDependency
{
    public Task MigrateAsync()
    {
        return Task.CompletedTask;
    }
}
