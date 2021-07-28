using System.Runtime.Serialization;

namespace Service.Liquidity.Portfolio.Grpc.Simulation.Models
{
    [DataContract]
    public class GetSimulationRequest
    {
        [DataMember] public long SimulationId { get; set; }
    }
}
