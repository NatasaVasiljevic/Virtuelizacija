using System;
using System.ServiceModel;
using DronWcfService;

namespace DroneHost
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var host = new ServiceHost(typeof(DroneService)))
            {
                host.Open();
                Console.WriteLine("[SERVER] DroneService pokrenut na net.tcp://localhost:8733/DroneService/");
                Console.WriteLine("[SERVER] Pritisni Enter za zaustavljanje...");
                Console.ReadLine();
                host.Close();
            }
        }
    }
}