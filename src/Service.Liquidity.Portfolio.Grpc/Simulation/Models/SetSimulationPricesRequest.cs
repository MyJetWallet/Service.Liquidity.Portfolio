using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Service.Liquidity.Portfolio.Grpc.Simulation.Models
{
    [DataContract]
    public class SetSimulationPricesRequest
    {
        [DataMember(Order = 1)] public long SimulationId { get; set; }
        [DataMember(Order = 2)] public Dictionary<string, decimal> PriceMap { get; set; }
    }
}
