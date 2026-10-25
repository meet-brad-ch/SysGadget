using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace SysGadget.Core
{
    /// <summary>Reads the transfer rates of every physical disk from the "PhysicalDisk" counters, and its space.</summary>
    public sealed class DiskProvider
    {
        private const string ReadCounter = "Disk Read Bytes/sec";
        private const string WriteCounter = "Disk Write Bytes/sec";
        private const string TotalInstance = "_total";

        private const string Category = "PhysicalDisk";

        private readonly CategoryRateReader reader = new CategoryRateReader(Category, ReadCounter, WriteCounter);

        /// <summary>
        /// Lists the physical disks without sampling them, spelled exactly as <see cref="Read"/> names them
        /// (both come from the same counter data).
        /// </summary>
        /// <returns>The disk names, sorted.</returns>
        public static IReadOnlyList<string> InstanceNames()
        {
            List<string> names = new List<string>();
            foreach (InstanceData instance in new PerformanceCounterCategory(Category).ReadCategory()[ReadCounter].Values)
            {
                if (!string.Equals(instance.InstanceName, TotalInstance, StringComparison.OrdinalIgnoreCase))
                {
                    names.Add(instance.InstanceName);
                }
            }

            names.Sort(string.CompareOrdinal);
            return names;
        }

        /// <summary>Samples every disk. Rates cover the time since the previous call.</summary>
        /// <returns>One entry per physical disk, sorted by name.</returns>
        public IReadOnlyList<DiskSample> Read()
        {
            RateSnapshot snapshot = reader.Read();
            IReadOnlyDictionary<string, double> writes = snapshot.Of(WriteCounter);
            List<DiskSample> result = new List<DiskSample>();
            foreach (KeyValuePair<string, double> read in snapshot.Of(ReadCounter))
            {
                if (string.Equals(read.Key, TotalInstance, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                writes.TryGetValue(read.Key, out double writeRate);
                result.Add(new DiskSample(read.Key, read.Value, writeRate, DiskSpace.Of(read.Key)));
            }

            result.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
            return result;
        }
    }
}
