using System;
using System.Collections.Generic;
using Service.Liquidity.Portfolio.Domain.Models;

namespace Service.Liquidity.Portfolio.Services
{
    public class AssetPortfolioMath
    {
        public void UpdateBalance(AssetBalance balance, AssetBalanceDifference difference, bool forceSet = false)
        {
            // for SetBalance
            if (forceSet)
            {
                balance.Volume = 0m;
            }
            if ((balance.Volume >= 0 && difference.Volume > 0) || (balance.Volume <= 0 && difference.Volume < 0))
            {
                balance.OpenPrice =
                    (balance.Volume * balance.OpenPrice + difference.Volume * difference.CurrentPriceInUsd) /
                    (difference.Volume + balance.Volume);
                balance.Volume += difference.Volume;
                return;
            }
            var originalVolume = balance.Volume;
            var decreaseVolumeAbs = Math.Min(Math.Abs(balance.Volume), Math.Abs(difference.Volume));
            if (decreaseVolumeAbs > 0)
            {
                if (balance.Volume > 0)
                    balance.Volume -= decreaseVolumeAbs;
                else
                    balance.Volume += decreaseVolumeAbs;

                if (balance.Volume == 0)
                    balance.OpenPrice = 0;
            }
            if (decreaseVolumeAbs < Math.Abs(difference.Volume))
            {
                balance.Volume = difference.Volume + originalVolume;
                balance.OpenPrice = difference.CurrentPriceInUsd; 
            }
        }
    }
}
