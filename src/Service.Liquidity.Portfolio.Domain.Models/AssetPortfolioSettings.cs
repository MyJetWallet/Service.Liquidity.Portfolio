using System.Runtime.Serialization;

namespace Service.Liquidity.Portfolio.Domain.Models
{
    [DataContract]
    public class AssetPortfolioSettings
    {
        [DataMember(Order = 1)] public string Asset { get; set; }
        [DataMember(Order = 2)] public double Normal { get; set; }
        [DataMember(Order = 3)] public double Warning { get; set; }
        [DataMember(Order = 4)] public double Danger { get; set; }
        [DataMember(Order = 5)] public double Critical { get; set; }
    }
}
