using System.Runtime.Serialization;

namespace Service.Liquidity.Portfolio.Grpc.Simulation.Models
{
    [DataContract]
    public class DeleteSimulationResponse
    {
        [DataMember(Order = 1)] public bool Success { get; set; }
        [DataMember(Order = 2)] public string ErrorText { get; set; }
    }
}
