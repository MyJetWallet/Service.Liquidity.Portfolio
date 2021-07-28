using System.Runtime.Serialization;

namespace Service.Liquidity.Portfolio.Grpc.Simulation.Models
{
    [DataContract]
    public class ReportSimulationTradeRequest
    {
        [DataMember]
        public long SimulationId { get; set; }
        
        [DataMember]
        public string BaseAsset { get; set; }
        
        [DataMember]
        public string QuoteAsset { get; set; }
        
        [DataMember]
        public decimal BaseVolume { get; set; }
        
        [DataMember]
        public decimal QuoteVolume { get; set; }
        
        [DataMember]
        public decimal BaseAssetIndexPrice { get; set; }
        
        [DataMember]
        public decimal QuoteAssetIndexPrice { get; set; }
    }
}
