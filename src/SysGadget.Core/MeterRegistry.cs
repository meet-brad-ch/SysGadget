using System;
using System.Collections.Generic;

namespace SysGadget.Core
{
    /// <summary>
    /// The behaviour behind the <see cref="MeterCatalog"/> ids: how to open each source and how to
    /// turn a source snapshot into the rows of each item. The constructor checks that the catalogue
    /// is fully covered, so a new catalogue entry without behaviour fails at start-up.
    /// </summary>
    public sealed class MeterRegistry
    {
        private readonly Dictionary<string, Func<SampleSource>> sources = new Dictionary<string, Func<SampleSource>>();
        private readonly Dictionary<string, Func<object, GadgetSettings, IReadOnlyList<MeterRow>>> items =
            new Dictionary<string, Func<object, GadgetSettings, IReadOnlyList<MeterRow>>>();

        private readonly Dictionary<string, Func<IReadOnlyList<string>>> instanceLists = new Dictionary<string, Func<IReadOnlyList<string>>>();

        private readonly Func<ITemperatureSource>? temperatureFactory;
        private readonly bool nvidiaInstalled;

        /// <summary>Builds the registry.</summary>
        /// <param name="temperatureFactory">
        /// Opens the temperature source, or null when none is available (no admin rights).
        /// </param>
        /// <param name="nvidiaInstalled">Whether the NVIDIA driver with NVML is installed (GPU temperature).</param>
        /// <exception cref="InvalidOperationException">A catalogue source or item has no behaviour.</exception>
        public MeterRegistry(Func<ITemperatureSource>? temperatureFactory, bool nvidiaInstalled)
        {
            this.temperatureFactory = temperatureFactory;
            this.nvidiaInstalled = nvidiaInstalled;
            AddSources();
            AddItems();
            instanceLists[MeterCatalog.Disks] = DiskProvider.InstanceNames;
            CheckCoverage();
        }

        /// <summary>Lists the instances of every per-instance item on this machine now, without sampling.</summary>
        /// <returns>One reference per instance, for example per disk.</returns>
        public IReadOnlyList<InstanceRef> FindInstances()
        {
            List<InstanceRef> found = new List<InstanceRef>();
            foreach (KeyValuePair<string, Func<IReadOnlyList<string>>> list in instanceLists)
            {
                foreach (string instance in list.Value())
                {
                    found.Add(new InstanceRef(list.Key, instance));
                }
            }

            return found;
        }

        /// <summary>Why a source cannot be opened now, or null when it can.</summary>
        /// <param name="sourceId">The source id.</param>
        /// <param name="settings">The current settings.</param>
        /// <returns>A reason for the user, or null.</returns>
        public string? UnavailableReason(string sourceId, GadgetSettings settings)
        {
            switch (sourceId)
            {
                case MeterCatalog.Sources.Temperature when !settings.UseLibreHardwareMonitor:
                    return "Turn on in Settings";
                case MeterCatalog.Sources.Temperature when temperatureFactory == null:
                    return "Needs admin rights";
                case MeterCatalog.Sources.GpuTemperature when !nvidiaInstalled:
                    return "No NVIDIA driver";
                default:
                    return null;
            }
        }

        /// <summary>Opens a source.</summary>
        /// <param name="sourceId">The source id; check <see cref="UnavailableReason"/> first.</param>
        /// <returns>The open source.</returns>
        public SampleSource Open(string sourceId)
        {
            return sources[sourceId]();
        }

        /// <summary>Turns a snapshot into the rows of one item.</summary>
        /// <param name="itemId">The item id.</param>
        /// <param name="snapshot">The snapshot of the item's source.</param>
        /// <param name="settings">The current settings.</param>
        /// <returns>The rows.</returns>
        public IReadOnlyList<MeterRow> Rows(string itemId, object snapshot, GadgetSettings settings)
        {
            return items[itemId](snapshot, settings);
        }

        private void AddSources()
        {
            sources[MeterCatalog.Sources.Cpu] = () =>
            {
                CpuCounterProvider cpu = new CpuCounterProvider();
                return new SampleSource(context => cpu.Read(), cpu);
            };
            sources[MeterCatalog.Sources.Memory] = () =>
            {
                MemoryProvider memory = new MemoryProvider();
                return new SampleSource(context => memory.Read(), null);
            };
            sources[MeterCatalog.Sources.Gpu] = () =>
            {
                GpuCounterProvider gpu = new GpuCounterProvider();
                return new SampleSource(context => gpu.Read(), null);
            };
            sources[MeterCatalog.Sources.GpuTemperature] = () =>
            {
                NvmlTemperatureProvider nvml = new NvmlTemperatureProvider();
                return new SampleSource(context => nvml.Read(), nvml);
            };
            sources[MeterCatalog.Sources.Disk] = () =>
            {
                DiskProvider disk = new DiskProvider();
                return new SampleSource(context => disk.Read(), null);
            };
            sources[MeterCatalog.Sources.Network] = () =>
            {
                NetworkProvider network = new NetworkProvider();
                return new SampleSource(context => network.Read(), null);
            };
            sources[MeterCatalog.Sources.Temperature] = () =>
            {
                ITemperatureSource temperatures = temperatureFactory!();
                return new SampleSource(context => temperatures.Read(), temperatures);
            };
            sources[MeterCatalog.Sources.Clock] = () => new SampleSource(context => context, null);
        }

        private void AddItems()
        {
            Add<IReadOnlyList<CoreLoad>>(MeterCatalog.CpuTotal, (loads, s) => MeterRowBuilder.CpuTotal(loads));
            Add<IReadOnlyList<CoreLoad>>(MeterCatalog.CpuCores, (loads, s) => MeterRowBuilder.CpuCores(loads, s.UseCompactCores));
            Add<MemorySample>(MeterCatalog.Ram, (memory, s) => MeterRowBuilder.Memory(memory));
            Add<MemorySample>(MeterCatalog.Commit, (memory, s) => MeterRowBuilder.Commit(memory));
            Add<IReadOnlyList<GpuSample>>(MeterCatalog.GpuLoad, (gpus, s) => MeterRowBuilder.GpuLoad(gpus));
            Add<IReadOnlyList<GpuSample>>(MeterCatalog.GpuMemory, (gpus, s) => MeterRowBuilder.GpuMemory(gpus));
            Add<IReadOnlyList<TemperatureReading>>(MeterCatalog.GpuTemperature, (readings, s) => MeterRowBuilder.GpuTemperature(readings));
            Add<IReadOnlyList<DiskSample>>(MeterCatalog.Disks, (disks, s) => MeterRowBuilder.Disks(disks));
            Add<IReadOnlyList<NetworkSample>>(MeterCatalog.NetworkRates, (network, s) => MeterRowBuilder.Network(network, s.NetworkMbit));
            Add<IReadOnlyList<TemperatureReading>>(MeterCatalog.Temperatures, (readings, s) => MeterRowBuilder.Temperatures(readings));
            Add<SampleContext>(MeterCatalog.Time, (context, s) => MeterRowBuilder.Time(context.Now));
            Add<SampleContext>(MeterCatalog.Uptime, (context, s) => MeterRowBuilder.Uptime(context.Uptime));
        }

        private void Add<TSnapshot>(string itemId, Func<TSnapshot, GadgetSettings, IReadOnlyList<MeterRow>> rows)
        {
            items[itemId] = (snapshot, settings) => rows((TSnapshot)snapshot, settings);
        }

        private void CheckCoverage()
        {
            foreach (string sourceId in MeterCatalog.Sources.All)
            {
                if (!sources.ContainsKey(sourceId))
                {
                    throw new InvalidOperationException("Source '" + sourceId + "' has no factory.");
                }
            }

            foreach (GroupDefinition group in MeterCatalog.Groups)
            {
                foreach (ItemDefinition item in group.Items)
                {
                    if (!items.ContainsKey(item.Id) || !sources.ContainsKey(item.SourceId))
                    {
                        throw new InvalidOperationException("Item '" + item.Id + "' has no row function or no source.");
                    }

                    if (item.PerInstance != instanceLists.ContainsKey(item.Id))
                    {
                        throw new InvalidOperationException("Item '" + item.Id + "' and its instance list do not match.");
                    }
                }
            }
        }
    }
}
