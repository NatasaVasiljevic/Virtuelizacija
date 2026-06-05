using System;
using System.IO;

namespace DroneCliente
{
    public static class DisposePatternProof
    {
        public static void Run()
        {
            string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "dispose_test");
            Directory.CreateDirectory(folder);

            string csvPath = Path.Combine(folder, "dispose_test.csv");
            string logPath = Path.Combine(folder, "dispose_test.log");

            File.WriteAllText(csvPath,
                "time,wind_speed,wind_angle,linear_acceleration_x,linear_acceleration_y,linear_acceleration_z" + Environment.NewLine +
                "t1,3.2,20,0.1,0.2,9.8" + Environment.NewLine);

            try
            {
                using (var reader = new CsvDroneReader(csvPath, logPath))
                {
                    reader.ReadSamples();
                    throw new InvalidOperationException("Simulacija prekida veze usred prenosa.");
                }
            }
            catch (InvalidOperationException)
            {
                // Ako Dispose nije oslobodio resurse, sledeci exclusive open ce pasti.
                using (File.Open(csvPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }
                using (File.Open(logPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }
                Console.WriteLine("[DISPOSE TEST] Resursi su zatvoreni posle simuliranog izuzetka.");
            }
        }
    }
}
