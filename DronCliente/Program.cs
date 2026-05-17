using System;
using System.ServiceModel;
using DronWcfService;

namespace DronCliente
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("[KLIJENT] Pokretanje...");

            var binding = new NetTcpBinding()
            {
                MaxReceivedMessageSize = 10485760,
                ReceiveTimeout = TimeSpan.FromMinutes(10),
                SendTimeout = TimeSpan.FromMinutes(10)
            };

            var endpoint = new EndpointAddress("net.tcp://localhost:8733/DroneService/");

            var factory = new ChannelFactory<IDroneService>(binding, endpoint);
            IDroneService proxy = factory.CreateChannel();

            try
            {
                // StartSession
                var meta = new DroneSessionMeta
                {
                    SessionId = Guid.NewGuid().ToString(),
                    StartTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };

                string response = proxy.StartSession(meta);
                Console.WriteLine($"[KLIJENT] StartSession -> {response}");

                // PushSample (test uzorak)
                var sample = new DroneSample
                {
                    LinearAccelerationX = 0.1,
                    LinearAccelerationY = 0.2,
                    LinearAccelerationZ = 9.8,
                    WindSpeed = 5.0,
                    WindAngle = 45.0,
                    Time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };

                response = proxy.PushSample(sample);
                Console.WriteLine($"[KLIJENT] PushSample -> {response}");

                // EndSession
                response = proxy.EndSession();
                Console.WriteLine($"[KLIJENT] EndSession -> {response}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GREŠKA] {ex.Message}");
            }
            finally
            {
                ((IClientChannel)proxy).Close();
                factory.Close();
            }

            Console.WriteLine("[KLIJENT] Pritisni Enter za izlaz...");
            Console.ReadLine();
        }
    }
}