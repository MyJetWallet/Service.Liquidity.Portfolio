using System;
using System.Runtime.Serialization;

namespace Service.Liquidity.Portfolio.Domain.Models
{
    [DataContract]
    public class AssetBalance
    {
        [DataMember(Order = 1)] public string WalletId { get; set; }
        [DataMember(Order = 2)] public string Asset { get; set; }
        [DataMember(Order = 3)] public double Volume { get; set; }
        [DataMember(Order = 4)] public DateTime UpdateDate { get; set; }
        
        public AssetBalance()
        {
        }
    }
}
