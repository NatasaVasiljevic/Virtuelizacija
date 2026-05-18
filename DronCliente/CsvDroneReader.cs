using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace DroneCliente
{
    public class CsvDroneReader : IDisposable
    {
        private FileStream _fileStream;
        private StreamReader _streamReader;
        private StreamWriter _logWriter;
        private FileStream _logStream;
        private bool _disposed = false;
        public const int MaxRows = 110;

        public CsvDroneReader(string csvPath, string logPath)
        {
            _fileStream = new FileStream(csvPath, FileMode.Open, FileAccess.Read);
            _streamReader = new StreamReader(_fileStream);
            _logStream = new FileStream(logPath, FileMode.Create, FileAccess.Write);
            _logWriter = new StreamWriter(_logStream);
            _logWriter.AutoFlush = true;
            _logWriter.WriteLine($"[LOG] Pokretanje citanja fajla: {csvPath}");
            _logWriter.WriteLine($"[LOG] Vreme: {DateTime.Now}");
        }

        public List<DroneSampleLocal> ReadSamples()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(CsvDroneReader));

            var samples = new List<DroneSampleLocal>();
            string headerLine = _streamReader.ReadLine();

            if (headerLine == null)
            {
                _logWriter.WriteLine("[LOG] GRESKA: Fajl je prazan.");
                return samples;
            }

            string[] headers = headerLine.Split(',');
            int idxAx = IndexOf(headers, "linear_acceleration_x");
            int idxAy = IndexOf(headers, "linear_acceleration_y");
            int idxAz = IndexOf(headers, "linear_acceleration_z");
            int idxWs = IndexOf(headers, "wind_speed");
            int idxWa = IndexOf(headers, "wind_angle");
            int idxTime = IndexOf(headers, "time");

            if (idxAz < 0 || idxWs < 0)
            {
                _logWriter.WriteLine("[LOG] GRESKA: Nedostaju obavezne kolone.");
                return samples;
            }

            int rowNumber = 1;
            int validCount = 0;

            while (!_streamReader.EndOfStream && validCount < MaxRows)
            {
                rowNumber++;
                string line = _streamReader.ReadLine();

                if (string.IsNullOrWhiteSpace(line))
                {
                    _logWriter.WriteLine($"[LOG] Red {rowNumber}: prazan red, preskocen.");
                    continue;
                }

                string[] parts = line.Split(',');

                if (parts.Length < headers.Length)
                {
                    _logWriter.WriteLine($"[LOG] Red {rowNumber}: NEVALIDAN - premalo kolona. Sadrzaj: {line}");
                    continue;
                }

                if (!TryParseDouble(parts, idxAz, out double az))
                {
                    _logWriter.WriteLine($"[LOG] Red {rowNumber}: NEVALIDAN - LinearAccelerationZ nije broj.");
                    continue;
                }

                if (!TryParseDouble(parts, idxWs, out double ws))
                {
                    _logWriter.WriteLine($"[LOG] Red {rowNumber}: NEVALIDAN - WindSpeed nije broj.");
                    continue;
                }

                if (ws < 0)
                {
                    _logWriter.WriteLine($"[LOG] Red {rowNumber}: NEVALIDAN - WindSpeed je negativan: {ws}");
                    continue;
                }

                TryParseDouble(parts, idxAx, out double ax);
                TryParseDouble(parts, idxAy, out double ay);
                TryParseDouble(parts, idxWa, out double wa);
                string time = (idxTime >= 0 && idxTime < parts.Length) ? parts[idxTime].Trim() : rowNumber.ToString();

                samples.Add(new DroneSampleLocal
                {
                    LinearAccelerationX = ax,
                    LinearAccelerationY = ay,
                    LinearAccelerationZ = az,
                    WindSpeed = ws,
                    WindAngle = wa,
                    Time = time
                });

                validCount++;
            }

            int extraCount = 0;
            while (!_streamReader.EndOfStream)
            {
                _streamReader.ReadLine();
                extraCount++;
            }

            if (extraCount > 0)
                _logWriter.WriteLine($"[LOG] Preskoceno {extraCount} redova viska (limit je {MaxRows}).");

            _logWriter.WriteLine($"[LOG] Ucitano {validCount} validnih uzoraka.");
            return samples;
        }

        private int IndexOf(string[] arr, string name)
        {
            for (int i = 0; i < arr.Length; i++)
                if (arr[i].Trim().Equals(name, StringComparison.OrdinalIgnoreCase))
                    return i;
            return -1;
        }

        private bool TryParseDouble(string[] parts, int idx, out double value)
        {
            value = 0;
            if (idx < 0 || idx >= parts.Length) return false;
            return double.TryParse(parts[idx].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _logWriter?.WriteLine($"[LOG] Zatvaranje resursa u {DateTime.Now}.");
                    _streamReader?.Close();
                    _streamReader?.Dispose();
                    _fileStream?.Dispose();
                    _logWriter?.Dispose();
                    _logStream?.Dispose();
                }
                _disposed = true;
            }
        }

        ~CsvDroneReader()
        {
            Dispose(false);
        }
    }

    public class DroneSampleLocal
    {
        public double LinearAccelerationX { get; set; }
        public double LinearAccelerationY { get; set; }
        public double LinearAccelerationZ { get; set; }
        public double WindSpeed { get; set; }
        public double WindAngle { get; set; }
        public string Time { get; set; }
    }
}