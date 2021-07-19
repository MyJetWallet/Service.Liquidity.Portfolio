using System;
using System.Runtime.Serialization;

namespace Service.Liquidity.Portfolio.Domain.Models
{
    [DataContract]
    public class ChangeBalanceHistory
    {
        public const string TopicName = "jetwallet-liquidity-portfolio-changebalancehistory";
        
        [DataMember(Order = 1)] public long Id { get; set; }
        [DataMember(Order = 2)] public string BrokerId { get; set; }
        [DataMember(Order = 3)] public string WalletName { get; set; }
        [DataMember(Order = 4)] public string Asset { get; set; }
        [DataMember(Order = 5)] public double VolumeDifference { get; set; }
        [DataMember(Order = 6)] public DateTime UpdateDate { get; set; }
        [DataMember(Order = 7)] public string Comment { get; set; }
        [DataMember(Order = 8)] public string User { get; set; }
        
    }
}
