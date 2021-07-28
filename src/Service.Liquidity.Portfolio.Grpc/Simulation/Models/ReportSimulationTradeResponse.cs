using System.Runtime.Serialization;

namespace Service.Liquidity.Portfolio.Grpc.Simulation.Models
{
    [DataContract]
    public class ReportSimulationTradeResponse
    {
        [DataMember] public bool Success { get; set; }
    }
}
