using System;
using System.Collections.Generic;
using System.Linq;

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

        private readonly MachineSources machine;

        /// <summary>Builds the registry.</summary>
        /// <param name="machine">The hardware sources: <see cref="MachineSources.Real"/>, or fixed ones in tests.</param>
        /// <exception cref="InvalidOperationException">A catalogue source or item has no behaviour.</exception>
        public MeterRegistry(MachineSources machine)
        {
            this.machine = machine;
            AddSources();
            AddItems();
            instanceLists[MeterCatalog.DiskRead] = machine.DiskNames;
            instanceLists[MeterCatalog.DiskWrite] = machine.DiskNames;
            instanceLists[MeterCatalog.DiskFree] = machine.DiskNames;
            CheckCoverage();
        }

        /// <summary>Lists the instances of every per-instance item on this machine now, without sampling.</summary>
        /// <returns>
        /// One reference per instance and item, by instance first and then in catalogue order, so new
        /// entries arrive as "disk 0: read, write, free; disk 1: read, write, free".
        /// </returns>
        public IReadOnlyList<InstanceRef> FindInstances()
        {
            Dictionary<string, IReadOnlyList<string>> listed = new Dictionary<string, IReadOnlyList<string>>();
            List<string> instances = new List<string>();
            foreach (KeyValuePair<string, Func<IReadOnlyList<string>>> list in instanceLists)
            {
                listed[list.Key] = list.Value();
                instances.AddRange(listed[list.Key].Where(i => !instances.Contains(i)));
            }

            List<InstanceRef> found = new List<InstanceRef>();
            foreach (string instance in instances)
            {
                foreach (ItemDefinition item in MeterCatalog.Groups.SelectMany(g => g.Items).Where(i => i.PerInstance))
                {
                    if (listed[item.Id].Contains(instance))
                    {
                        found.Add(new InstanceRef(item.Id, instance));
                    }
                }
            }

            return found;
        }

        /// <summary>Why a source cannot be opened now, or null when it can.</summary>
        /// <param name="sourceId">The source id.</param>
        /// <returns>A reason for the user, or null.</returns>
        public string? UnavailableReason(string sourceId)
        {
            switch (sourceId)
            {
                case MeterCatalog.Sources.Temperature when machine.Temperatures == null:
                    return "Needs admin rights";
                case MeterCatalog.Sources.GpuTemperature when machine.GpuTemperature == null:
                    return "No NVIDIA driver";
                default:
                    return null;
            }
        }

        /// <summary>Opens a source.</summary>
        /// <param name="sourceId">The source id; check <see cref="UnavailableReason"/> first.</param>
        /// <returns>The open source.</returns>
        /// <exception cref="InvalidOperationException">The source is unavailable.</exception>
        public SampleSource Open(string sourceId)
        {
            string? reason = UnavailableReason(sourceId);
            if (reason != null)
            {
                throw new InvalidOperationException("Source '" + sourceId + "' is unavailable: " + reason);
            }

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
            sources[MeterCatalog.Sources.Cpu] = machine.Cpu;
            sources[MeterCatalog.Sources.Memory] = machine.Memory;
            sources[MeterCatalog.Sources.Gpu] = machine.Gpu;
            sources[MeterCatalog.Sources.GpuTemperature] = () => machine.GpuTemperature!(); // Open checks it is there
            sources[MeterCatalog.Sources.Disk] = machine.Disk;
            sources[MeterCatalog.Sources.Network] = machine.Network;
            sources[MeterCatalog.Sources.Temperature] = () => machine.Temperatures!();
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
            Add<IReadOnlyList<DiskSample>>(MeterCatalog.DiskRead, (disks, s) => MeterRowBuilder.DiskRead(disks));
            Add<IReadOnlyList<DiskSample>>(MeterCatalog.DiskWrite, (disks, s) => MeterRowBuilder.DiskWrite(disks));
            Add<IReadOnlyList<DiskSample>>(MeterCatalog.DiskFree, (disks, s) => MeterRowBuilder.DiskFree(disks));
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
