﻿using System;
using System.Runtime.Serialization;

namespace Service.Liquidity.Portfolio.Domain.Models
{
    [DataContract]
    public class ManualSettlement
    {
        public const string TopicName = "jetwallet-liquidity-portfolio-manualsettlement";
        
        [DataMember(Order = 1)] public string BrokerId { get; set; }
        [DataMember(Order = 2)] public string WalletFrom { get; set; }
        [DataMember(Order = 3)] public string WalletTo { get; set; }
        [DataMember(Order = 4)] public string Asset { get; set; }
        [DataMember(Order = 5)] public decimal VolumeFrom { get; set; }
        [DataMember(Order = 6)] public decimal VolumeTo { get; set; }
        [DataMember(Order = 7)] public string Comment { get; set; }
        [DataMember(Order = 8)] public string User { get; set; }
        [DataMember(Order = 9)] public DateTime SettlementDate { get; set; }
    }
}
