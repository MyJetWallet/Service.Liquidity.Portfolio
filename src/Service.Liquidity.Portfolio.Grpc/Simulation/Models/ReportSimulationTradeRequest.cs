﻿using System.Runtime.Serialization;

namespace Service.Liquidity.Portfolio.Grpc.Simulation.Models
{
    [DataContract]
    public class ReportSimulationTradeRequest
    {
        [DataMember(Order = 1)]
        public long SimulationId { get; set; }
        
        [DataMember(Order = 2)]
        public string BaseAsset { get; set; }
        
        [DataMember(Order = 3)]
        public string QuoteAsset { get; set; }
        
        [DataMember(Order = 4)]
        public decimal BaseVolume { get; set; }
        
        [DataMember(Order = 5)]
        public decimal QuoteVolume { get; set; }
        
        [DataMember(Order = 6)]
        public string WalletName { get; set; }
    }
}
