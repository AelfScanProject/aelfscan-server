using Xunit.Abstractions;

namespace AElfScanServer;

public abstract class AElfScanServerDomainTestBase : AElfScanServer.AElfScanServerTestBase<AElfScanServerDomainTestModule>
{
    protected AElfScanServerDomainTestBase(ITestOutputHelper output) : base(output)
    {
    }
}