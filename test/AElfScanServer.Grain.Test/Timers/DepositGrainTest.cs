using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace ETransferServer.Grain.Test.Timers;

public class DepositGrainTest : AElfScanServerTestBase
{
    public DepositGrainTest(ITestOutputHelper output) : base(output)
    {
    }
    
    
    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
      //  services.AddSingleton(MockHttpFactory());
    }


    [Fact]
    public void DepositTimer()
    {
        
        // MockHttpByPath(HttpMethod.Get, "/v1/custody/transactions_by_time_ex", new );
        
        /*var coBoTimerGrain = Cluster.Client.GetGrain<ICoBoDepositQueryTimerGrain>(GuidHelper.UniqGuid(nameof(ICoBoDepositQueryTimerGrain)));
        coBoTimerGrain.GetLastCallbackTime();*/
        
        

    }
    
    
    
    
    
}