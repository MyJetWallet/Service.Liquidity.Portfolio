using System.Runtime.Serialization;

namespace Service.Liquidity.Portfolio.Grpc.Models.GetBalances
{
    [DataContract]
    public enum AssetBalanceState
    {
        [DataMember(Order = 1)] Normal,
        [DataMember(Order = 2)] Warning,
        [DataMember(Order = 3)] Danger,
        [DataMember(Order = 4)] Critical
    }
}
