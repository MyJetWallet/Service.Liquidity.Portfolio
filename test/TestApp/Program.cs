using System;
using System.Threading.Tasks;
using ProtoBuf.Grpc.Client;
using Service.Liquidity.Portfolio.Client;
using Service.Liquidity.Portfolio.Grpc.Models;

namespace TestApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            GrpcClientFactory.AllowUnencryptedHttp2 = true;

            Console.Write("Press enter to start");
            Console.ReadLine();


            var factory = new PortfolioClientFactory("http://localhost:5001");
            var client = factory.GetHelloService();
            
            Console.WriteLine("End");
            Console.ReadLine();
        }
    }
}
