using System;
using System.ServiceModel;

namespace DronWcfService
{
    public class DroneService : IDroneService
    {
        private static string _sessionId = null;

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
            Console.WriteLine($"[SERVER] Sesija pokrenuta: {_sessionId} u {meta.StartTime}");
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
                throw new FaultException<ValidationFaultDetail>(
                    new ValidationFaultDetail
                    {
                        FieldName = "WindSpeed",
                        Message = "WindSpeed mora biti >= 0.",
                        ActualValue = sample.WindSpeed,
                        AllowedRange = ">= 0"
                    });

            if (sample.WindAngle < -180 || sample.WindAngle > 360)
                throw new FaultException<ValidationFaultDetail>(
                    new ValidationFaultDetail
                    {
                        FieldName = "WindAngle",
                        Message = "WindAngle mora biti izmedju -180 i 360.",
                        ActualValue = sample.WindAngle,
                        AllowedRange = "[-180, 360]"
                    });

            if (double.IsNaN(sample.LinearAccelerationX) || double.IsInfinity(sample.LinearAccelerationX))
                throw new FaultException<ValidationFaultDetail>(
                    new ValidationFaultDetail { FieldName = "LinearAccelerationX", Message = "Vrednost nije validna (NaN/Infinity)." });

            if (double.IsNaN(sample.LinearAccelerationY) || double.IsInfinity(sample.LinearAccelerationY))
                throw new FaultException<ValidationFaultDetail>(
                    new ValidationFaultDetail { FieldName = "LinearAccelerationY", Message = "Vrednost nije validna (NaN/Infinity)." });

            if (double.IsNaN(sample.LinearAccelerationZ) || double.IsInfinity(sample.LinearAccelerationZ))
                throw new FaultException<ValidationFaultDetail>(
                    new ValidationFaultDetail { FieldName = "LinearAccelerationZ", Message = "Vrednost nije validna (NaN/Infinity)." });

            Console.WriteLine($"[SERVER] Primljen uzorak: Az={sample.LinearAccelerationZ}, WindSpeed={sample.WindSpeed}, Time={sample.Time}");
            return "ACK|IN_PROGRESS";
        }

        public string EndSession()
        {
            Console.WriteLine($"[SERVER] Sesija završena: {_sessionId}");
            _sessionId = null;
            return "ACK|COMPLETED";
        }
    }
}