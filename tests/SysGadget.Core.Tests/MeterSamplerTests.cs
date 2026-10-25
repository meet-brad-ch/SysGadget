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

        /// <summary>Defaults plus this machine's disks, showing only the given items (a per-instance id shows all its instances).</summary>
        private static GadgetSettings OnlyItems(params string[] itemIds)
        {
            GadgetSettings settings = GadgetSettings.CreateDefault();
            settings.AddInstances(new MeterRegistry(null, false).FindInstances());
            foreach (ItemSetting item in settings.Groups.SelectMany(g => g.Items))
            {
                item.Visible = itemIds.Contains(item.Id);
            }

            return settings;
        }

        [Fact]
        public void Sample_Defaults_GivesCpuMemoryGpu_InOrder_TotalAsTheFirstCpuRow()
        {
            using (MeterSampler sampler = new MeterSampler(new MeterRegistry(null, NvmlTemperatureProvider.IsInstalled())))
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
                Assert.Equal(new[] { "Load", "VRAM", "Temp" }, sections[2].Rows.Select(r => r.Label));
            }
        }

        [Fact]
        public void Sample_EveryItemVisible_EveryGroupHasRows()
        {
            using (MeterSampler sampler = new MeterSampler(new MeterRegistry(null, NvmlTemperatureProvider.IsInstalled())))
            {
                GadgetSettings settings = OnlyItems(MeterCatalog.Groups.SelectMany(g => g.Items).Select(i => i.Id).ToArray());
                sampler.Sample(settings, Context);
                Thread.Sleep(1000);

                IReadOnlyList<GroupSection> sections = sampler.Sample(settings, Context);

                Assert.Equal(MeterCatalog.Groups.Count, sections.Count);
                Assert.All(sections, s => Assert.True(s.Rows.Count > 0, s.Id));
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Sample_EveryItemVisible_ThinOrNot_TwoSamplesWork(bool thin)
        {
            using (MeterSampler sampler = new MeterSampler(new MeterRegistry(null, NvmlTemperatureProvider.IsInstalled())))
            {
                GadgetSettings settings = OnlyItems(MeterCatalog.Groups.SelectMany(g => g.Items).Select(i => i.Id).ToArray());
                settings.ThinBar = thin;

                sampler.Sample(settings, Context);
                IReadOnlyList<GroupSection> sections = sampler.Sample(settings, Context);

                Assert.Equal(MeterCatalog.Groups.Count, sections.Count);
            }
        }

        [Fact]
        public void Sample_DiskEntries_ShowOnlyTheirDisk_InTheirOrder_AndTheThinBarShowsTheFirstShownDisk()
        {
            using (MeterSampler sampler = new MeterSampler(new MeterRegistry(null, false)))
            {
                GadgetSettings settings = OnlyItems(MeterCatalog.Disks);
                List<ItemSetting> disks = settings.Groups.Single(g => g.Id == MeterCatalog.StorageGroup).Items;
                Assert.True(disks.Count >= 2, "this test needs a machine with two disks");
                string first = disks[0].Instance;
                string second = disks[1].Instance;
                settings.PlaceItem(disks[1].Key, disks[0].Key, false); // second disk first
                settings.SetItemVisible(ItemSetting.MakeKey(MeterCatalog.Disks, first), false);

                GroupSection storage = sampler.Sample(settings, Context).Single();

                Assert.All(storage.Rows, r => Assert.Equal(second, r.Instance));
                Assert.Equal(second, storage.Rows[0].Label);

                settings.SetItemVisible(ItemSetting.MakeKey(MeterCatalog.Disks, first), true);
                settings.ThinBar = true;
                MeterRow thin = sampler.Sample(settings, Context).Single().Rows.Single();

                Assert.Equal(second, thin.Instance);
                Assert.True(thin.HasBar);
            }
        }

        [Fact]
        public void Sample_HiddenGroup_AndGroupWithoutVisibleItems_AreLeftOut()
        {
            using (MeterSampler sampler = new MeterSampler(new MeterRegistry(null, NvmlTemperatureProvider.IsInstalled())))
            {
                GadgetSettings settings = OnlyItems(MeterCatalog.Time, MeterCatalog.Ram);
                settings.SetGroupVisible(MeterCatalog.MemoryGroup, false);

                IReadOnlyList<GroupSection> sections = sampler.Sample(settings, Context);

                Assert.Equal(new[] { MeterCatalog.SystemGroup }, sections.Select(s => s.Id));
            }
        }

        [Fact]
        public void Sample_CompactCores_MarksTheCoreRowsCompact()
        {
            using (MeterSampler sampler = new MeterSampler(new MeterRegistry(null, NvmlTemperatureProvider.IsInstalled())))
            {
                GadgetSettings settings = OnlyItems(MeterCatalog.CpuCores);
                settings.UseCompactCores = true;

                GroupSection cpu = sampler.Sample(settings, Context).Single();

                Assert.All(cpu.Rows, r => Assert.True(r.Compact));
            }
        }

        [Fact]
        public void Sample_ThinBar_ShowsOneLineOfTheFirstVisibleItemOfEachGroup_InItsOrder()
        {
            using (MeterSampler sampler = new MeterSampler(new MeterRegistry(null, NvmlTemperatureProvider.IsInstalled())))
            {
                GadgetSettings settings = OnlyItems(MeterCatalog.CpuTotal, MeterCatalog.CpuCores, MeterCatalog.Ram, MeterCatalog.Commit,
                    MeterCatalog.Time, MeterCatalog.Uptime, MeterCatalog.NetworkRates, MeterCatalog.Disks);
                settings.ThinBar = true;
                settings.PlaceItem(MeterCatalog.Commit, MeterCatalog.Ram, false);
                settings.PlaceItem(MeterCatalog.Uptime, MeterCatalog.Time, false);

                IReadOnlyList<GroupSection> sections = sampler.Sample(settings, Context);

                Assert.Equal("Total", sections.Single(s => s.Id == MeterCatalog.CpuGroup).Rows.Single().Label);
                Assert.Equal("Commit", sections.Single(s => s.Id == MeterCatalog.MemoryGroup).Rows.Single().Label);
                Assert.Equal("Uptime", sections.Single(s => s.Id == MeterCatalog.SystemGroup).Rows.Single().Label);
                Assert.Equal("Down", sections.Single(s => s.Id == MeterCatalog.NetworkGroup).Rows.Single().Label);
                Assert.True(sections.Single(s => s.Id == MeterCatalog.StorageGroup).Rows.Single().HasBar);

                settings.UseCompactCores = true;
                settings.PlaceItem(MeterCatalog.CpuCores, MeterCatalog.CpuTotal, false);
                GroupSection cpu = sampler.Sample(settings, Context).Single(s => s.Id == MeterCatalog.CpuGroup);

                Assert.Equal(Environment.ProcessorCount, cpu.Rows.Count);
                Assert.All(cpu.Rows, r => Assert.True(r.Compact));
            }
        }

        [Fact]
        public void Sample_TemperaturesWithoutSource_ShowTheReasonRow()
        {
            using (MeterSampler sampler = new MeterSampler(new MeterRegistry(null, NvmlTemperatureProvider.IsInstalled())))
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
            using (MeterSampler sampler = new MeterSampler(new MeterRegistry(() => { created++; return fake; }, NvmlTemperatureProvider.IsInstalled())))
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
