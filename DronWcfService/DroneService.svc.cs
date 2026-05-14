using System;
using System.ServiceModel;

namespace DronWcfService
{
    public class DroneService : IDroneService
    {
        private static string _sessionId = null;

        public string StartSession(DroneSessionMeta meta)
        {
            _sessionId = meta.SessionId;
            Console.WriteLine($"[SERVER] Sesija pokrenuta: {_sessionId} u {meta.StartTime}");
            return "ACK|IN_PROGRESS";
        }

        public string PushSample(DroneSample sample)
        {
            if (_sessionId == null)
                return "NACK|NO_SESSION";

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