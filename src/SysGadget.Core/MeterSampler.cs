using System;
using System.Collections.Generic;

namespace SysGadget.Core
{
    /// <summary>
    /// Samples the visible meters. A provider is opened when the first meter that needs it
    /// becomes visible and released when the last one is hidden, so a hidden meter costs nothing.
    /// </summary>
    public sealed class MeterSampler : IDisposable
    {
        private readonly Func<ITemperatureSource>? temperatureFactory;
        private readonly MemoryProvider memory = new MemoryProvider();
        private CpuCounterProvider? cpu;
        private GpuCounterProvider? gpu;
        private NetworkProvider? network;
        private DiskProvider? disks;
        private ITemperatureSource? temperatures;

        /// <summary>Creates a sampler.</summary>
        /// <param name="temperatureFactory">
        /// Opens the temperature source, or null when none is available (no admin rights or not installed).
        /// </param>
        public MeterSampler(Func<ITemperatureSource>? temperatureFactory)
        {
            this.temperatureFactory = temperatureFactory;
        }

        /// <summary>Samples every visible meter.</summary>
        /// <param name="settings">The current settings.</param>
        /// <param name="now">The local time, for the clock.</param>
        /// <param name="uptime">The time since boot, for the clock.</param>
        /// <returns>One section per visible meter, in display order.</returns>
        public IReadOnlyList<MeterSection> Sample(GadgetSettings settings, DateTime now, TimeSpan uptime)
        {
            bool needCpu = settings.IsVisible(MeterCatalog.CpuCores) || settings.IsVisible(MeterCatalog.CpuTotal);
            bool needGpu = settings.IsVisible(MeterCatalog.GpuLoad) || settings.IsVisible(MeterCatalog.GpuMemory);
            bool needTemperatures = settings.IsVisible(MeterCatalog.Temperatures)
                && settings.UseLibreHardwareMonitor
                && temperatureFactory != null;

            cpu = Maintain(needCpu, cpu, () => new CpuCounterProvider());
            gpu = Maintain(needGpu, gpu, () => new GpuCounterProvider());
            network = Maintain(settings.IsVisible(MeterCatalog.Network), network, () => new NetworkProvider());
            disks = Maintain(settings.IsVisible(MeterCatalog.Disks), disks, () => new DiskProvider());
            temperatures = Maintain(needTemperatures, temperatures, () => temperatureFactory!());

            IReadOnlyList<CoreLoad>? loads = cpu?.Read();
            IReadOnlyList<GpuSample>? gpus = gpu?.Read();
            MemorySample? mem = settings.IsVisible(MeterCatalog.Memory) || settings.IsVisible(MeterCatalog.Commit)
                ? memory.Read()
                : null;

            List<MeterSection> sections = new List<MeterSection>();
            foreach (MeterSetting meter in settings.Meters)
            {
                if (!meter.Visible)
                {
                    continue;
                }

                IReadOnlyList<MeterRow> rows = BuildRows(meter.Id, settings, loads, gpus, mem, now, uptime);
                sections.Add(new MeterSection(meter.Id, MeterCatalog.Get(meter.Id).Title, rows));
            }

            return sections;
        }

        /// <summary>Releases every provider.</summary>
        public void Dispose()
        {
            cpu?.Dispose();
            cpu = null;
            temperatures?.Dispose();
            temperatures = null;
        }

        private IReadOnlyList<MeterRow> BuildRows(
            string id,
            GadgetSettings settings,
            IReadOnlyList<CoreLoad>? loads,
            IReadOnlyList<GpuSample>? gpus,
            MemorySample? mem,
            DateTime now,
            TimeSpan uptime)
        {
            switch (id)
            {
                case MeterCatalog.CpuTotal:
                    return MeterRowBuilder.CpuTotal(loads!);
                case MeterCatalog.CpuCores:
                    return MeterRowBuilder.CpuCores(loads!);
                case MeterCatalog.Memory:
                    return MeterRowBuilder.Memory(mem!);
                case MeterCatalog.Commit:
                    return MeterRowBuilder.Commit(mem!);
                case MeterCatalog.GpuLoad:
                    return MeterRowBuilder.GpuLoad(gpus!);
                case MeterCatalog.GpuMemory:
                    return MeterRowBuilder.GpuMemory(gpus!);
                case MeterCatalog.Network:
                    return MeterRowBuilder.Network(network!.Read());
                case MeterCatalog.Disks:
                    return MeterRowBuilder.Disks(disks!.Read());
                case MeterCatalog.Temperatures:
                    return temperatures != null
                        ? MeterRowBuilder.Temperatures(temperatures.Read())
                        : new[] { new MeterRow("Temperatures", UnavailableReason(settings), 0.0, false) };
                case MeterCatalog.Clock:
                    return MeterRowBuilder.Clock(now, uptime);
                default:
                    throw new InvalidOperationException("No sampler for meter '" + id + "'.");
            }
        }

        private string UnavailableReason(GadgetSettings settings)
        {
            if (!settings.UseLibreHardwareMonitor)
            {
                return "turn on LibreHardwareMonitor in settings";
            }

            return "needs administrator rights";
        }

        private static T? Maintain<T>(bool needed, T? current, Func<T> create)
            where T : class
        {
            if (needed)
            {
                return current ?? create();
            }

            (current as IDisposable)?.Dispose();
            return null;
        }
    }
}
