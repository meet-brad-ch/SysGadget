using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace SysGadget.Core
{
    /// <summary>Reads activity of every physical disk from the "PhysicalDisk" counters.</summary>
    public sealed class DiskProvider
    {
        private const string BusyCounter = "% Disk Time";
        private const string ReadCounter = "Disk Read Bytes/sec";
        private const string WriteCounter = "Disk Write Bytes/sec";
        private const string TotalInstance = "_total";

        private const string Category = "PhysicalDisk";

        private readonly CategoryRateReader reader = new CategoryRateReader(Category, BusyCounter, ReadCounter, WriteCounter);

        /// <summary>
        /// Lists the physical disks without sampling them, spelled exactly as <see cref="Read"/> names them
        /// (both come from the same counter data).
        /// </summary>
        /// <returns>The disk names, sorted.</returns>
        public static IReadOnlyList<string> InstanceNames()
        {
            List<string> names = new List<string>();
            foreach (InstanceData instance in new PerformanceCounterCategory(Category).ReadCategory()[BusyCounter].Values)
            {
                if (!string.Equals(instance.InstanceName, TotalInstance, StringComparison.OrdinalIgnoreCase))
                {
                    names.Add(instance.InstanceName);
                }
            }

            names.Sort(string.CompareOrdinal);
            return names;
        }

        /// <summary>Samples every disk. Values cover the time since the previous call.</summary>
        /// <returns>One entry per physical disk, sorted by name.</returns>
        public IReadOnlyList<DiskSample> Read()
        {
            RateSnapshot snapshot = reader.Read();
            IReadOnlyDictionary<string, double> reads = snapshot.Of(ReadCounter);
            IReadOnlyDictionary<string, double> writes = snapshot.Of(WriteCounter);
            List<DiskSample> result = new List<DiskSample>();
            foreach (KeyValuePair<string, double> busy in snapshot.Of(BusyCounter))
            {
                if (string.Equals(busy.Key, TotalInstance, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                reads.TryGetValue(busy.Key, out double readRate);
                writes.TryGetValue(busy.Key, out double writeRate);
                result.Add(new DiskSample(busy.Key, Math.Max(0.0, Math.Min(100.0, busy.Value)), readRate, writeRate));
            }

            result.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
            return result;
        }
    }
}
