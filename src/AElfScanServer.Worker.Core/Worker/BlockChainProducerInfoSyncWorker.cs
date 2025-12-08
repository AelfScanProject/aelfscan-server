using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using AElfScanServer.Worker.Core.Dtos;
using AElfScanServer.Worker.Core.Options;
using AElfScanServer.Worker.Core.Service;
using AElfScanServer.Common.HttpClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace AElfScanServer.Worker.Core.Worker;

public class BlockChainProducerInfoSyncWorker : AsyncPeriodicBackgroundWorkerBase
{
    private readonly IHttpProvider _httpProvider;
    private readonly WorkerOptions _workerOptions;
    private readonly IAddressService _addressService;
    private readonly BlockChainProducerInfoSyncWorkerOptions _options;
    private readonly ILogger<BlockChainProducerInfoSyncWorker> _logger;

    public BlockChainProducerInfoSyncWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
        ILogger<BlockChainProducerInfoSyncWorker> logger, IHttpProvider httpProvider, IAddressService addressService,
        IOptionsSnapshot<BlockChainProducerInfoSyncWorkerOptions> options,
        IOptionsSnapshot<WorkerOptions> workerOptions) : base(timer, serviceScopeFactory)
    {
        _logger = logger;
        _options = options.Value;
        _httpProvider = httpProvider;
        _addressService = addressService;
        _workerOptions = workerOptions.Value;
        Timer.Period = 1000 * _options.ExecuteInterval;
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        // var tasks = _workerOptions.Chains.Select(ExecuteSearchAsync);
        // await Task.WhenAll(tasks);
    }

    private async Task ExecuteSearchAsync(ChainOptionDto option)
    {
        var blockChainProducerInfoListResult = await _httpProvider.InvokeAsync<GetBlockChainProducersInfoResponseDto>(
            option.BasicInfoUrl, new ApiInfo(HttpMethod.Get, _options.BlockChainProducersUri),
            param: new Dictionary<string, string>
            {
                { "isActive", "true" }
            });

        var bpInfoList = blockChainProducerInfoListResult.Data;

        var (total, addressIndexList) = await _addressService.GetAddressIndexAsync(option.ChainId,
            bpInfoList.Where(p => !string.IsNullOrEmpty(p.Name)).Select(a => a.Address).ToList());
        addressIndexList.ForEach(p => p.Name = bpInfoList.FirstOrDefault(s => s.Address == p.Address)?.Name);

        if (total == 0)
        {
            _logger.LogDebug("No need update BlockChainProducer information");
            return;
        }

        await _addressService.BulkAddOrUpdateAsync(addressIndexList);
    }
}