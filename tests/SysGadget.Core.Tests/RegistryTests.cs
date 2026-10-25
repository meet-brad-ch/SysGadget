using System;
using System.Collections.Generic;
using System.Linq;
using SysGadget.Core;
using Xunit;

namespace SysGadget.Core.Tests
{
    public class RegistryTests
    {
        private static readonly string[] TwoDisks = { "0 C:", "1 D:" };

        [Fact]
        public void Constructor_CoversTheCatalogue_WithAndWithoutTemperatureFactory()
        {
            MeterRegistry withoutTemperatures = new MeterRegistry(FakeMachine.Sources(TwoDisks, null, false));
            MeterRegistry withTemperatures = new MeterRegistry(FakeMachine.Sources(TwoDisks, () => new FakeTemperatures()));

            Assert.NotNull(withoutTemperatures);
            Assert.NotNull(withTemperatures);
        }

        [Fact]
        public void UnavailableReason_OnlyForTemperatureWithoutAdmin_AndGpuTemperatureWithoutNvidia()
        {
            Assert.Null(new MeterRegistry(FakeMachine.Sources(TwoDisks)).UnavailableReason(MeterCatalog.Sources.Cpu));
            Assert.Equal("Needs admin rights", new MeterRegistry(FakeMachine.Sources(TwoDisks)).UnavailableReason(MeterCatalog.Sources.Temperature));
            Assert.Null(new MeterRegistry(FakeMachine.Sources(TwoDisks, () => new FakeTemperatures())).UnavailableReason(MeterCatalog.Sources.Temperature));
            Assert.Null(new MeterRegistry(FakeMachine.Sources(TwoDisks)).UnavailableReason(MeterCatalog.Sources.GpuTemperature));
            Assert.Equal("No NVIDIA driver", new MeterRegistry(FakeMachine.Sources(TwoDisks, null, false)).UnavailableReason(MeterCatalog.Sources.GpuTemperature));
        }

        [Fact]
        public void Open_UnavailableSource_Throws()
        {
            MeterRegistry registry = new MeterRegistry(FakeMachine.Sources(TwoDisks, null, false));

            Assert.Throws<InvalidOperationException>(() => registry.Open(MeterCatalog.Sources.Temperature));
            Assert.Throws<InvalidOperationException>(() => registry.Open(MeterCatalog.Sources.GpuTemperature));
        }

        [Fact]
        public void FindInstances_ListsReadWriteFreePerDisk_DiskByDisk()
        {
            IReadOnlyList<InstanceRef> found = new MeterRegistry(FakeMachine.Sources(TwoDisks)).FindInstances();

            Assert.Equal(TwoDisks.Length * 3, found.Count);
            for (int i = 0; i < TwoDisks.Length; i++)
            {
                Assert.Equal(
                    new[] { MeterCatalog.DiskRead, MeterCatalog.DiskWrite, MeterCatalog.DiskFree },
                    found.Skip(i * 3).Take(3).Select(f => f.ItemId));
                Assert.All(found.Skip(i * 3).Take(3), f => Assert.Equal(TwoDisks[i], f.Instance));
            }
        }

        [Fact]
        public void Real_DiskNames_AreSpelledAsTheDiskSamples()
        {
            List<string> listed = MachineSources.Real(null, false).DiskNames().OrderBy(n => n, StringComparer.Ordinal).ToList();
            List<string> sampled = new DiskProvider().Read().Select(d => d.Name).OrderBy(n => n, StringComparer.Ordinal).ToList();

            Assert.NotEmpty(listed);
            Assert.Equal(sampled, listed);
        }

        [Fact]
        public void Rows_ClockItems_ReadTheInjectedContext()
        {
            MeterRegistry registry = new MeterRegistry(FakeMachine.Sources(TwoDisks));
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
