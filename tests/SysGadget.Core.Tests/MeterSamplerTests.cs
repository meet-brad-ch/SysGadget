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
        private static readonly SampleContext Context = new SampleContext(new DateTime(2026, 10, 25, 9, 0, 0), TimeSpan.FromMinutes(5));

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

        private static GadgetSettings OnlyItems(params string[] itemIds)
        {
            GadgetSettings settings = GadgetSettings.CreateDefault();
            foreach (ItemSetting item in settings.Groups.SelectMany(g => g.Items))
            {
                item.Visible = itemIds.Contains(item.Id);
            }

            return settings;
        }

        [Fact]
        public void Sample_Defaults_GivesCpuMemoryGpu_InOrder_TotalAsTheFirstCpuRow()
        {
            using (MeterSampler sampler = new MeterSampler(new MeterRegistry(null)))
            {
                GadgetSettings settings = GadgetSettings.CreateDefault();
                sampler.Sample(settings, Context);
                Thread.Sleep(1000);

                IReadOnlyList<GroupSection> sections = sampler.Sample(settings, Context);

                Assert.Equal(new[] { MeterCatalog.CpuGroup, MeterCatalog.MemoryGroup, MeterCatalog.GpuGroup }, sections.Select(s => s.Id));
                GroupSection cpu = sections[0];
                Assert.Equal("CPU", cpu.Title);
                Assert.Equal("Total", cpu.Rows[0].Label);
                Assert.Equal("Core 0", cpu.Rows[1].Label);
                Assert.Equal(Environment.ProcessorCount + 1, cpu.Rows.Count);
                Assert.Equal("RAM", sections[1].Rows.Single().Label);
                Assert.Equal(new[] { "Load", "VRAM" }, sections[2].Rows.Select(r => r.Label).Take(2));
            }
        }

        [Fact]
        public void Sample_EveryItemVisible_EveryGroupHasRows()
        {
            using (MeterSampler sampler = new MeterSampler(new MeterRegistry(null)))
            {
                GadgetSettings settings = OnlyItems(MeterCatalog.Groups.SelectMany(g => g.Items).Select(i => i.Id).ToArray());
                sampler.Sample(settings, Context);
                Thread.Sleep(1000);

                IReadOnlyList<GroupSection> sections = sampler.Sample(settings, Context);

                Assert.Equal(MeterCatalog.Groups.Count, sections.Count);
                Assert.All(sections, s => Assert.True(s.Rows.Count > 0, s.Id));
            }
        }

        [Fact]
        public void Sample_HiddenGroup_AndGroupWithoutVisibleItems_AreLeftOut()
        {
            using (MeterSampler sampler = new MeterSampler(new MeterRegistry(null)))
            {
                GadgetSettings settings = OnlyItems(MeterCatalog.Time, MeterCatalog.Ram);
                settings.SetGroupVisible(MeterCatalog.MemoryGroup, false);

                IReadOnlyList<GroupSection> sections = sampler.Sample(settings, Context);

                Assert.Equal(new[] { MeterCatalog.SystemGroup }, sections.Select(s => s.Id));
            }
        }

        [Fact]
        public void Sample_PassesCollapsedState_AndCompactCoreRows()
        {
            using (MeterSampler sampler = new MeterSampler(new MeterRegistry(null)))
            {
                GadgetSettings settings = OnlyItems(MeterCatalog.CpuCores);
                settings.SetCollapsed(MeterCatalog.CpuGroup, true);
                settings.UseCompactCores = true;

                GroupSection cpu = sampler.Sample(settings, Context).Single();

                Assert.True(cpu.Collapsed);
                Assert.All(cpu.Rows, r => Assert.True(r.Compact));
            }
        }

        [Fact]
        public void Sample_TemperaturesWithoutSource_ShowTheReasonRow()
        {
            using (MeterSampler sampler = new MeterSampler(new MeterRegistry(null)))
            {
                GadgetSettings settings = OnlyItems(MeterCatalog.Temperatures);

                MeterRow row = sampler.Sample(settings, Context).Single().Rows.Single();

                Assert.Equal("Turn on in Settings", row.ValueText);
                Assert.False(row.HasBar);
            }
        }

        [Fact]
        public void Sample_TemperatureSource_IsOpenedOnce_AndReleasedWhenHidden()
        {
            int created = 0;
            FakeTemperatures fake = new FakeTemperatures();
            using (MeterSampler sampler = new MeterSampler(new MeterRegistry(() => { created++; return fake; })))
            {
                GadgetSettings settings = OnlyItems(MeterCatalog.Temperatures);
                settings.UseLibreHardwareMonitor = true;

                sampler.Sample(settings, Context);
                MeterRow row = sampler.Sample(settings, Context).Single().Rows.Single();

                Assert.Equal(1, created);
                Assert.Equal("55 °C", row.ValueText);
                Assert.Equal(0, fake.Disposed);

                settings.SetItemVisible(MeterCatalog.Temperatures, false);
                settings.SetItemVisible(MeterCatalog.Time, true);
                sampler.Sample(settings, Context);

                Assert.Equal(1, fake.Disposed);
            }
        }
    }
}
