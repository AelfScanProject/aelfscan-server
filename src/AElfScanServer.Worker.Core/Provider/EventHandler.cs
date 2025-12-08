using System;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core.Extension;
using AElf.Types;
using AElfScanServer.Worker.Core.Service;
using Volo.Abp.DependencyInjection;

namespace AElfScanServer.Worker.Core.Provider;

public interface IEventHandler
{
    string EventName { get; }
    Task<double> HandlerAsync(string chainId, LogEvent logEvent);
}

public abstract class EventHandler : IEventHandler
{
    public abstract string EventName { get; }
    protected readonly IAddressService AddressService;

    protected EventHandler(IAddressService addressService)
    {
        AddressService = addressService;
    }

    public abstract Task<double> HandlerAsync(string chainId, LogEvent logEvent);
}

// public class TransferredEventHandler : EventHandler, ISingletonDependency
// {
//     public override string EventName => nameof(Transferred);
//     
//     public override async Task<double> HandlerAsync(string chainId, LogEvent logEvent)
//     {
//         var transferred = new Transferred();
//         transferred.MergeFrom(logEvent);
//         if (transferred.From != null)
//         {
//             await AddressService.PatchAddressInfoAsync(chainId, transferred.From.ToBase58(), addressIndices);
//         }
//
//         if (transferred.To != null)
//         {
//             await AddressService.PatchAddressInfoAsync(chainId, transferred.To.ToBase58(), addressIndices);
//         }
//
//         return transferred.Symbol == "ELF" ? Convert.ToDouble(transferred.Amount) : 0;
//     }
//
//     public TransferredEventHandler(IAddressService addressService) : base(addressService)
//     {
//     }
// }