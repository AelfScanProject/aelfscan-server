using Orleans.TestingHost;
using Volo.Abp.Modularity;
using Xunit.Abstractions;

namespace AElfScanServer;

public abstract class AElfScanServerOrleansTestBase<TStartupModule> : AElfScanServer.AElfScanServerTestBase<TStartupModule> where TStartupModule : IAbpModule
{

    protected readonly TestCluster Cluster;
    
    public AElfScanServerOrleansTestBase(ITestOutputHelper output) : base(output)
    {
        Cluster = GetRequiredService<ClusterFixture>().Cluster;
    }
}