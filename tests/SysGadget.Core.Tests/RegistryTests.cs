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
            MeterRegistry withoutTemperatures = new MeterRegistry(null);
            MeterRegistry withTemperatures = new MeterRegistry(() => new FakeTemperatures());

            Assert.NotNull(withoutTemperatures);
            Assert.NotNull(withTemperatures);
        }

        [Fact]
        public void UnavailableReason_OnlyForTemperature_FollowingSettingsAndFactory()
        {
            GadgetSettings off = GadgetSettings.CreateDefault();
            GadgetSettings on = GadgetSettings.CreateDefault();
            on.UseLibreHardwareMonitor = true;

            Assert.Null(new MeterRegistry(null).UnavailableReason(MeterCatalog.Sources.Cpu, off));
            Assert.Equal("Turn on in Settings",
                new MeterRegistry(null).UnavailableReason(MeterCatalog.Sources.Temperature, off));
            Assert.Equal("Needs admin rights",
                new MeterRegistry(null).UnavailableReason(MeterCatalog.Sources.Temperature, on));
            Assert.Null(new MeterRegistry(() => new FakeTemperatures()).UnavailableReason(MeterCatalog.Sources.Temperature, on));
        }

        [Fact]
        public void Rows_ClockItems_ReadTheInjectedContext()
        {
            MeterRegistry registry = new MeterRegistry(null);
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
