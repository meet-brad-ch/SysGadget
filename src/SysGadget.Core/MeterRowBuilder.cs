using System;
using System.Collections.Generic;
using System.Globalization;

namespace SysGadget.Core
{
    /// <summary>Turns samples into the rows a meter displays.</summary>
    public static class MeterRowBuilder
    {
        private const int NetworkAdaptersShown = 2;

        /// <summary>One bar per logical processor.</summary>
        /// <param name="loads">The per-core samples.</param>
        /// <returns>One row per core, labelled "Core n".</returns>
        public static IReadOnlyList<MeterRow> CpuCores(IReadOnlyList<CoreLoad> loads)
        {
            List<MeterRow> rows = new List<MeterRow>(loads.Count);
            for (int i = 0; i < loads.Count; i++)
            {
                rows.Add(Percent("Core " + i.ToString(CultureInfo.InvariantCulture), loads[i].Percent));
            }

            return rows;
        }

        /// <summary>One bar for the average load of all processors.</summary>
        /// <param name="loads">The per-core samples.</param>
        /// <returns>A single row.</returns>
        public static IReadOnlyList<MeterRow> CpuTotal(IReadOnlyList<CoreLoad> loads)
        {
            double sum = 0.0;
            foreach (CoreLoad load in loads)
            {
                sum += load.Percent;
            }

            return new[] { Percent("CPU", loads.Count == 0 ? 0.0 : sum / loads.Count) };
        }

        /// <summary>Physical memory in use.</summary>
        /// <param name="sample">The memory sample.</param>
        /// <returns>A single row.</returns>
        public static IReadOnlyList<MeterRow> Memory(MemorySample sample)
        {
            return new[]
            {
                new MeterRow("RAM", ByteFormat.Size(sample.UsedBytes) + " / " + ByteFormat.Size(sample.TotalBytes),
                    sample.UsedPercent, true),
            };
        }

        /// <summary>Commit charge.</summary>
        /// <param name="sample">The memory sample.</param>
        /// <returns>A single row.</returns>
        public static IReadOnlyList<MeterRow> Commit(MemorySample sample)
        {
            return new[]
            {
                new MeterRow("Commit", ByteFormat.Size(sample.CommitUsedBytes) + " / " + ByteFormat.Size(sample.CommitLimitBytes),
                    sample.CommitPercent, true),
            };
        }

        /// <summary>GPU load per adapter.</summary>
        /// <param name="samples">The GPU samples.</param>
        /// <returns>One row per adapter.</returns>
        public static IReadOnlyList<MeterRow> GpuLoad(IReadOnlyList<GpuSample> samples)
        {
            List<MeterRow> rows = new List<MeterRow>(samples.Count);
            foreach (GpuSample sample in samples)
            {
                rows.Add(Percent(sample.Adapter.Name, sample.LoadPercent));
            }

            return rows;
        }

        /// <summary>GPU dedicated memory per adapter.</summary>
        /// <param name="samples">The GPU samples.</param>
        /// <returns>One row per adapter.</returns>
        public static IReadOnlyList<MeterRow> GpuMemory(IReadOnlyList<GpuSample> samples)
        {
            List<MeterRow> rows = new List<MeterRow>(samples.Count);
            foreach (GpuSample sample in samples)
            {
                rows.Add(new MeterRow(
                    sample.Adapter.Name,
                    ByteFormat.Size(sample.DedicatedUsedBytes) + " / " + ByteFormat.Size(sample.Adapter.DedicatedBytes),
                    sample.MemoryPercent,
                    true));
            }

            return rows;
        }

        /// <summary>Throughput of the busiest adapters.</summary>
        /// <param name="samples">The network samples, busiest first.</param>
        /// <returns>Up to two rows without bars.</returns>
        public static IReadOnlyList<MeterRow> Network(IReadOnlyList<NetworkSample> samples)
        {
            List<MeterRow> rows = new List<MeterRow>();
            for (int i = 0; i < samples.Count && i < NetworkAdaptersShown; i++)
            {
                rows.Add(new MeterRow(
                    samples[i].AdapterName,
                    "↓ " + ByteFormat.Rate(samples[i].ReceivedBytesPerSecond)
                        + "  ↑ " + ByteFormat.Rate(samples[i].SentBytesPerSecond),
                    0.0,
                    false));
            }

            return rows;
        }

        /// <summary>Busy time per disk, with the rates as text.</summary>
        /// <param name="samples">The disk samples.</param>
        /// <returns>One row per disk.</returns>
        public static IReadOnlyList<MeterRow> Disks(IReadOnlyList<DiskSample> samples)
        {
            List<MeterRow> rows = new List<MeterRow>(samples.Count);
            foreach (DiskSample sample in samples)
            {
                rows.Add(new MeterRow(
                    sample.Name,
                    "R " + ByteFormat.Rate(sample.ReadBytesPerSecond) + "  W " + ByteFormat.Rate(sample.WriteBytesPerSecond),
                    sample.BusyPercent,
                    true));
            }

            return rows;
        }

        /// <summary>Temperatures; the bar shows degrees on a 0 to 100 scale.</summary>
        /// <param name="readings">The sensor readings.</param>
        /// <returns>One row per sensor.</returns>
        public static IReadOnlyList<MeterRow> Temperatures(IReadOnlyList<TemperatureReading> readings)
        {
            List<MeterRow> rows = new List<MeterRow>(readings.Count);
            foreach (TemperatureReading reading in readings)
            {
                rows.Add(new MeterRow(
                    reading.HardwareName + " " + reading.SensorName,
                    reading.Celsius.ToString("0", CultureInfo.InvariantCulture) + " °C",
                    Math.Max(0.0, Math.Min(100.0, reading.Celsius)),
                    true));
            }

            return rows;
        }

        /// <summary>Time of day and uptime.</summary>
        /// <param name="now">The local time.</param>
        /// <param name="uptime">The time since boot.</param>
        /// <returns>Two rows without bars.</returns>
        public static IReadOnlyList<MeterRow> Clock(DateTime now, TimeSpan uptime)
        {
            return new[]
            {
                new MeterRow("Time", now.ToString("ddd HH:mm:ss", CultureInfo.CurrentCulture), 0.0, false),
                new MeterRow("Uptime", ByteFormat.Uptime(uptime), 0.0, false),
            };
        }

        private static MeterRow Percent(string label, double percent)
        {
            return new MeterRow(label, percent.ToString("0", CultureInfo.InvariantCulture) + " %", percent, true);
        }
    }
}
