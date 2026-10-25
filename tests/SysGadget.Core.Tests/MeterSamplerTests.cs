using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using SysGadget.Core;
using Xunit;

namespace SysGadget.Core.Tests
{
    public class MeterSamplerTests
    {
        private sealed class FakeTemperatures : ITemperatureSource
        {
            public int Disposed { get; private set; }

            public IReadOnlyList<TemperatureReading> Read()
            {
                return new[] { new TemperatureReading("CPU", "Package", 55.0) };
            }

            public void Dispose()
            {
                Disposed++;
            }
        }

        private static GadgetSettings OnlyVisible(params string[] ids)
        {
            GadgetSettings settings = GadgetSettings.CreateDefault();
            foreach (MeterSetting meter in settings.Meters)
            {
                meter.Visible = ids.Contains(meter.Id);
            }

            return settings;
        }

        [Fact]
        public void Sample_DefaultSettings_GivesVisibleMetersInOrder_WithRows()
        {
            using (MeterSampler sampler = new MeterSampler(null))
            {
                GadgetSettings settings = GadgetSettings.CreateDefault();
                sampler.Sample(settings, DateTime.Now, TimeSpan.Zero);
                Thread.Sleep(1000);

                IReadOnlyList<MeterSection> sections = sampler.Sample(settings, DateTime.Now, TimeSpan.Zero);

                Assert.Equal(
                    settings.Meters.Where(m => m.Visible).Select(m => m.Id),
                    sections.Select(s => s.Id));
                Assert.All(sections, s => Assert.NotEmpty(s.Rows));
            }
        }

        [Fact]
        public void Sample_AllMetersVisible_EveryMeterHasRows()
        {
            using (MeterSampler sampler = new MeterSampler(null))
            {
                GadgetSettings settings = GadgetSettings.CreateDefault();
                foreach (MeterSetting meter in settings.Meters)
                {
                    meter.Visible = true;
                }

                sampler.Sample(settings, DateTime.Now, TimeSpan.FromMinutes(5));
                Thread.Sleep(1000);

                IReadOnlyList<MeterSection> sections = sampler.Sample(settings, DateTime.Now, TimeSpan.FromMinutes(5));

                Assert.Equal(MeterCatalog.All.Count, sections.Count);
                Assert.All(sections, s => Assert.NotEmpty(s.Rows));
            }
        }

        [Fact]
        public void Sample_TemperaturesWithoutSource_ShowsTheReason()
        {
            using (MeterSampler sampler = new MeterSampler(null))
            {
                GadgetSettings settings = OnlyVisible(MeterCatalog.Temperatures);

                MeterSection section = sampler.Sample(settings, DateTime.Now, TimeSpan.Zero).Single();

                Assert.Equal("turn on LibreHardwareMonitor in settings", section.Rows[0].ValueText);

                settings.UseLibreHardwareMonitor = true;
                section = sampler.Sample(settings, DateTime.Now, TimeSpan.Zero).Single();

                Assert.Equal("needs administrator rights", section.Rows[0].ValueText);
            }
        }

        [Fact]
        public void Sample_TemperatureSource_IsOpenedOnce_AndReleasedWhenHidden()
        {
            int created = 0;
            FakeTemperatures fake = new FakeTemperatures();
            using (MeterSampler sampler = new MeterSampler(() => { created++; return fake; }))
            {
                GadgetSettings settings = OnlyVisible(MeterCatalog.Temperatures);
                settings.UseLibreHardwareMonitor = true;

                sampler.Sample(settings, DateTime.Now, TimeSpan.Zero);
                MeterSection section = sampler.Sample(settings, DateTime.Now, TimeSpan.Zero).Single();

                Assert.Equal(1, created);
                Assert.Equal("55 °C", section.Rows[0].ValueText);
                Assert.Equal(0, fake.Disposed);

                settings.SetVisible(MeterCatalog.Temperatures, false);
                settings.SetVisible(MeterCatalog.Clock, true);
                sampler.Sample(settings, DateTime.Now, TimeSpan.Zero);

                Assert.Equal(1, fake.Disposed);
            }
        }
    }
}
