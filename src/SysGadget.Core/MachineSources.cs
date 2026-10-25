using System;
using System.Collections.Generic;

namespace SysGadget.Core
{
    /// <summary>
    /// The machine side of the <see cref="MeterRegistry"/>: one factory per hardware source and the
    /// list of the machine's disks. <see cref="Real"/> gives the providers of this machine; tests give
    /// fixed snapshots, so the registry and the sampler run without the hardware.
    /// </summary>
    public sealed class MachineSources
    {
        /// <summary>Creates the set.</summary>
        /// <param name="cpu">Opens the CPU source (snapshot: IReadOnlyList&lt;CoreLoad&gt;).</param>
        /// <param name="memory">Opens the memory source (MemorySample).</param>
        /// <param name="gpu">Opens the GPU source (IReadOnlyList&lt;GpuSample&gt;).</param>
        /// <param name="gpuTemperature">Opens the GPU temperature source (IReadOnlyList&lt;TemperatureReading&gt;), or null without the NVIDIA driver.</param>
        /// <param name="disk">Opens the disk source (IReadOnlyList&lt;DiskSample&gt;).</param>
        /// <param name="network">Opens the network source (IReadOnlyList&lt;NetworkSample&gt;).</param>
        /// <param name="temperatures">Opens the all-sensors source (IReadOnlyList&lt;TemperatureReading&gt;), or null without admin rights.</param>
        /// <param name="diskNames">Lists the disks now, without sampling.</param>
        public MachineSources(
            Func<SampleSource> cpu,
            Func<SampleSource> memory,
            Func<SampleSource> gpu,
            Func<SampleSource>? gpuTemperature,
            Func<SampleSource> disk,
            Func<SampleSource> network,
            Func<SampleSource>? temperatures,
            Func<IReadOnlyList<string>> diskNames)
        {
            Cpu = cpu;
            Memory = memory;
            Gpu = gpu;
            GpuTemperature = gpuTemperature;
            Disk = disk;
            Network = network;
            Temperatures = temperatures;
            DiskNames = diskNames;
        }

        /// <summary>Opens the CPU source.</summary>
        public Func<SampleSource> Cpu { get; }

        /// <summary>Opens the memory source.</summary>
        public Func<SampleSource> Memory { get; }

        /// <summary>Opens the GPU source.</summary>
        public Func<SampleSource> Gpu { get; }

        /// <summary>Opens the GPU temperature source, or null without the NVIDIA driver.</summary>
        public Func<SampleSource>? GpuTemperature { get; }

        /// <summary>Opens the disk source.</summary>
        public Func<SampleSource> Disk { get; }

        /// <summary>Opens the network source.</summary>
        public Func<SampleSource> Network { get; }

        /// <summary>Opens the all-sensors source, or null without admin rights.</summary>
        public Func<SampleSource>? Temperatures { get; }

        /// <summary>Lists the disks now, without sampling.</summary>
        public Func<IReadOnlyList<string>> DiskNames { get; }

        /// <summary>The providers of this machine.</summary>
        /// <param name="temperatures">Opens the all-sensors source, or null when it is not available (no admin rights).</param>
        /// <param name="nvidiaInstalled">Whether the NVIDIA driver with NVML is installed.</param>
        /// <returns>The set.</returns>
        public static MachineSources Real(Func<ITemperatureSource>? temperatures, bool nvidiaInstalled)
        {
            return new MachineSources(
                () =>
                {
                    CpuCounterProvider cpu = new CpuCounterProvider();
                    return new SampleSource(context => cpu.Read(), cpu);
                },
                () => new SampleSource(context => MemoryProvider.Read(), null),
                () =>
                {
                    GpuCounterProvider gpu = new GpuCounterProvider();
                    return new SampleSource(context => gpu.Read(), null);
                },
                nvidiaInstalled ? OpenNvml : (Func<SampleSource>?)null,
                () =>
                {
                    DiskProvider disk = new DiskProvider();
                    return new SampleSource(context => disk.Read(), null);
                },
                () =>
                {
                    NetworkProvider network = new NetworkProvider();
                    return new SampleSource(context => network.Read(), null);
                },
                temperatures == null ? (Func<SampleSource>?)null : () => OpenTemperatures(temperatures),
                DiskProvider.InstanceNames);
        }

        private static SampleSource OpenNvml()
        {
            NvmlTemperatureProvider nvml = new NvmlTemperatureProvider();
            return new SampleSource(context => nvml.Read(), nvml);
        }

        private static SampleSource OpenTemperatures(Func<ITemperatureSource> factory)
        {
            ITemperatureSource temperatures = factory();
            return new SampleSource(context => temperatures.Read(), temperatures);
        }
    }
}
