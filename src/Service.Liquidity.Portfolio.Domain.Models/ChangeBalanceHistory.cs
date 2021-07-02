using System;
using System.Runtime.Serialization;

namespace Service.Liquidity.Portfolio.Domain.Models
{
    [DataContract]
    public class ChangeBalanceHistory
    {
        [DataMember(Order = 1)] public long Id { get; set; }
        [DataMember(Order = 2)] public string BrokerId { get; set; }
        [DataMember(Order = 3)] public string ClientId { get; set; }
        [DataMember(Order = 4)] public string WalletId { get; set; }
        [DataMember(Order = 5)] public string Asset { get; set; }
        [DataMember(Order = 6)] public double VolumeDifference { get; set; }
        [DataMember(Order = 7)] public DateTime UpdateDate { get; set; }
    }
}
