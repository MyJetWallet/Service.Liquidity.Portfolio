using System.Runtime.Serialization;

namespace Service.Liquidity.Portfolio.Domain.Models
{
    [DataContract]
    public class AssetPortfolioSettings
    {
        [DataMember(Order = 1)] public string Asset { get; set; }
        [DataMember(Order = 2)] public double NetWarningLevel { get; set; }
        [DataMember(Order = 3)] public double NetDangerLevel { get; set; }
        [DataMember(Order = 4)] public double NetCriticalLevel { get; set; }
    }
}
