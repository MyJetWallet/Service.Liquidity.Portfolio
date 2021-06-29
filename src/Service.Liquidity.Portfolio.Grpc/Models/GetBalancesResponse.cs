using System.Collections.Generic;
using System.Runtime.Serialization;
using Service.Liquidity.Portfolio.Domain.Models;

namespace Service.Liquidity.Portfolio.Grpc.Models
{
    [DataContract]
    public class GetBalancesResponse
    {
        [DataMember(Order = 1)]
        public List<AssetBalance> Balances { get; set; }
    }
}
