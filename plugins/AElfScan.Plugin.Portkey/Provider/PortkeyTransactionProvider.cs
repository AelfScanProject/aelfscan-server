using System.Diagnostics.Metrics;
using AElf.OpenTelemetry;
using AElfScanServer.Common.IndexerPluginProvider;
using CAServer.CAActivity.Provider;
using GraphQL;
using Microsoft.Extensions.Logging;
using Portkey.UserAssets;
using Volo.Abp.DependencyInjection;

namespace Portkey.Provider;

public class PortkeyTransactionProvider : IPortkeyTransactionProvider, ISingletonDependency, IAddressTypeProvider
{
    private readonly IGraphQLHelper _graphQlHelper;
    private static Counter<long> _alarmCounter;
    private readonly ILogger<GraphQLHelper> _logger;
    public PortkeyTransactionProvider(IGraphQLHelper graphQlHelper,IInstrumentationProvider instrumentationProvider
    )
    {
        _graphQlHelper = graphQlHelper;
        _alarmCounter = instrumentationProvider.Meter.
            CreateCounter<long>(
                "aelfScanAlarmCount", 
                "counts", 
                "The number of Alarm"
            );
    }


    public async Task<IndexerTransactions> GetActivitiesAsync(List<CAAddressInfo> caAddressInfos, string inputChainId,
        string symbolOpt, List<string> inputTransactionTypes, int inputSkipCount, int inputMaxResultCount,string transactionId = "")
    {
        try
        {
            return   await _graphQlHelper.QueryAsync<IndexerTransactions>(new GraphQLRequest
            {
                Query = @"
			    query ($chainId:String,$transactionId:String,$symbol:String,$caAddressInfos:[CAAddressInfo]!,$methodNames:[String],$startBlockHeight:Long!,$endBlockHeight:Long!,$skipCount:Int!,$maxResultCount:Int!) {
                    caHolderTransaction(dto: {chainId:$chainId,transactionId:$transactionId,symbol:$symbol,caAddressInfos:$caAddressInfos,methodNames:$methodNames,startBlockHeight:$startBlockHeight,endBlockHeight:$endBlockHeight,skipCount:$skipCount,maxResultCount:$maxResultCount}){
                        data{id,chainId,blockHash,blockHeight,previousBlockHash,transactionId,methodName,tokenInfo{symbol,tokenContractAddress,decimals,totalSupply,tokenName},status,timestamp,nftInfo{symbol,totalSupply,imageUrl,decimals,tokenName},transferInfo{fromAddress,toAddress,amount,toChainId,fromChainId,fromCAAddress},fromAddress,transactionFees{symbol,amount},isManagerConsumer,
                            toContractAddress,tokenTransferInfos{tokenInfo{symbol,decimals,tokenName,tokenContractAddress},nftInfo{symbol,decimals,tokenName,collectionName,collectionSymbol,inscriptionName,imageUrl},transferInfo{amount,fromAddress,fromCAAddress,toAddress,fromChainId,toChainId}}},totalRecordCount
                    }
                }",
                Variables = new
                {
                    caAddressInfos = caAddressInfos, chainId = inputChainId, symbol = symbolOpt,
                    methodNames = inputTransactionTypes, skipCount = inputSkipCount, maxResultCount = inputMaxResultCount,
                    startBlockHeight = 0, endBlockHeight = 0,transactionId=transactionId
                }
            });
        }
        catch (Exception e)
        {
            _alarmCounter.Add(1,new KeyValuePair<string, object>("alarmType","Portkey"));
            _logger.LogError(e,"GetActivitiesAsync error");
        }

        return new IndexerTransactions();
    }

    public async Task<string> GetAddressType(string chainId, string address)
    {
        var result = await GetCaHolderManagerInfoAsync(new List<string>() { address });

        if (result != null && !result.CaHolderManagerInfo.IsNullOrEmpty())
        {
            return "PortKey";
        }

        return "";
    }

    public async Task<CAHolderInfo> GetCaHolderManagerInfoAsync(List<string> caAddresses)
    {
        try
        {
            await _graphQlHelper.QueryAsync<CAHolderInfo>(new GraphQLRequest
            {
                Query = @"
			    query($caAddresses:[String],$skipCount:Int!,$maxResultCount:Int!) {
                    caHolderManagerInfo(dto: {caAddresses:$caAddresses,skipCount:$skipCount,maxResultCount:$maxResultCount}){
                        originChainId,chainId,caHash,caAddress, managerInfos{address,extraData}}
                }",
                Variables = new
                {
                    caAddresses, skipCount = 0, maxResultCount = caAddresses.Count
                }
            });
        }
        catch (Exception e)
        {
            _alarmCounter.Add(1,new KeyValuePair<string, object>("alarmType","Portkey"));
            _logger.LogError(e,"GetCaHolderManagerInfoAsync error");
        }
        return new CAHolderInfo();
    }
}