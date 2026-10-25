using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace SysGadget.Core
{
    /// <summary>
    /// Reads GPU load and dedicated memory per adapter from the "GPU Engine" and
    /// "GPU Adapter Memory" counter sets. Load is the busiest engine type of the
    /// adapter (3D, copy, video, ...), summed over all processes, as Task Manager does.
    /// </summary>
    public sealed class GpuCounterProvider
    {
        private const string EngineCategory = "GPU Engine";
        private const string MemoryCategory = "GPU Adapter Memory";
        private const string UtilizationCounter = "Utilization Percentage";
        private const string DedicatedCounter = "Dedicated Usage";

        private readonly IReadOnlyList<GpuAdapterInfo> adapters;
        private readonly CategoryRateReader engines;
        private readonly PerformanceCounterCategory memory;

        /// <summary>Lists the adapters and takes the baseline sample.</summary>
        /// <exception cref="InvalidOperationException">The counter sets or adapters are missing.</exception>
        public GpuCounterProvider()
        {
            if (!PerformanceCounterCategory.Exists(MemoryCategory))
            {
                throw new InvalidOperationException("Performance counter set '" + MemoryCategory + "' does not exist.");
            }

            adapters = new DxgiAdapterEnumerator().List();
            if (adapters.Count == 0)
            {
                throw new InvalidOperationException("DXGI lists no hardware graphics adapter.");
            }

            memory = new PerformanceCounterCategory(MemoryCategory);
            engines = new CategoryRateReader(EngineCategory, UtilizationCounter);
        }

        /// <summary>The hardware adapters being read.</summary>
        public IReadOnlyList<GpuAdapterInfo> Adapters => adapters;

        /// <summary>Samples every adapter. Load covers the time since the previous call.</summary>
        /// <returns>One entry per adapter, in the order of <see cref="Adapters"/>.</returns>
        public IReadOnlyList<GpuSample> Read()
        {
            Dictionary<string, Dictionary<string, double>> byAdapter = new Dictionary<string, Dictionary<string, double>>();
            foreach (KeyValuePair<string, double> engine in engines.Read().Of(UtilizationCounter))
            {
                GpuInstanceName name = GpuInstanceName.ParseEngine(engine.Key);
                if (!byAdapter.TryGetValue(name.Luid, out Dictionary<string, double>? perType))
                {
                    perType = new Dictionary<string, double>();
                    byAdapter[name.Luid] = perType;
                }

                perType.TryGetValue(name.EngineType, out double sum);
                perType[name.EngineType] = sum + engine.Value;
            }

            Dictionary<string, ulong> used = ReadDedicatedUsage();
            List<GpuSample> result = new List<GpuSample>(adapters.Count);
            foreach (GpuAdapterInfo adapter in adapters)
            {
                double load = 0.0;
                if (byAdapter.TryGetValue(adapter.Luid, out Dictionary<string, double>? perType))
                {
                    foreach (double sum in perType.Values)
                    {
                        load = Math.Max(load, sum);
                    }
                }

                used.TryGetValue(adapter.Luid, out ulong usedBytes);
                result.Add(new GpuSample(adapter, Math.Min(100.0, load), usedBytes));
            }

            return result;
        }

        private Dictionary<string, ulong> ReadDedicatedUsage()
        {
            Dictionary<string, ulong> used = new Dictionary<string, ulong>();
            InstanceDataCollection data = memory.ReadCategory()[DedicatedCounter];
            foreach (InstanceData instance in data.Values)
            {
                string luid = GpuInstanceName.ParseAdapter(instance.InstanceName).Luid;
                used.TryGetValue(luid, out ulong sum);
                used[luid] = sum + (ulong)instance.RawValue;
            }

            return used;
        }
    }
}
