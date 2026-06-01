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

        // Tekuci prosek za Az
        private double _azSum = 0;
        private int _azCount = 0;

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

            // Provera odstupanja +-25% od proseka
            if (sample.LinearAccelerationZ < 0.75 * azMean ||
                sample.LinearAccelerationZ > 1.25 * azMean)
            {
                string smer = sample.LinearAccelerationZ < 0.75 * azMean
                    ? "ispod ocekivane vrednosti"
                    : "iznad ocekivane vrednosti";
                OnWarningRaised?.Invoke(
                    $"[OutOfBandWarning] Az={sample.LinearAccelerationZ:F2}, " +
                    $"AzMean={azMean:F2} - {smer}");
            }

            // Provera kinetičke energije vetra
            double wKinetic = 0.5 * sample.WindSpeed * sample.WindSpeed;
            if (wKinetic > W_threshold)
            {
                OnWarningRaised?.Invoke(
                    $"[WindEnergySpike] Wkinetic={wKinetic:F2} > W_threshold={W_threshold}");
            }
        }

        public void FireTransferCompleted(string sessionId)
        {
            OnTransferCompleted?.Invoke(sessionId);
            // Reset proseka za sledecu sesiju
            _azSum = 0;
            _azCount = 0;
        }
    }
}