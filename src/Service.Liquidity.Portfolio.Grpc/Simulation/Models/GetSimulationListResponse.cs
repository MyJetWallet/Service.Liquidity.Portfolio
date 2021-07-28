using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Service.Liquidity.Portfolio.Grpc.Simulation.Models
{
    [DataContract]
    public class GetSimulationListResponse
    {
        [DataMember]
        public List<PortfolioSimulation> SimulationList { get; set; }
    }
}
