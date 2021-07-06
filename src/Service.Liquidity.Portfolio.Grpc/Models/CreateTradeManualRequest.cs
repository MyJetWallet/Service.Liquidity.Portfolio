using System.Runtime.Serialization;

namespace Service.Liquidity.Portfolio.Grpc.Models
{
    [DataContract]
    public class CreateTradeManualRequest
    {
        [DataMember(Order = 1)] public string BrokerId { get; set; }
        [DataMember(Order = 2)] public string ClientId { get; set; }
        [DataMember(Order = 3)] public string WalletId { get; set; }
        [DataMember(Order = 4)] public string Symbol { get; set; }
        [DataMember(Order = 6)] public double Price { get; set; }
        [DataMember(Order = 7)] public double BaseVolume { get; set; }
        [DataMember(Order = 8)] public double QuoteVolume { get; set; }
        [DataMember(Order = 9)] public string Comment { get; set; }
        [DataMember(Order = 10)] public string User { get; set; }
    }
}
