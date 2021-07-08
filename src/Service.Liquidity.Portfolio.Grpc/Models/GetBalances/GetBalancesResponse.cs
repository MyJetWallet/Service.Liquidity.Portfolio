using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Service.Liquidity.Portfolio.Grpc.Models.GetBalances
{
    [DataContract]
    public class GetBalancesResponse
    {
        [DataMember(Order = 1)] public List<NetBalanceByWallet> BalanceByWallet { get; set; }
        [DataMember(Order = 2)] public List<NetBalanceByAsset> BalanceByAsset { get; set; }
        
    }
}
