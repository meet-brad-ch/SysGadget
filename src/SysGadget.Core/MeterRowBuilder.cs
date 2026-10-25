using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace SysGadget.Core
{
    /// <summary>Turns source snapshots into the rows an item displays.</summary>
    public static class MeterRowBuilder
    {
        private const int MaxDeviceWords = 3;

        private static readonly string[] VendorWords =
        {
            "NVIDIA", "GeForce", "AMD", "Radeon", "Intel(R)", "Intel", "Samsung", "SSD", "NVMe", "WDC", "WD",
            "Western", "Digital", "Crucial", "Kingston", "Seagate", "Corsair", "SK", "hynix",
        };

        private static readonly string[] SensorNoiseWords = { "Temperature", "Junction" };

        private const string WarningLimitSensor = "Warning Temperature";
        private const string CriticalLimitSensor = "Critical Temperature";
        private const double DefaultWarningCelsius = 75.0;
        private const double DefaultCriticalCelsius = 90.0;
        private const double ApproachCelsius = 10.0;

        private static readonly Regex Capacity = new Regex(@"^\d+(\.\d+)?(TB|GB)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        /// <summary>One bar per logical processor.</summary>
        /// <param name="loads">The per-core samples.</param>
        /// <param name="compact">True marks the rows as cells of the compact core strip.</param>
        /// <returns>One row per core, labelled "Core n".</returns>
        public static IReadOnlyList<MeterRow> CpuCores(IReadOnlyList<CoreLoad> loads, bool compact)
        {
            List<MeterRow> rows = new List<MeterRow>(loads.Count);
            for (int i = 0; i < loads.Count; i++)
            {
                double percent = loads[i].Percent;
                rows.Add(new MeterRow("Core " + i.ToString(CultureInfo.InvariantCulture), PercentText(percent), percent, true, compact));
            }

            return rows;
        }

        /// <summary>One bar for the average load of all processors.</summary>
        /// <param name="loads">The per-core samples.</param>
        /// <returns>A single row labelled "Total".</returns>
        public static IReadOnlyList<MeterRow> CpuTotal(IReadOnlyList<CoreLoad> loads)
        {
            double sum = 0.0;
            foreach (CoreLoad load in loads)
            {
                sum += load.Percent;
            }

            return new[] { Percent("Total", loads.Count == 0 ? 0.0 : sum / loads.Count) };
        }

        /// <summary>Physical memory in use.</summary>
        /// <param name="sample">The memory sample.</param>
        /// <returns>A single row.</returns>
        public static IReadOnlyList<MeterRow> Memory(MemorySample sample)
        {
            return new[]
            {
                new MeterRow("RAM", ByteFormat.Pair(sample.UsedBytes, sample.TotalBytes), sample.UsedPercent, true),
            };
        }

        /// <summary>Commit charge.</summary>
        /// <param name="sample">The memory sample.</param>
        /// <returns>A single row.</returns>
        public static IReadOnlyList<MeterRow> Commit(MemorySample sample)
        {
            return new[]
            {
                new MeterRow("Commit", ByteFormat.Pair(sample.CommitUsedBytes, sample.CommitLimitBytes),
                    sample.CommitPercent, true),
            };
        }

        /// <summary>GPU load per adapter.</summary>
        /// <param name="samples">The GPU samples.</param>
        /// <returns>One row per adapter: "Load" with one adapter, the short adapter name with more.</returns>
        public static IReadOnlyList<MeterRow> GpuLoad(IReadOnlyList<GpuSample> samples)
        {
            List<MeterRow> rows = new List<MeterRow>(samples.Count);
            foreach (GpuSample sample in samples)
            {
                rows.Add(Percent(GpuLabel(samples, sample, "Load"), sample.LoadPercent));
            }

            return rows;
        }

        /// <summary>GPU dedicated memory per adapter.</summary>
        /// <param name="samples">The GPU samples.</param>
        /// <returns>One row per adapter: "VRAM" with one adapter, the short adapter name with more.</returns>
        public static IReadOnlyList<MeterRow> GpuMemory(IReadOnlyList<GpuSample> samples)
        {
            List<MeterRow> rows = new List<MeterRow>(samples.Count);
            foreach (GpuSample sample in samples)
            {
                rows.Add(new MeterRow(
                    GpuLabel(samples, sample, "VRAM"),
                    ByteFormat.Pair(sample.DedicatedUsedBytes, sample.Adapter.DedicatedBytes),
                    sample.MemoryPercent,
                    true));
            }

            return rows;
        }

        /// <summary>GPU core temperature per GPU, with its peak, coloured against the GPU's own limits.</summary>
        /// <param name="readings">One reading per GPU, the hardware name being the GPU name.</param>
        /// <returns>One row per GPU without a bar: "Temp" with one GPU, the short GPU name with more.</returns>
        public static IReadOnlyList<MeterRow> GpuTemperature(IReadOnlyList<TemperatureReading> readings)
        {
            List<MeterRow> rows = new List<MeterRow>(readings.Count);
            foreach (TemperatureReading reading in readings)
            {
                string label = readings.Count == 1 ? "Temp" : ShortDeviceName(reading.HardwareName);
                rows.Add(TemperatureRow(label, reading, reading.WarningCelsius, reading.CriticalCelsius));
            }

            return rows;
        }

        /// <summary>
        /// How alarming a temperature is. With device limits: amber from 10 °C below the warning limit,
        /// red at the critical limit (or at the warning limit when only that is known). Without limits:
        /// amber from 75 °C, red from 90 °C.
        /// </summary>
        /// <param name="celsius">The temperature.</param>
        /// <param name="warning">The device's warning limit, or null.</param>
        /// <param name="critical">The device's critical limit, or null.</param>
        /// <returns>The alert level.</returns>
        public static AlertLevel TemperatureAlert(double celsius, double? warning, double? critical)
        {
            if (warning == null && critical == null)
            {
                return celsius >= DefaultCriticalCelsius ? AlertLevel.Critical
                    : celsius >= DefaultWarningCelsius ? AlertLevel.Warning
                    : AlertLevel.Normal;
            }

            double red = critical ?? warning!.Value;
            double amberFrom = (warning ?? critical!.Value) - ApproachCelsius;
            return celsius >= red ? AlertLevel.Critical
                : celsius >= amberFrom ? AlertLevel.Warning
                : AlertLevel.Normal;
        }

        private static MeterRow TemperatureRow(string label, TemperatureReading reading, double? warning, double? critical)
        {
            return new MeterRow(
                label,
                Degrees(reading.Celsius),
                0.0,
                false,
                false,
                string.Empty,
                TemperatureAlert(reading.Celsius, warning, critical));
        }

        /// <summary>Throughput of the busiest adapter.</summary>
        /// <param name="samples">The network samples, busiest first.</param>
        /// <param name="bandwidthMbit">The connection bandwidth in Mbit/s: a full bar.</param>
        /// <returns>"Down" and "Up" rows whose bars show the share of the bandwidth, full above it; none without an adapter.</returns>
        public static IReadOnlyList<MeterRow> Network(IReadOnlyList<NetworkSample> samples, int bandwidthMbit)
        {
            if (samples.Count == 0)
            {
                return new MeterRow[0];
            }

            return new[]
            {
                RateRow("Down", samples[0].ReceivedBytesPerSecond, bandwidthMbit),
                RateRow("Up", samples[0].SentBytesPerSecond, bandwidthMbit),
            };
        }

        private static MeterRow RateRow(string label, double bytesPerSecond, int bandwidthMbit)
        {
            double percent = Math.Min(100.0, bytesPerSecond * 8.0 / (bandwidthMbit * 1000000.0) * 100.0);
            return new MeterRow(label, ByteFormat.Rate(bytesPerSecond), percent, true);
        }

        /// <summary>Space, busy time and rates per disk.</summary>
        /// <param name="samples">The disk samples.</param>
        /// <returns>
        /// Per disk, all tagged with the disk as their instance: the used space ("412 / 931 GB", bar =
        /// share used) under the disk's name, then "Active" (busy bar), then the read and write rates
        /// without a bar. A disk without a drive letter has no space row; its busy row carries the name.
        /// </returns>
        public static IReadOnlyList<MeterRow> Disks(IReadOnlyList<DiskSample> samples)
        {
            List<MeterRow> rows = new List<MeterRow>(samples.Count * 3);
            foreach (DiskSample sample in samples)
            {
                if (sample.Space != null)
                {
                    rows.Add(new MeterRow(
                        sample.Name,
                        ByteFormat.Pair(sample.Space.UsedBytes, sample.Space.TotalBytes),
                        sample.Space.UsedPercent,
                        true,
                        false,
                        sample.Name));
                }

                string busyLabel = sample.Space != null ? "Active" : sample.Name;
                rows.Add(new MeterRow(busyLabel, PercentText(sample.BusyPercent), sample.BusyPercent, true, false, sample.Name));
                rows.Add(new MeterRow(
                    string.Empty,
                    "R " + ByteFormat.Rate(sample.ReadBytesPerSecond) + "  W " + ByteFormat.Rate(sample.WriteBytesPerSecond),
                    0.0,
                    false,
                    false,
                    sample.Name));
            }

            return rows;
        }

        /// <summary>
        /// Temperatures with their peaks, no bars. Drives report their alarm limits as sensors
        /// ("Warning Temperature", "Critical Temperature"); those are not shown as rows but used as the
        /// limits of the other sensors of the same device.
        /// </summary>
        /// <param name="readings">The sensor readings.</param>
        /// <returns>One row per reading, labelled "device sensor", for example "990 PRO Composite".</returns>
        public static IReadOnlyList<MeterRow> Temperatures(IReadOnlyList<TemperatureReading> readings)
        {
            List<MeterRow> rows = new List<MeterRow>(readings.Count);
            foreach (TemperatureReading reading in readings)
            {
                if (reading.SensorName == WarningLimitSensor || reading.SensorName == CriticalLimitSensor)
                {
                    continue;
                }

                double? warning = reading.WarningCelsius ?? LimitOf(readings, reading.HardwareName, WarningLimitSensor);
                double? critical = reading.CriticalCelsius ?? LimitOf(readings, reading.HardwareName, CriticalLimitSensor);
                string sensor = ShortSensorName(reading.SensorName);
                string label = ShortDeviceName(reading.HardwareName) + (sensor.Length > 0 ? " " + sensor : string.Empty);
                rows.Add(TemperatureRow(label, reading, warning, critical));
            }

            return rows;
        }

        private static double? LimitOf(IReadOnlyList<TemperatureReading> readings, string hardwareName, string limitSensor)
        {
            TemperatureReading? limit = readings.FirstOrDefault(r => r.HardwareName == hardwareName && r.SensorName == limitSensor);
            return limit?.Celsius;
        }

        /// <summary>
        /// Shortens a device name for a narrow label: vendor words and a capacity ("2TB") are dropped
        /// and at most three words are kept, for example "Samsung SSD 990 PRO 2TB" gives "990 PRO".
        /// </summary>
        /// <param name="name">The device name as Windows or LibreHardwareMonitor reports it.</param>
        /// <returns>The short name; the full name when nothing is left.</returns>
        public static string ShortDeviceName(string name)
        {
            List<string> kept = new List<string>();
            foreach (string word in name.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (string.Equals(word, "with", StringComparison.OrdinalIgnoreCase))
                {
                    break; // "990 PRO with Heatsink": the rest describes the variant, not the device
                }

                if (!VendorWords.Contains(word, StringComparer.OrdinalIgnoreCase) && !Capacity.IsMatch(word) && kept.Count < MaxDeviceWords)
                {
                    kept.Add(word);
                }
            }

            return kept.Count == 0 ? name : string.Join(" ", kept);
        }

        /// <summary>
        /// Shortens a sensor name for a row in the temperature card: drops a leading "GPU"/"CPU" (the
        /// device name says it), the word "Temperature" (the card says it) and "Junction".
        /// </summary>
        /// <param name="sensor">The sensor name as LibreHardwareMonitor reports it.</param>
        /// <returns>The short name; empty when nothing is left ("Temperature" alone).</returns>
        public static string ShortSensorName(string sensor)
        {
            List<string> kept = new List<string>();
            string[] words = sensor.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < words.Length; i++)
            {
                bool devicePrefix = i == 0 && words.Length > 1 && (words[i] == "GPU" || words[i] == "CPU");
                if (!devicePrefix && !SensorNoiseWords.Contains(words[i], StringComparer.OrdinalIgnoreCase))
                {
                    kept.Add(words[i]);
                }
            }

            return string.Join(" ", kept);
        }

        /// <summary>Time of day.</summary>
        /// <param name="now">The local time.</param>
        /// <returns>One row without a bar.</returns>
        public static IReadOnlyList<MeterRow> Time(DateTime now)
        {
            return new[] { new MeterRow("Time", now.ToString("ddd HH:mm:ss", CultureInfo.CurrentCulture), 0.0, false) };
        }

        /// <summary>Time since boot.</summary>
        /// <param name="uptime">The uptime.</param>
        /// <returns>One row without a bar.</returns>
        public static IReadOnlyList<MeterRow> Uptime(TimeSpan uptime)
        {
            return new[] { new MeterRow("Uptime", ByteFormat.Uptime(uptime), 0.0, false) };
        }

        private static string GpuLabel(IReadOnlyList<GpuSample> samples, GpuSample sample, string single)
        {
            return samples.Count == 1 ? single : ShortDeviceName(sample.Adapter.Name);
        }

        private static MeterRow Percent(string label, double percent)
        {
            return new MeterRow(label, PercentText(percent), percent, true);
        }

        private static string Degrees(double celsius)
        {
            return celsius.ToString("0", CultureInfo.InvariantCulture) + " °C";
        }

        private static string PercentText(double percent)
        {
            return percent.ToString("0", CultureInfo.InvariantCulture) + " %";
        }
    }
}
