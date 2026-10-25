using System;
using System.IO;
using System.Linq;
using SysGadget.Core;
using Xunit;

namespace SysGadget.Core.Tests
{
    public class SettingsTests
    {
        [Fact]
        public void CreateDefault_ListsEveryCatalogueMeter_InCatalogueOrder()
        {
            GadgetSettings settings = GadgetSettings.CreateDefault();

            Assert.Equal(MeterCatalog.All.Select(d => d.Id), settings.Meters.Select(m => m.Id));
            Assert.True(settings.IsVisible(MeterCatalog.CpuCores));
            Assert.False(settings.IsVisible(MeterCatalog.Temperatures));
        }

        [Fact]
        public void SetVisible_ChangesOnlyThatMeter()
        {
            GadgetSettings settings = GadgetSettings.CreateDefault();

            settings.SetVisible(MeterCatalog.GpuLoad, false);

            Assert.False(settings.IsVisible(MeterCatalog.GpuLoad));
            Assert.True(settings.IsVisible(MeterCatalog.GpuMemory));
        }

        [Fact]
        public void Move_ShiftsOneStep_AndIgnoresMovesPastTheEnds()
        {
            GadgetSettings settings = GadgetSettings.CreateDefault();
            string first = settings.Meters[0].Id;
            string second = settings.Meters[1].Id;

            settings.Move(first, -1);
            Assert.Equal(first, settings.Meters[0].Id);

            settings.Move(first, 1);
            Assert.Equal(second, settings.Meters[0].Id);
            Assert.Equal(first, settings.Meters[1].Id);
        }

        [Fact]
        public void IsVisible_UnknownId_Throws()
        {
            Assert.Throws<System.Collections.Generic.KeyNotFoundException>(
                () => GadgetSettings.CreateDefault().IsVisible("nope"));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(100000)]
        public void Validate_WidthOutOfRange_Throws(int width)
        {
            GadgetSettings settings = GadgetSettings.CreateDefault();
            settings.Width = width;

            Assert.Throws<InvalidDataException>(() => settings.Validate());
        }

        [Fact]
        public void Validate_UnknownOrRepeatedMeter_Throws()
        {
            GadgetSettings unknown = GadgetSettings.CreateDefault();
            unknown.Meters.Add(new MeterSetting { Id = "nope", Visible = true });
            GadgetSettings repeated = GadgetSettings.CreateDefault();
            repeated.Meters.Add(new MeterSetting { Id = MeterCatalog.Memory, Visible = true });

            Assert.Throws<InvalidDataException>(() => unknown.Validate());
            Assert.Throws<InvalidDataException>(() => repeated.Validate());
        }

        [Fact]
        public void Validate_AppendsMeterMissingFromAnOlderFile()
        {
            GadgetSettings settings = GadgetSettings.CreateDefault();
            settings.Meters.RemoveAll(m => m.Id == MeterCatalog.Clock);

            settings.Validate();

            Assert.Equal(MeterCatalog.Clock, settings.Meters.Last().Id);
            Assert.False(settings.IsVisible(MeterCatalog.Clock));
        }

        [Fact]
        public void Store_RoundTrip_KeepsEveryValue_AndWritesEdgeAsText()
        {
            string folder = Path.Combine(Path.GetTempPath(), "SysGadgetTests-" + Guid.NewGuid().ToString("N"));
            string file = Path.Combine(folder, "settings.json");
            try
            {
                GadgetSettings written = GadgetSettings.CreateDefault();
                written.Edge = DockEdge.Bottom;
                written.MonitorIndex = 1;
                written.AlwaysOnTop = false;
                written.Width = 300;
                written.Opacity = 0.5;
                written.IntervalMs = 2000;
                written.UseLibreHardwareMonitor = true;
                written.SetVisible(MeterCatalog.Network, true);
                written.Move(MeterCatalog.Clock, -3);
                new SettingsStore(file).Save(written);

                GadgetSettings read = new SettingsStore(file).Load();

                Assert.Contains("\"Bottom\"", File.ReadAllText(file));
                Assert.Equal(DockEdge.Bottom, read.Edge);
                Assert.Equal(1, read.MonitorIndex);
                Assert.False(read.AlwaysOnTop);
                Assert.Equal(300, read.Width);
                Assert.Equal(0.5, read.Opacity, 6);
                Assert.Equal(2000, read.IntervalMs);
                Assert.True(read.UseLibreHardwareMonitor);
                Assert.True(read.IsVisible(MeterCatalog.Network));
                Assert.Equal(written.Meters.Select(m => m.Id), read.Meters.Select(m => m.Id));
            }
            finally
            {
                if (Directory.Exists(folder))
                {
                    Directory.Delete(folder, true);
                }
            }
        }

        [Fact]
        public void Store_MissingFile_GivesDefaults()
        {
            string file = Path.Combine(Path.GetTempPath(), "SysGadgetTests-missing-" + Guid.NewGuid().ToString("N"), "s.json");

            GadgetSettings settings = new SettingsStore(file).Load();

            Assert.Equal(MeterCatalog.All.Count, settings.Meters.Count);
        }

        [Fact]
        public void Store_BrokenJson_ThrowsInvalidData()
        {
            string folder = Path.Combine(Path.GetTempPath(), "SysGadgetTests-" + Guid.NewGuid().ToString("N"));
            string file = Path.Combine(folder, "settings.json");
            try
            {
                Directory.CreateDirectory(folder);
                File.WriteAllText(file, "{ not json");

                Assert.Throws<InvalidDataException>(() => new SettingsStore(file).Load());
            }
            finally
            {
                Directory.Delete(folder, true);
            }
        }
    }
}
