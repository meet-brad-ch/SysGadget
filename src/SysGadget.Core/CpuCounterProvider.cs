using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace SysGadget.Core
{
    /// <summary>
    /// Reads the load of every logical processor from the Windows "Processor Information"
    /// counter set. No admin rights and no driver are needed.
    /// </summary>
    public sealed class CpuCounterProvider : IDisposable
    {
        private const string Category = "Processor Information";
        private const string CounterName = "% Processor Utility";

        private readonly List<PerformanceCounter> counters = new List<PerformanceCounter>();
        private readonly List<ProcessorInstanceName> names = new List<ProcessorInstanceName>();

        /// <summary>Opens one counter per logical processor.</summary>
        /// <exception cref="InvalidOperationException">The counter set is missing or has no cores.</exception>
        public CpuCounterProvider()
        {
            if (!PerformanceCounterCategory.Exists(Category))
            {
                throw new InvalidOperationException("Performance counter set '" + Category + "' does not exist.");
            }

            foreach (string instance in new PerformanceCounterCategory(Category).GetInstanceNames())
            {
                ProcessorInstanceName? name = ProcessorInstanceName.ParseCore(instance);
                if (name == null)
                {
                    continue;
                }

                names.Add(name);
                counters.Add(new PerformanceCounter(Category, CounterName, instance, true));
            }

            if (counters.Count == 0)
            {
                throw new InvalidOperationException("Counter set '" + Category + "' lists no processor instances.");
            }

            // A rate counter needs a first sample as its baseline.
            foreach (PerformanceCounter counter in counters)
            {
                counter.NextValue();
            }
        }

        /// <summary>Number of logical processors being read.</summary>
        public int CoreCount => counters.Count;

        /// <summary>Samples every core. Call at most once per second or so.</summary>
        /// <returns>One entry per logical processor, load clamped to 0..100 like Task Manager does.</returns>
        public IReadOnlyList<CoreLoad> Read()
        {
            List<CoreLoad> result = new List<CoreLoad>(counters.Count);
            for (int i = 0; i < counters.Count; i++)
            {
                double percent = Math.Max(0.0, Math.Min(100.0, counters[i].NextValue()));
                result.Add(new CoreLoad(names[i].Group, names[i].Index, percent));
            }

            return result;
        }

        /// <summary>Releases the counters.</summary>
        public void Dispose()
        {
            foreach (PerformanceCounter counter in counters)
            {
                counter.Dispose();
            }

            counters.Clear();
        }
    }
}
