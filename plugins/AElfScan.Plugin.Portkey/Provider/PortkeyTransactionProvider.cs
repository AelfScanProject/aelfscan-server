using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using AElf.Indexing.Elasticsearch;
using AElfScanServer.Common.ExceptionHandling;
using AElfScanServer.Common.IndexerPluginProvider;
using CAServer.CAActivity.Provider;
using GraphQL;
using Nest;
using Portkey.UserAssets;
using Volo.Abp.DependencyInjection;

namespace Portkey.Provider;

public class PortkeyTransactionProvider : IPortkeyTransactionProvider, ISingletonDependency, IAddressTypeProvider
{
    private readonly IGraphQLHelper _graphQlHelper;


    public PortkeyTransactionProvider(IGraphQLHelper graphQlHelper
    )
    {
        _graphQlHelper = graphQlHelper;
    }


    [ExceptionHandler( typeof(Exception),
        Message = "GetActivitiesAsync err",
        TargetType = typeof(ExceptionHandlingService),
        MethodName = nameof(ExceptionHandlingService.AlarmException),LogTargets = ["Portkey"])]
    public async Task<IndexerTransactions> GetActivitiesAsync(List<CAAddressInfo> caAddressInfos, string inputChainId,
        string symbolOpt, List<string> inputTransactionTypes, int inputSkipCount, int inputMaxResultCount,string transactionId = "")
    {
        return await _graphQlHelper.QueryAsync<IndexerTransactions>(new GraphQLRequest
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

    public async Task<string> GetAddressType(string chainId, string address)
    {
        var result = await GetCaHolderManagerInfoAsync(new List<string>() { address });

        if (result != null && !result.CaHolderManagerInfo.IsNullOrEmpty())
        {
            return "PortKey";
        }

        return "";
    }

    [ExceptionHandler( typeof(Exception),
        Message = "GetCaHolderManagerInfoAsync err",
        TargetType = typeof(ExceptionHandlingService),
        MethodName = nameof(ExceptionHandlingService.AlarmException),LogTargets = ["Portkey"])]
    public async Task<CAHolderInfo> GetCaHolderManagerInfoAsync(List<string> caAddresses)
    {
        return await _graphQlHelper.QueryAsync<CAHolderInfo>(new GraphQLRequest
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
}