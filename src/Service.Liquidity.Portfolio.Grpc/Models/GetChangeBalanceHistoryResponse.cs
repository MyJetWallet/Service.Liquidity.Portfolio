using System.Collections.Generic;
using System.Runtime.Serialization;
using Service.Liquidity.Portfolio.Domain.Models;

namespace Service.Liquidity.Portfolio.Grpc.Models
{
    [DataContract]
    public class GetChangeBalanceHistoryResponse
    {
        [DataMember(Order = 1)] public bool Success { get; set; }
        [DataMember(Order = 2)] public List<ChangeBalanceHistory> Histories { get; set; }
        [DataMember(Order = 3)] public string ErrorText { get; set; }
    }
}
