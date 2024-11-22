using Microsoft.Extensions.Options;
using Moq;
using Orleans.TestingHost;
using Xunit.Abstractions;

namespace ETransferServer.Grain.Test;

public class AElfScanServerTestBase : AElfScanServer.AElfScanServerTestBase<AElfScanServerGrainTestModule>
{
    protected readonly TestCluster Cluster;
    public AElfScanServerTestBase(ITestOutputHelper output) : base(output)
    {
        Cluster = GetRequiredService<ClusterFixture>().Cluster;
    }
   

}