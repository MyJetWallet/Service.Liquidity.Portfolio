using System.Collections.Generic;
using System.Runtime.Serialization;
using Service.Liquidity.Portfolio.Domain.Models;

namespace Service.Liquidity.Portfolio.Grpc.Simulation.Models
{
    [DataContract]
    public class PortfolioSimulation
    {
        [DataMember(Order = 1)]
        public long SimulationId { get; set; }
        
        [DataMember(Order = 2)]
        public AssetPortfolio Portfolio { get; set; }
        
        [DataMember(Order = 3)]
        public List<AssetPortfolioTrade> Trades { get; set; }
        
        [DataMember(Order = 4)]
        public Dictionary<string, decimal> PriceMap = new Dictionary<string, decimal>();

        public PortfolioSimulation()
        {
        }

        public PortfolioSimulation(long simulationId)
        {
            SimulationId = simulationId;
            Portfolio = new AssetPortfolio();
            Trades = new List<AssetPortfolioTrade>();
            PriceMap = new Dictionary<string, decimal>()
            {
                {"USD", 1}
            };
        }
    }
}
