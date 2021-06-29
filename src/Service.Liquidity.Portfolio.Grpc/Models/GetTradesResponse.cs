using System.Collections.Generic;
using System.Runtime.Serialization;
using Service.Liquidity.Portfolio.Domain.Models;

namespace Service.Liquidity.Portfolio.Grpc.Models
{
    [DataContract]
    public class GetTradesResponse
    {
        [DataMember(Order = 1)]
        public List<PortfolioTrade> Trades { get; set; }
    }
}
