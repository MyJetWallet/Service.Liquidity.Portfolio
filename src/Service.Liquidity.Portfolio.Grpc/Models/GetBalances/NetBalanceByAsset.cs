using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Service.Liquidity.Portfolio.Domain.Models;

namespace Service.Liquidity.Portfolio.Grpc.Models.GetBalances
{
    [DataContract]
    public class NetBalanceByAsset
    {
        public const string TopicName = "asset-balance";
        
        [DataMember(Order = 1)] public string Asset { get; set; }
        [DataMember(Order = 2)] public List<NetBalanceByWallet> WalletBalances { get; set; }
        [DataMember(Order = 3)] public AssetBalanceState BalanceState { get; set; }
        [DataMember(Order = 4)] public double NetVolume { get; set; }
        [DataMember(Order = 5)] public double NetUsdVolume { get; set; }
        [DataMember(Order = 6)] public AssetPortfolioSettings Settings { get; set; }
        
        public NetBalanceByAsset()
        {
            BalanceState = AssetBalanceState.Undefined;
        }

        public void SetState(AssetPortfolioSettings assetBalanceSettings)
        {
            if (assetBalanceSettings.Asset != Asset)
                throw new Exception("Bad asset settings");

            if (NetVolume < assetBalanceSettings.NetWarningLevel)
            {
                BalanceState = AssetBalanceState.Normal;
            } else if (NetVolume < assetBalanceSettings.NetDangerLevel)
            {
                BalanceState = AssetBalanceState.Warning;
            } else if (NetVolume < assetBalanceSettings.NetCriticalLevel)
            {
                BalanceState = AssetBalanceState.Danger;
            } else
            {
                BalanceState = AssetBalanceState.Critical;
            }
        }
    }
}
