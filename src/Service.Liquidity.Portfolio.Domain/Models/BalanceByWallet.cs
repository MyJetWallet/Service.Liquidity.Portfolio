using System.Runtime.Serialization;

namespace Service.Liquidity.Portfolio.Domain.Models
{
    [DataContract]
    public class BalanceByWallet
    {
        [DataMember(Order = 1)] public string BrokerId { get; set; }
        [DataMember(Order = 2)] public string WalletName { get; set; }
        [DataMember(Order = 3)] public decimal Volume { get; set; }
        [DataMember(Order = 4)] public decimal UsdVolume { get; set; }
        [DataMember(Order = 5)] public bool IsInternal { get; set; }

        public BalanceByWallet GetCopy()
        {
            return new BalanceByWallet
            {
                BrokerId = BrokerId,
                WalletName = WalletName,
                Volume = Volume,
                UsdVolume = UsdVolume,
                IsInternal = IsInternal
            };
        }
    }
}
