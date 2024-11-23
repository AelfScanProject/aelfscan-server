using Xunit.Abstractions;

namespace AElfScanServer;

public abstract partial class AElfScanServerApplicationTestBase : AElfScanServerOrleansTestBase<AElfScanServerApplicationTestModule>
{

    public  readonly ITestOutputHelper Output;
    protected AElfScanServerApplicationTestBase(ITestOutputHelper output) : base(output)
    {
        Output = output;
    }
}