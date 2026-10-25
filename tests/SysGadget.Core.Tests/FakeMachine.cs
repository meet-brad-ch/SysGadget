using System;
using System.Collections.Generic;
using System.Linq;
using SysGadget.Core;

namespace SysGadget.Core.Tests
{
    /// <summary>A machine with fixed snapshots: four cores, one NVIDIA GPU, the given disks, one network adapter.</summary>
    internal static class FakeMachine
    {
        public const int Cores = 4;
        private const ulong GiB = 1UL << 30;

        /// <summary>The sources of the fake machine.</summary>
        /// <param name="disks">The disk instance names, in the order the machine lists them.</param>
        /// <param name="temperatures">Opens the all-sensors source, or null for "no admin rights".</param>
        /// <param name="nvidia">Whether the GPU temperature is available.</param>
        public static MachineSources Sources(IReadOnlyList<string> disks, Func<ITemperatureSource>? temperatures = null, bool nvidia = true)
        {
            return new MachineSources(
                () => Fixed(Enumerable.Range(0, Cores).Select(i => new CoreLoad(0, i, 10.0 * (i + 1))).ToList()),
                () => Fixed(new MemorySample(32 * GiB, 20 * GiB, 64 * GiB, 16 * GiB)),
                () => Fixed(new[] { new GpuSample(new GpuAdapterInfo("0x00000000_0x00014b7e", "NVIDIA GeForce RTX 4090", 24 * GiB), 30.0, 6 * GiB) }),
                nvidia ? () => Fixed(new[] { new TemperatureReading("NVIDIA GeForce RTX 4090", "GPU Core", 52.0, 87.0, 92.0) }) : (Func<SampleSource>?)null,
                () => Fixed(disks.Select(d => new DiskSample(d, 1.0e6, 2.0e6, new DiskSpace(1024 * GiB, 400 * GiB))).ToList()),
                () => Fixed(new[] { new NetworkSample("Ethernet", 1.0e6, 1.0e5) }),
                temperatures == null ? (Func<SampleSource>?)null : () =>
                {
                    ITemperatureSource source = temperatures();
                    return new SampleSource(context => source.Read(), source);
                },
                () => disks);
        }

        private static SampleSource Fixed(object snapshot)
        {
            return new SampleSource(context => snapshot, null);
        }
    }
}
