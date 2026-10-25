using System;
using System.Collections.Generic;

namespace SysGadget.Core
{
    /// <summary>The meters this version can show, in default display order.</summary>
    public static class MeterCatalog
    {
        /// <summary>Id of the per-core CPU bars.</summary>
        public const string CpuCores = "cpu.cores";

        /// <summary>Id of the overall CPU bar.</summary>
        public const string CpuTotal = "cpu.total";

        /// <summary>Id of the physical memory bar.</summary>
        public const string Memory = "memory";

        /// <summary>Id of the commit charge bar.</summary>
        public const string Commit = "memory.commit";

        /// <summary>Id of the GPU load bars.</summary>
        public const string GpuLoad = "gpu.load";

        /// <summary>Id of the GPU dedicated memory bars.</summary>
        public const string GpuMemory = "gpu.memory";

        /// <summary>Id of the optional temperature readings (needs LibreHardwareMonitor).</summary>
        public const string Temperatures = "sensors.temperatures";

        /// <summary>Id of the network throughput meter.</summary>
        public const string Network = "network";

        /// <summary>Id of the disk activity meter.</summary>
        public const string Disks = "disks";

        /// <summary>Id of the clock and uptime line.</summary>
        public const string Clock = "clock";

        /// <summary>Every meter, in default display order.</summary>
        public static IReadOnlyList<MeterDefinition> All { get; } = new List<MeterDefinition>
        {
            new MeterDefinition(CpuTotal, "CPU total", true),
            new MeterDefinition(CpuCores, "CPU per core", true),
            new MeterDefinition(Memory, "Memory", true),
            new MeterDefinition(Commit, "Commit charge", false),
            new MeterDefinition(GpuLoad, "GPU load", true),
            new MeterDefinition(GpuMemory, "GPU memory", true),
            new MeterDefinition(Network, "Network", false),
            new MeterDefinition(Disks, "Disk activity", false),
            new MeterDefinition(Temperatures, "Temperatures (LibreHardwareMonitor)", false),
            new MeterDefinition(Clock, "Clock and uptime", false),
        };

        /// <summary>Finds a definition.</summary>
        /// <param name="id">The meter id.</param>
        /// <returns>The definition.</returns>
        /// <exception cref="KeyNotFoundException">No meter has this id.</exception>
        public static MeterDefinition Get(string id)
        {
            foreach (MeterDefinition definition in All)
            {
                if (string.Equals(definition.Id, id, StringComparison.Ordinal))
                {
                    return definition;
                }
            }

            throw new KeyNotFoundException("Unknown meter id '" + id + "'.");
        }
    }
}
