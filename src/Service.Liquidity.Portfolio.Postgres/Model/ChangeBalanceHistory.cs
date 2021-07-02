using System;

namespace Service.Liquidity.Portfolio.Postgres.Model
{
    public class ChangeBalanceHistory
    {
        public long Id { get; set; }
        public string BrokerId { get; set; }
        public string ClientId { get; set; }
        public string WalletId { get; set; }
        public string Asset { get; set; }
        public double VolumeDifference { get; set; }
        public DateTime UpdateDate { get; set; }
    }
}
