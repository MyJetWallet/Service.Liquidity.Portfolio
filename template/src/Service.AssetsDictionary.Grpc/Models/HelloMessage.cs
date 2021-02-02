using System.Runtime.Serialization;
using Service.AssetsDictionary.Domain.Models;

namespace Service.AssetsDictionary.Grpc.Models
{
    [DataContract]
    public class HelloMessage : IHelloMessage
    {
        [DataMember(Order = 1)]
        public string Message { get; set; }
    }
}