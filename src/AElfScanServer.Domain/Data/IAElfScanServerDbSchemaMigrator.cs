using System.Threading.Tasks;

namespace AElfScanServer.Domain.Common.Data;

public interface IAElfScanServerDbSchemaMigrator
{
    Task MigrateAsync();
}
