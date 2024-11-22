using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.CAActivity.Provider;
using Portkey.UserAssets;

namespace Portkey.Provider;

public interface IPortkeyTransactionProvider
{
    Task<CAHolderInfo> GetCaHolderManagerInfoAsync(List<string> userCaAddresses);

    Task<IndexerTransactions> GetActivitiesAsync(List<CAAddressInfo> caAddressInfos, string inputChainId,
        string symbolOpt, List<string> inputTransactionTypes, int inputSkipCount, int inputMaxResultCount,string transactionId = "");
}