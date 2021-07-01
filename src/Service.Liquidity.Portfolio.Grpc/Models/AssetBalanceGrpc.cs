using System;
using System.Runtime.Serialization;
using Service.Liquidity.Portfolio.Domain.Models;

namespace Service.Liquidity.Portfolio.Grpc.Models
{
    [DataContract]
    public class AssetBalanceGrpc
    {
        [DataMember(Order = 1)] public string BrokerId { get; set; }
        [DataMember(Order = 2)] public string ClientId { get; set; }
        [DataMember(Order = 3)] public string WalletId { get; set; }
        [DataMember(Order = 4)] public string Asset { get; set; }
        [DataMember(Order = 5)] public double Volume { get; set; }
        [DataMember(Order = 6)] public DateTime UpdateDate { get; set; }
        [DataMember(Order = 7)] public double UsdProjection { get; set; }

        public AssetBalanceGrpc(AssetBalance assetBalance)
        {
            BrokerId = assetBalance.BrokerId;
            ClientId = assetBalance.ClientId;
            WalletId = assetBalance.WalletId;
            Asset = assetBalance.Asset;
            Volume = assetBalance.Volume;
            UpdateDate = assetBalance.UpdateDate;
        }
        public AssetBalanceGrpc()
        {
        }

        public AssetBalance GetDomainModel()
        {
            return new AssetBalance()
            {
                BrokerId = this.BrokerId,
                ClientId = this.ClientId,
                WalletId = this.WalletId,
                Asset = this.Asset,
                Volume = this.Volume,
                UpdateDate = this.UpdateDate
            };
        }
    }
}
