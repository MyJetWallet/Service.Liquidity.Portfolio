using System.Runtime.Serialization;

namespace Service.Liquidity.Portfolio.Grpc.Models.GetBalances
{
    [DataContract]
    public enum AssetBalanceState
    {
        [DataMember(Order = 1)] Undefined,
        [DataMember(Order = 2)] Normal,
        [DataMember(Order = 3)] Warning,
        [DataMember(Order = 4)] Danger,
        [DataMember(Order = 5)] Critical
    }
}
