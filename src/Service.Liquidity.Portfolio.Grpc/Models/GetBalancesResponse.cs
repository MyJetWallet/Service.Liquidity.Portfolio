using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Service.Liquidity.Portfolio.Domain.Models;

namespace Service.Liquidity.Portfolio.Grpc.Models
{
    [DataContract]
    public class GetBalancesResponse
    {
        [DataMember(Order = 1)]
        public List<AssetBalanceGrpc> Balances { get; set; }

        public void SetBalances(List<AssetBalance> balanceList)
        {
            Balances = balanceList.Select(elem => new AssetBalanceGrpc(elem)).ToList();
        }
    }
}
