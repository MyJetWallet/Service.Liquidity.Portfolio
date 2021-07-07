using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Service.Liquidity.Portfolio.Domain.Models;

namespace Service.Liquidity.Portfolio.Grpc.Models.GetBalances
{
    [DataContract]
    public class GetBalancesResponse
    {
        [DataMember(Order = 1)] public List<NetBalanceByWallet> BalanceByWallet { get; set; }
        [DataMember(Order = 2)] public List<NetBalanceByAsset> BalanceByAsset { get; set; }
        
    }
}
