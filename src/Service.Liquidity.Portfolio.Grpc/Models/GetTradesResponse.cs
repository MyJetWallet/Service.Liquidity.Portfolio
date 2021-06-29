using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Service.Liquidity.Portfolio.Domain.Models;

namespace Service.Liquidity.Portfolio.Grpc.Models
{
    [DataContract]
    public class GetTradesResponse
    {
        [DataMember(Order = 1)] public bool Success { get; set; }
        [DataMember(Order = 2)] public string ErrorMessage { get; set; }
        [DataMember(Order = 3)] public DateTime DateForNextQuery { get; set; }
        [DataMember(Order = 4)] public List<PortfolioTrade> Trades { get; set; }
    }
}
