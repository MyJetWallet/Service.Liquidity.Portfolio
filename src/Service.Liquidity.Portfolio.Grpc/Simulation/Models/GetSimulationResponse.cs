using System.Runtime.Serialization;

namespace Service.Liquidity.Portfolio.Grpc.Simulation.Models
{
    [DataContract]
    public class GetSimulationResponse
    {
        [DataMember] public PortfolioSimulation Portfolio { get; set; }
    }
}
