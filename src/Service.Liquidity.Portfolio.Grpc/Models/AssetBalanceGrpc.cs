using System;
using System.Runtime.Serialization;
using Service.Liquidity.Portfolio.Domain.Models;

namespace Service.Liquidity.Portfolio.Grpc.Models
{
    [DataContract]
    public class AssetBalanceGrpc : AssetBalance
    {
        [DataMember(Order = 1)] public new string BrokerId { get; set; }
        [DataMember(Order = 2)] public new string ClientId { get; set; }
        [DataMember(Order = 3)] public new string WalletId { get; set; }
        [DataMember(Order = 4)] public new string Asset { get; set; }
        [DataMember(Order = 5)] public new double Volume { get; set; }
        [DataMember(Order = 6)] public new DateTime UpdateDate { get; set; }
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
    }
}
