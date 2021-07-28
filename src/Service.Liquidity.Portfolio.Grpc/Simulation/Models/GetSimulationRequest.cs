using System.Runtime.Serialization;

namespace Service.Liquidity.Portfolio.Grpc.Simulation.Models
{
    [DataContract]
    public class GetSimulationRequest
    {
        [DataMember(Order = 1)] public long SimulationId { get; set; }
    }
}
