using System.Collections.Generic;
using System.Runtime.Serialization;
using Service.Liquidity.Portfolio.Domain.Models;

namespace Service.Liquidity.Portfolio.Grpc.Simulation.Models
{
    [DataContract]
    public class PortfolioSimulation
    {
        [DataMember]
        public long SimulationId { get; set; }
        
        [DataMember]
        public AssetPortfolio Portfolio { get; set; }
        
        [DataMember]
        public List<AssetPortfolioTrade> Trades { get; set; }
    }
}
