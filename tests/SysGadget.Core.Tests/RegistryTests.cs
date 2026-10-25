using System;
using System.Collections.Generic;
using System.Linq;
using SysGadget.Core;
using Xunit;

namespace SysGadget.Core.Tests
{
    public class RegistryTests
    {
        [Fact]
        public void Constructor_CoversTheCatalogue_WithAndWithoutTemperatureFactory()
        {
            MeterRegistry withoutTemperatures = new MeterRegistry(null, false);
            MeterRegistry withTemperatures = new MeterRegistry(() => new FakeTemperatures(), true);

            Assert.NotNull(withoutTemperatures);
            Assert.NotNull(withTemperatures);
        }

        [Fact]
        public void UnavailableReason_OnlyForTemperature_FollowingSettingsAndFactory()
        {
            GadgetSettings off = GadgetSettings.CreateDefault();
            GadgetSettings on = GadgetSettings.CreateDefault();
            on.UseLibreHardwareMonitor = true;

            Assert.Null(new MeterRegistry(null, true).UnavailableReason(MeterCatalog.Sources.Cpu, off));
            Assert.Equal("Turn on in Settings",
                new MeterRegistry(null, true).UnavailableReason(MeterCatalog.Sources.Temperature, off));
            Assert.Equal("Needs admin rights",
                new MeterRegistry(null, true).UnavailableReason(MeterCatalog.Sources.Temperature, on));
            Assert.Null(new MeterRegistry(() => new FakeTemperatures(), true).UnavailableReason(MeterCatalog.Sources.Temperature, on));
            Assert.Null(new MeterRegistry(null, true).UnavailableReason(MeterCatalog.Sources.GpuTemperature, off));
            Assert.Equal("No NVIDIA driver", new MeterRegistry(null, false).UnavailableReason(MeterCatalog.Sources.GpuTemperature, off));
        }

        [Fact]
        public void FindInstances_ListsThisMachinesDisks_AsSpelledByTheSampler()
        {
            IReadOnlyList<InstanceRef> found = new MeterRegistry(null, false).FindInstances();

            Assert.NotEmpty(found);
            Assert.All(found, f => Assert.Equal(MeterCatalog.Disks, f.ItemId));
            Assert.Equal(
                new DiskProvider().Read().Select(d => d.Name).OrderBy(n => n, System.StringComparer.Ordinal),
                found.Select(f => f.Instance));
        }

        [Fact]
        public void Rows_ClockItems_ReadTheInjectedContext()
        {
            MeterRegistry registry = new MeterRegistry(null, true);
            SampleContext context = new SampleContext(new DateTime(2026, 10, 25, 9, 30, 0), TimeSpan.FromHours(2));
            object snapshot;
            using (SampleSource clock = registry.Open(MeterCatalog.Sources.Clock))
            {
                snapshot = clock.Read(context);
            }

            IReadOnlyList<MeterRow> uptime = registry.Rows(MeterCatalog.Uptime, snapshot, GadgetSettings.CreateDefault());

            Assert.Equal("02:00:00", uptime.Single().ValueText);
        }

        private sealed class FakeTemperatures : ITemperatureSource
        {
            public IReadOnlyList<TemperatureReading> Read()
            {
                return new TemperatureReading[0];
            }

            public void Dispose()
            {
            }
        }
    }
}
