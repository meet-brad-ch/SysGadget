using System;
using System.Collections.Generic;

namespace SysGadget.Core
{
    /// <summary>
    /// Everything the gadget can show, as data: groups, their items, the source each item reads
    /// from and the defaults. Behaviour lives in <see cref="MeterRegistry"/>, keyed by these ids.
    /// </summary>
    public static class MeterCatalog
    {
        /// <summary>Ids of the sample sources.</summary>
        public static class Sources
        {
            /// <summary>Per-core CPU load.</summary>
            public const string Cpu = "source.cpu";

            /// <summary>Physical memory and commit charge.</summary>
            public const string Memory = "source.memory";

            /// <summary>GPU load and memory per adapter.</summary>
            public const string Gpu = "source.gpu";

            /// <summary>NVIDIA GPU temperatures through NVML.</summary>
            public const string GpuTemperature = "source.gputemperature";

            /// <summary>Disk activity.</summary>
            public const string Disk = "source.disk";

            /// <summary>Network throughput.</summary>
            public const string Network = "source.network";

            /// <summary>Temperatures through LibreHardwareMonitor.</summary>
            public const string Temperature = "source.temperature";

            /// <summary>Time of day and uptime.</summary>
            public const string Clock = "source.clock";

            /// <summary>Every source id.</summary>
            public static IReadOnlyList<string> All { get; } = new[] { Cpu, Memory, Gpu, GpuTemperature, Disk, Network, Temperature, Clock };
        }

        /// <summary>Id of the CPU group.</summary>
        public const string CpuGroup = "cpu";

        /// <summary>Id of the overall CPU load item.</summary>
        public const string CpuTotal = "cpu.total";

        /// <summary>Id of the per-core CPU load item.</summary>
        public const string CpuCores = "cpu.cores";

        /// <summary>Id of the memory group.</summary>
        public const string MemoryGroup = "memory";

        /// <summary>Id of the physical memory item.</summary>
        public const string Ram = "memory.ram";

        /// <summary>Id of the commit charge item.</summary>
        public const string Commit = "memory.commit";

        /// <summary>Id of the GPU group.</summary>
        public const string GpuGroup = "gpu";

        /// <summary>Id of the GPU load item.</summary>
        public const string GpuLoad = "gpu.load";

        /// <summary>Id of the GPU dedicated memory item.</summary>
        public const string GpuMemory = "gpu.memory";

        /// <summary>Id of the GPU temperature item (NVIDIA, through NVML).</summary>
        public const string GpuTemperature = "gpu.temperature";

        /// <summary>Id of the storage group.</summary>
        public const string StorageGroup = "storage";

        /// <summary>Id of the disk activity item.</summary>
        public const string Disks = "storage.disks";

        /// <summary>Id of the network group.</summary>
        public const string NetworkGroup = "network";

        /// <summary>Id of the network throughput item.</summary>
        public const string NetworkRates = "network.rates";

        /// <summary>Id of the sensors group.</summary>
        public const string SensorsGroup = "sensors";

        /// <summary>Id of the temperature item (needs LibreHardwareMonitor).</summary>
        public const string Temperatures = "sensors.temperatures";

        /// <summary>Id of the system group.</summary>
        public const string SystemGroup = "system";

        /// <summary>Id of the time-of-day item.</summary>
        public const string Time = "system.time";

        /// <summary>Id of the uptime item.</summary>
        public const string Uptime = "system.uptime";

        /// <summary>Every group, in default display order.</summary>
        public static IReadOnlyList<GroupDefinition> Groups { get; } = new List<GroupDefinition>
        {
            new GroupDefinition(CpuGroup, "CPU", new[]
            {
                new ItemDefinition(CpuTotal, "Total", Sources.Cpu, true),
                new ItemDefinition(CpuCores, "Per core", Sources.Cpu, true),
            }),
            new GroupDefinition(MemoryGroup, "Memory", new[]
            {
                new ItemDefinition(Ram, "RAM", Sources.Memory, true),
                new ItemDefinition(Commit, "Commit charge", Sources.Memory, false),
            }),
            new GroupDefinition(GpuGroup, "GPU", new[]
            {
                new ItemDefinition(GpuLoad, "Load", Sources.Gpu, true),
                new ItemDefinition(GpuMemory, "Dedicated memory", Sources.Gpu, true),
                new ItemDefinition(GpuTemperature, "Temperature (NVIDIA)", Sources.GpuTemperature, true),
            }),
            new GroupDefinition(StorageGroup, "Storage", new[]
            {
                new ItemDefinition(Disks, "Disk activity", Sources.Disk, false),
            }),
            new GroupDefinition(NetworkGroup, "Network", new[]
            {
                new ItemDefinition(NetworkRates, "Down and up", Sources.Network, false),
            }),
            new GroupDefinition(SensorsGroup, "Sensors", new[]
            {
                new ItemDefinition(Temperatures, "Temperatures (LibreHardwareMonitor)", Sources.Temperature, false),
            }),
            new GroupDefinition(SystemGroup, "System", new[]
            {
                new ItemDefinition(Time, "Time", Sources.Clock, false),
                new ItemDefinition(Uptime, "Uptime", Sources.Clock, false),
            }),
        };

        /// <summary>Finds a group.</summary>
        /// <param name="id">The group id.</param>
        /// <returns>The definition.</returns>
        /// <exception cref="KeyNotFoundException">No group has this id.</exception>
        public static GroupDefinition GetGroup(string id)
        {
            foreach (GroupDefinition group in Groups)
            {
                if (string.Equals(group.Id, id, StringComparison.Ordinal))
                {
                    return group;
                }
            }

            throw new KeyNotFoundException("Unknown group id '" + id + "'.");
        }

        /// <summary>Finds an item.</summary>
        /// <param name="id">The item id.</param>
        /// <returns>The definition.</returns>
        /// <exception cref="KeyNotFoundException">No item has this id.</exception>
        public static ItemDefinition GetItem(string id)
        {
            GroupDefinition group = GroupOf(id);
            foreach (ItemDefinition item in group.Items)
            {
                if (string.Equals(item.Id, id, StringComparison.Ordinal))
                {
                    return item;
                }
            }

            throw new KeyNotFoundException("Unknown item id '" + id + "'.");
        }

        /// <summary>Finds the group an item belongs to.</summary>
        /// <param name="itemId">The item id.</param>
        /// <returns>The group definition.</returns>
        /// <exception cref="KeyNotFoundException">No item has this id.</exception>
        public static GroupDefinition GroupOf(string itemId)
        {
            foreach (GroupDefinition group in Groups)
            {
                foreach (ItemDefinition item in group.Items)
                {
                    if (string.Equals(item.Id, itemId, StringComparison.Ordinal))
                    {
                        return group;
                    }
                }
            }

            throw new KeyNotFoundException("Unknown item id '" + itemId + "'.");
        }
    }
}
