using System;
using System.Collections.Generic;
using System.Globalization;

namespace SysGadget.Core
{
    /// <summary>Turns source snapshots into the rows an item displays.</summary>
    public static class MeterRowBuilder
    {
        private static readonly string[] VendorPrefixes = { "NVIDIA ", "GeForce ", "AMD ", "Radeon ", "Intel(R) " };

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
                    readings.Count == 1 ? "Temp" : ShortGpuName(reading.HardwareName),
                    Degrees(reading.Celsius),
                    Math.Max(0.0, Math.Min(100.0, reading.Celsius)),
                    true));
            }

            return rows;
        }

        /// <summary>Drops the vendor words so the name fits a narrow label, for example "RTX 4090".</summary>
        /// <param name="name">The adapter name as DXGI reports it.</param>
        /// <returns>The shortened name; the full name when nothing is left after shortening.</returns>
        public static string ShortGpuName(string name)
        {
            string shortened = name;
            bool changed = true;
            while (changed)
            {
                changed = false;
                foreach (string prefix in VendorPrefixes)
                {
                    if (shortened.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    {
                        shortened = shortened.Substring(prefix.Length);
                        changed = true;
                    }
                }
            }

            return shortened.Length == 0 ? name : shortened;
        }

        /// <summary>Throughput of the busiest adapter.</summary>
        /// <param name="samples">The network samples, busiest first.</param>
        /// <returns>"Down" and "Up" rows without bars; none when there is no adapter.</returns>
        public static IReadOnlyList<MeterRow> Network(IReadOnlyList<NetworkSample> samples)
        {
            if (samples.Count == 0)
            {
                return new MeterRow[0];
            }

            return new[]
            {
                new MeterRow("Down", ByteFormat.Rate(samples[0].ReceivedBytesPerSecond), 0.0, false),
                new MeterRow("Up", ByteFormat.Rate(samples[0].SentBytesPerSecond), 0.0, false),
            };
        }

        /// <summary>Busy time per disk, with the rates on a second line.</summary>
        /// <param name="samples">The disk samples.</param>
        /// <returns>Two rows per disk: the busy bar, then the read and write rates without a bar.</returns>
        public static IReadOnlyList<MeterRow> Disks(IReadOnlyList<DiskSample> samples)
        {
            List<MeterRow> rows = new List<MeterRow>(samples.Count * 2);
            foreach (DiskSample sample in samples)
            {
                rows.Add(Percent(sample.Name, sample.BusyPercent));
                rows.Add(new MeterRow(
                    string.Empty,
                    "R " + ByteFormat.Rate(sample.ReadBytesPerSecond) + "  W " + ByteFormat.Rate(sample.WriteBytesPerSecond),
                    0.0,
                    false));
            }

            return rows;
        }

        /// <summary>Temperatures; the bar shows degrees on a 0 to 100 scale.</summary>
        /// <param name="readings">The sensor readings.</param>
        /// <returns>One row per sensor, labelled with the sensor name.</returns>
        public static IReadOnlyList<MeterRow> Temperatures(IReadOnlyList<TemperatureReading> readings)
        {
            List<MeterRow> rows = new List<MeterRow>(readings.Count);
            foreach (TemperatureReading reading in readings)
            {
                rows.Add(new MeterRow(
                    reading.SensorName,
                    Degrees(reading.Celsius),
                    Math.Max(0.0, Math.Min(100.0, reading.Celsius)),
                    true));
            }

            return rows;
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
            return samples.Count == 1 ? single : ShortGpuName(sample.Adapter.Name);
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
