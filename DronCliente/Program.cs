using DronWcfService;
using System;
using System.IO;
using System.ServiceModel;

namespace DronCliente
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("[KLIJENT] Pokretanje...");

            string csvPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "flights_data.csv");
            string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "invalid_rows.log");

            if (!File.Exists(csvPath))
            {
                Console.WriteLine($"[GRESKA] CSV fajl nije pronadjen: {csvPath}");
                Console.WriteLine("Stavi flights_data.csv u isti folder kao DroneCliente.exe");
                Console.ReadLine();
                return;
            }

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
                using (var reader = new CsvDroneReader(csvPath, logPath))
                {
                    var samples = reader.ReadSamples();
                    Console.WriteLine($"[KLIJENT] Ucitano {samples.Count} uzoraka iz CSV-a.");

                    var meta = new DroneSessionMeta
                    {
                        SessionId = Guid.NewGuid().ToString(),
                        StartTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                    };

                    string response = proxy.StartSession(meta);
                    Console.WriteLine($"[KLIJENT] StartSession -> {response}");

                    int sent = 0, rejected = 0;

                    for (int i = 0; i < samples.Count; i++)
                    {
                        var s = samples[i];
                        var sample = new DroneSample
                        {
                            LinearAccelerationX = s.LinearAccelerationX,
                            LinearAccelerationY = s.LinearAccelerationY,
                            LinearAccelerationZ = s.LinearAccelerationZ,
                            WindSpeed = s.WindSpeed,
                            WindAngle = s.WindAngle,
                            Time = s.Time
                        };

                        try
                        {
                            response = proxy.PushSample(sample);
                            if (response.StartsWith("ACK"))
                                sent++;
                            else
                            {
                                Console.WriteLine($"[KLIJENT] Uzorak {i + 1} odbacen: {response}");
                                rejected++;
                            }
                        }
                        catch (FaultException<DataFormatFaultDetail> ex)
                        {
                            Console.WriteLine($"[KLIJENT] Format greska na uzorku {i + 1}: [{ex.Detail.FieldName}] {ex.Detail.Message}");
                            rejected++;
                        }
                        catch (FaultException<ValidationFaultDetail> ex)
                        {
                            Console.WriteLine($"[KLIJENT] Validacija greska na uzorku {i + 1}: [{ex.Detail.FieldName}] {ex.Detail.Message}");
                            rejected++;
                        }
                    }

                    response = proxy.EndSession();
                    Console.WriteLine($"[KLIJENT] EndSession -> {response}");
                    Console.WriteLine($"[KLIJENT] Poslato: {sent}, Odbaceno: {rejected}");
                }
            }
            catch (FaultException<DataFormatFaultDetail> ex)
            {
                Console.WriteLine($"[GRESKA] DataFormatFault: {ex.Detail.Message}");
            }
            catch (FaultException<ValidationFaultDetail> ex)
            {
                Console.WriteLine($"[GRESKA] ValidationFault: {ex.Detail.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GRESKA] {ex.Message}");
            }
            finally
            {
                try { ((IClientChannel)proxy).Close(); } catch { ((IClientChannel)proxy).Abort(); }
                try { factory.Close(); } catch { factory.Abort(); }
            }

            Console.WriteLine("[KLIJENT] Gotovo. Pritisni Enter za izlaz...");
            Console.ReadLine();
        }
    }
}