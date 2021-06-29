using System;
using System.Runtime.Serialization;

namespace Service.Liquidity.Portfolio.Grpc.Models
{
    [DataContract]
    public class GetTradesRequest
    {
        [DataMember(Order = 1)] public DateTime LastDate { get; set; }
        [DataMember(Order = 2)] public int BatchSize { get; set; }
    }
}
