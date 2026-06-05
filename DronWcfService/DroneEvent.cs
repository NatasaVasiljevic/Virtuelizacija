using System;

namespace DroneWcfService
{
    // Delegati
    public delegate void TransferStartedHandler(string sessionId);
    public delegate void SampleReceivedHandler(DroneSample sample);
    public delegate void TransferCompletedHandler(string sessionId);
    public delegate void WarningRaisedHandler(string warningMessage);

    public class DroneEventManager
    {
        // Eventi
        public event TransferStartedHandler OnTransferStarted;
        public event SampleReceivedHandler OnSampleReceived;
        public event TransferCompletedHandler OnTransferCompleted;
        public event WarningRaisedHandler OnWarningRaised;

        // Pragovi iz konfiguracije
        public double W_threshold { get; set; }
        public double Az_threshold { get; set; }
        public double AzDeviationPercent { get; set; } = 25.0;

        // Tekuci prosek za Az
        private double _azSum = 0;
        private int _azCount = 0;

        private double? _previousAz = null;

        public void FireTransferStarted(string sessionId)
        {
            OnTransferStarted?.Invoke(sessionId);
        }

        public void FireSampleReceived(DroneSample sample)
        {
            OnSampleReceived?.Invoke(sample);

            // Racunamo tekuci prosek Az
            _azSum += sample.LinearAccelerationZ;
            _azCount++;
            double azMean = _azSum / _azCount;

            double allowedDeviation = AzDeviationPercent / 100.0;
            double lowerBound = (1.0 - allowedDeviation) * azMean;
            double upperBound = (1.0 + allowedDeviation) * azMean;

            // Provera odstupanja od proseka prema pragu iz konfiguracije.
            if (sample.LinearAccelerationZ < lowerBound ||
                sample.LinearAccelerationZ > upperBound)
            {
                string smer = sample.LinearAccelerationZ < lowerBound
                    ? "ispod ocekivane vrednosti"
                    : "iznad ocekivane vrednosti";
                OnWarningRaised?.Invoke(
                    $"[OutOfBandWarning] Az={sample.LinearAccelerationZ:F2}, " +
                    $"AzMean={azMean:F2}, prag=+-{AzDeviationPercent:F0}% - {smer}");
            }

            if(_previousAz.HasValue)
            {
                double deltaAz = sample.LinearAccelerationZ - _previousAz.Value;

                if(Math.Abs(deltaAz) > Az_threshold)
                {
                    string smer = deltaAz < 0
                        ? "nagli pad"
                        : "nagli skok";
                    OnWarningRaised?.Invoke($"[AltitudeDropSpike] DeltaAz={deltaAz:F2}, " +
                        $"Az_threshold={Az_threshold} - {smer}");
                }
            }

            _previousAz = sample.LinearAccelerationZ;

            double wKinetic = 0.5 * sample.WindSpeed * sample.WindSpeed;

            if(wKinetic > W_threshold)
            {
                OnWarningRaised?.Invoke($"[WindEnergySpike] Wkinetic={wKinetic:F2} > W_threshold={W_threshold}");
            }
        }

        public void FireTransferCompleted(string sessionId)
        {
            OnTransferCompleted?.Invoke(sessionId);
            // Reset proseka za sledecu sesiju
            _azSum = 0;
            _azCount = 0;
            _previousAz = null;
        }
    }
}