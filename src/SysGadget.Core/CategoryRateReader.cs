using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace SysGadget.Core
{
    /// <summary>
    /// Reads selected counters of a whole counter set with one call and turns each
    /// instance into a value over the time since the previous call. Instances may come
    /// and go between calls; an instance without a previous sample is left out.
    /// </summary>
    public sealed class CategoryRateReader
    {
        private readonly PerformanceCounterCategory category;
        private readonly string[] counters;
        private Dictionary<string, Dictionary<string, CounterSample>> previous;

        /// <summary>Opens the counter set and takes the baseline sample.</summary>
        /// <param name="categoryName">Counter set name, for example "PhysicalDisk".</param>
        /// <param name="counterNames">Counters to read, for example "Disk Read Bytes/sec".</param>
        /// <exception cref="InvalidOperationException">The counter set does not exist.</exception>
        public CategoryRateReader(string categoryName, params string[] counterNames)
        {
            if (!PerformanceCounterCategory.Exists(categoryName))
            {
                throw new InvalidOperationException("Performance counter set '" + categoryName + "' does not exist.");
            }

            category = new PerformanceCounterCategory(categoryName);
            counters = counterNames;
            previous = Capture();
        }

        /// <summary>Samples the counters and computes the values since the previous call.</summary>
        /// <returns>The values per counter and instance.</returns>
        public RateSnapshot Read()
        {
            Dictionary<string, Dictionary<string, CounterSample>> current = Capture();
            Dictionary<string, Dictionary<string, double>> values = new Dictionary<string, Dictionary<string, double>>();
            foreach (string counter in counters)
            {
                Dictionary<string, double> perInstance = new Dictionary<string, double>();
                Dictionary<string, CounterSample> before = previous[counter];
                foreach (KeyValuePair<string, CounterSample> pair in current[counter])
                {
                    if (before.TryGetValue(pair.Key, out CounterSample start))
                    {
                        perInstance[pair.Key] = CounterSample.Calculate(start, pair.Value);
                    }
                }

                values[counter] = perInstance;
            }

            previous = current;
            return new RateSnapshot(values);
        }

        private Dictionary<string, Dictionary<string, CounterSample>> Capture()
        {
            InstanceDataCollectionCollection data = category.ReadCategory();
            Dictionary<string, Dictionary<string, CounterSample>> result = new Dictionary<string, Dictionary<string, CounterSample>>();
            foreach (string counter in counters)
            {
                Dictionary<string, CounterSample> perInstance = new Dictionary<string, CounterSample>();
                foreach (InstanceData instance in data[counter].Values)
                {
                    perInstance[instance.InstanceName] = instance.Sample;
                }

                result[counter] = perInstance;
            }

            return result;
        }
    }
}
