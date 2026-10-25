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

        private static readonly string[] LimitSensorNames = { "Warning Temperature", "Critical Temperature" };

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

        /// <summary>GPU core temperature per GPU; the bar shows degrees on a 0 to 100 scale.</summary>
        /// <param name="readings">One reading per GPU, the hardware name being the GPU name.</param>
        /// <returns>One row per GPU: "Temp" with one GPU, the short GPU name with more.</returns>
        public static IReadOnlyList<MeterRow> GpuTemperature(IReadOnlyList<TemperatureReading> readings)
        {
            List<MeterRow> rows = new List<MeterRow>(readings.Count);
            foreach (TemperatureReading reading in readings)
            {
                rows.Add(new MeterRow(
                    readings.Count == 1 ? "Temp" : ShortDeviceName(reading.HardwareName),
                    Degrees(reading.Celsius),
                    Math.Max(0.0, Math.Min(100.0, reading.Celsius)),
                    true));
            }

            return rows;
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

        /// <summary>Busy time per disk, with the rates on a second line.</summary>
        /// <param name="samples">The disk samples.</param>
        /// <returns>
        /// Two rows per disk, both tagged with the disk as their instance: the busy bar, then the read
        /// and write rates without a bar.
        /// </returns>
        public static IReadOnlyList<MeterRow> Disks(IReadOnlyList<DiskSample> samples)
        {
            List<MeterRow> rows = new List<MeterRow>(samples.Count * 2);
            foreach (DiskSample sample in samples)
            {
                rows.Add(new MeterRow(sample.Name, PercentText(sample.BusyPercent), sample.BusyPercent, true, false, sample.Name));
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
        /// Temperatures; the bar shows degrees on a 0 to 100 scale. Alarm limits that drives report as
        /// sensors ("Warning Temperature", "Critical Temperature") are fixed values, not readings, and
        /// are left out.
        /// </summary>
        /// <param name="readings">The sensor readings.</param>
        /// <returns>One row per reading, labelled "device sensor", for example "990 PRO Composite".</returns>
        public static IReadOnlyList<MeterRow> Temperatures(IReadOnlyList<TemperatureReading> readings)
        {
            List<MeterRow> rows = new List<MeterRow>(readings.Count);
            foreach (TemperatureReading reading in readings)
            {
                if (Array.IndexOf(LimitSensorNames, reading.SensorName) >= 0)
                {
                    continue;
                }

                rows.Add(new MeterRow(
                    ShortDeviceName(reading.HardwareName) + " " + ShortSensorName(reading.SensorName),
                    Degrees(reading.Celsius),
                    Math.Max(0.0, Math.Min(100.0, reading.Celsius)),
                    true));
            }

            return rows;
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
                if (!VendorWords.Contains(word, StringComparer.OrdinalIgnoreCase) && !Capacity.IsMatch(word) && kept.Count < MaxDeviceWords)
                {
                    kept.Add(word);
                }
            }

            return kept.Count == 0 ? name : string.Join(" ", kept);
        }

        /// <summary>Drops a leading "GPU" or "CPU" from a sensor name; the device name already says it.</summary>
        private static string ShortSensorName(string sensor)
        {
            foreach (string prefix in new[] { "GPU ", "CPU " })
            {
                if (sensor.StartsWith(prefix, StringComparison.Ordinal) && sensor.Length > prefix.Length)
                {
                    return sensor.Substring(prefix.Length);
                }
            }

            return sensor;
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
