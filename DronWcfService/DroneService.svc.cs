using System;
using System.Configuration;
using System.IO;
using System.ServiceModel;

namespace DroneWcfService
{
    public class DroneService : IDroneService
    {
        private static string _sessionId = null;
        private static StreamWriter _measurementsWriter = null;
        private static StreamWriter _rejectsWriter = null;
        private static FileStream _measurementsStream = null;
        private static FileStream _rejectsStream = null;

        // Zadatak 8 - event manager
        private static DroneEventManager _eventManager = CreateEventManager();

        private static DroneEventManager CreateEventManager()
        {
            var manager = new DroneEventManager();

            // Ucitavamo pragove iz konfiguracije
            double.TryParse(ConfigurationManager.AppSettings["W_threshold"], out double wt);
            double.TryParse(ConfigurationManager.AppSettings["Az_threshold"], out double az);
            manager.W_threshold = wt > 0 ? wt : 50.0;
            manager.Az_threshold = az > 0 ? az : 5.0;

            // Pretplata na evente
            manager.OnTransferStarted += sessionId =>
                Console.WriteLine($"[EVENT] OnTransferStarted - Sesija: {sessionId}");

            manager.OnSampleReceived += sample =>
                Console.WriteLine($"[EVENT] OnSampleReceived - Az={sample.LinearAccelerationZ:F2}, WindSpeed={sample.WindSpeed:F2}");

            manager.OnTransferCompleted += sessionId =>
                Console.WriteLine($"[EVENT] OnTransferCompleted - Sesija: {sessionId}");

            manager.OnWarningRaised += warning =>
                Console.WriteLine($"[UPOZORENJE] {warning}");

            return manager;
        }

        public string StartSession(DroneSessionMeta meta)
        {
            if (meta == null)
                throw new FaultException<DataFormatFaultDetail>(
                    new DataFormatFaultDetail { FieldName = "meta", Message = "Meta objekat ne sme biti null." });
            if (string.IsNullOrWhiteSpace(meta.SessionId))
                throw new FaultException<DataFormatFaultDetail>(
                    new DataFormatFaultDetail { FieldName = "SessionId", Message = "SessionId je obavezno polje." });
            if (string.IsNullOrWhiteSpace(meta.StartTime))
                throw new FaultException<DataFormatFaultDetail>(
                    new DataFormatFaultDetail { FieldName = "StartTime", Message = "StartTime je obavezno polje." });

            _sessionId = meta.SessionId;

            // Zadatak 6 - kreiranje fajlova
            string folder = ConfigurationManager.AppSettings["MeasurementsPath"] ?? "Measurements";
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            string measurementsFile = Path.Combine(folder, $"measurements_{_sessionId}.csv");
            string rejectsFile = Path.Combine(folder, $"rejects_{_sessionId}.csv");

            _measurementsStream = new FileStream(measurementsFile, FileMode.Create, FileAccess.Write);
            _measurementsWriter = new StreamWriter(_measurementsStream);
            _measurementsWriter.AutoFlush = true;
            _measurementsWriter.WriteLine("Time,LinearAccelerationX,LinearAccelerationY,LinearAccelerationZ,WindSpeed,WindAngle");

            _rejectsStream = new FileStream(rejectsFile, FileMode.Create, FileAccess.Write);
            _rejectsWriter = new StreamWriter(_rejectsStream);
            _rejectsWriter.AutoFlush = true;
            _rejectsWriter.WriteLine("Time,Razlog");

            Console.WriteLine($"[SERVER] Sesija pokrenuta: {_sessionId} u {meta.StartTime}");
            Console.WriteLine($"[SERVER] Fajlovi kreirani: {measurementsFile}");
            Console.WriteLine("[SERVER] Prenos u toku...");

            // Zadatak 8 - podizemo event
            _eventManager.FireTransferStarted(_sessionId);

            return "ACK|IN_PROGRESS";
        }

        public string PushSample(DroneSample sample)
        {
            if (_sessionId == null)
                return "NACK|NO_SESSION";

            if (sample == null)
                throw new FaultException<DataFormatFaultDetail>(
                    new DataFormatFaultDetail { FieldName = "sample", Message = "Uzorak ne sme biti null." });
            if (string.IsNullOrWhiteSpace(sample.Time))
                throw new FaultException<DataFormatFaultDetail>(
                    new DataFormatFaultDetail { FieldName = "Time", Message = "Polje Time je obavezno." });
            if (sample.WindSpeed < 0)
            {
                _rejectsWriter?.WriteLine($"{sample.Time},WindSpeed negativan: {sample.WindSpeed}");
                throw new FaultException<ValidationFaultDetail>(
                    new ValidationFaultDetail
                    {
                        FieldName = "WindSpeed",
                        Message = "WindSpeed mora biti >= 0.",
                        ActualValue = sample.WindSpeed,
                        AllowedRange = ">= 0"
                    });
            }
            if (sample.WindAngle < -180 || sample.WindAngle > 360)
            {
                _rejectsWriter?.WriteLine($"{sample.Time},WindAngle van opsega: {sample.WindAngle}");
                throw new FaultException<ValidationFaultDetail>(
                    new ValidationFaultDetail
                    {
                        FieldName = "WindAngle",
                        Message = "WindAngle mora biti izmedju -180 i 360.",
                        ActualValue = sample.WindAngle,
                        AllowedRange = "[-180, 360]"
                    });
            }
            if (double.IsNaN(sample.LinearAccelerationX) || double.IsInfinity(sample.LinearAccelerationX))
            {
                _rejectsWriter?.WriteLine($"{sample.Time},LinearAccelerationX nije validna vrednost");
                throw new FaultException<ValidationFaultDetail>(
                    new ValidationFaultDetail { FieldName = "LinearAccelerationX", Message = "Vrednost nije validna (NaN/Infinity)." });
            }
            if (double.IsNaN(sample.LinearAccelerationY) || double.IsInfinity(sample.LinearAccelerationY))
            {
                _rejectsWriter?.WriteLine($"{sample.Time},LinearAccelerationY nije validna vrednost");
                throw new FaultException<ValidationFaultDetail>(
                    new ValidationFaultDetail { FieldName = "LinearAccelerationY", Message = "Vrednost nije validna (NaN/Infinity)." });
            }
            if (double.IsNaN(sample.LinearAccelerationZ) || double.IsInfinity(sample.LinearAccelerationZ))
            {
                _rejectsWriter?.WriteLine($"{sample.Time},LinearAccelerationZ nije validna vrednost");
                throw new FaultException<ValidationFaultDetail>(
                    new ValidationFaultDetail { FieldName = "LinearAccelerationZ", Message = "Vrednost nije validna (NaN/Infinity)." });
            }

            // Zadatak 6 - snimanje u csv
            _measurementsWriter?.WriteLine($"{sample.Time},{sample.LinearAccelerationX},{sample.LinearAccelerationY},{sample.LinearAccelerationZ},{sample.WindSpeed},{sample.WindAngle}");

            // Zadatak 7 - sekvencijalni ispis
            Console.WriteLine($"[SERVER] Prenos u toku... Az={sample.LinearAccelerationZ:F2}, WindSpeed={sample.WindSpeed:F2}, Time={sample.Time}");

            // Zadatak 8 - podizemo event
            _eventManager.FireSampleReceived(sample);

            return "ACK|IN_PROGRESS";
        }

        public string EndSession()
        {
            Console.WriteLine($"[SERVER] Sesija završena: {_sessionId}");

            // Zadatak 6 - zatvaranje fajlova
            _measurementsWriter?.Close();
            _measurementsWriter?.Dispose();
            _measurementsStream?.Dispose();
            _rejectsWriter?.Close();
            _rejectsWriter?.Dispose();
            _rejectsStream?.Dispose();

            _measurementsWriter = null;
            _measurementsStream = null;
            _rejectsWriter = null;
            _rejectsStream = null;

            // Zadatak 7 - završen prenos
            Console.WriteLine("[SERVER] Završen prenos.");

            // Zadatak 8 - podizemo event
            _eventManager.FireTransferCompleted(_sessionId);

            _sessionId = null;
            return "ACK|COMPLETED";
        }
    }
}