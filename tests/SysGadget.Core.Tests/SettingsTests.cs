using System;
using System.IO;
using System.Linq;
using SysGadget.Core;
using Xunit;

namespace SysGadget.Core.Tests
{
    public class SettingsTests
    {
        private static string TempFolder()
        {
            return Path.Combine(Path.GetTempPath(), "SysGadgetTests-" + Guid.NewGuid().ToString("N"));
        }

        [Fact]
        public void CreateDefault_MirrorsTheCatalogue_GroupsAndItemsInOrder()
        {
            GadgetSettings settings = GadgetSettings.CreateDefault();

            Assert.Equal(MeterCatalog.Groups.Select(g => g.Id), settings.Groups.Select(g => g.Id));
            for (int i = 0; i < settings.Groups.Count; i++)
            {
                Assert.Equal(MeterCatalog.Groups[i].Items.Select(d => d.Id), settings.Groups[i].Items.Select(s => s.Id));
            }

            Assert.True(settings.IsItemVisible(MeterCatalog.CpuCores));
            Assert.False(settings.IsItemVisible(MeterCatalog.Temperatures));
            Assert.True(settings.IsGroupVisible(MeterCatalog.SensorsGroup));
            Assert.True(settings.UseCompactCores);
        }

        [Fact]
        public void Visibility_AndCollapse_ChangeOnlyTheirTarget()
        {
            GadgetSettings settings = GadgetSettings.CreateDefault();

            settings.SetItemVisible(MeterCatalog.GpuLoad, false);
            settings.SetGroupVisible(MeterCatalog.MemoryGroup, false);
            settings.SetCollapsed(MeterCatalog.CpuGroup, true);

            Assert.False(settings.IsItemVisible(MeterCatalog.GpuLoad));
            Assert.True(settings.IsItemVisible(MeterCatalog.GpuMemory));
            Assert.False(settings.IsGroupVisible(MeterCatalog.MemoryGroup));
            Assert.True(settings.IsItemVisible(MeterCatalog.Ram));
            Assert.True(settings.IsCollapsed(MeterCatalog.CpuGroup));
            Assert.False(settings.IsCollapsed(MeterCatalog.GpuGroup));
        }

        [Fact]
        public void MoveGroup_AndMoveItem_ShiftOneStep_AndIgnoreMovesPastTheEnds()
        {
            GadgetSettings settings = GadgetSettings.CreateDefault();

            settings.MoveGroup(MeterCatalog.CpuGroup, -1);
            Assert.Equal(MeterCatalog.CpuGroup, settings.Groups[0].Id);
            settings.MoveGroup(MeterCatalog.CpuGroup, 1);
            Assert.Equal(MeterCatalog.MemoryGroup, settings.Groups[0].Id);
            Assert.Equal(MeterCatalog.CpuGroup, settings.Groups[1].Id);

            settings.MoveItem(MeterCatalog.Uptime, -1);
            GroupSetting system = settings.Groups.Single(g => g.Id == MeterCatalog.SystemGroup);
            Assert.Equal(new[] { MeterCatalog.Uptime, MeterCatalog.Time }, system.Items.Select(i => i.Id));
            settings.MoveItem(MeterCatalog.Uptime, -1);
            Assert.Equal(MeterCatalog.Uptime, system.Items[0].Id);
        }

        [Fact]
        public void Lookups_UnknownIds_Throw()
        {
            GadgetSettings settings = GadgetSettings.CreateDefault();

            Assert.Throws<System.Collections.Generic.KeyNotFoundException>(() => settings.IsItemVisible("nope"));
            Assert.Throws<System.Collections.Generic.KeyNotFoundException>(() => settings.IsGroupVisible("nope"));
        }

        [Theory]
        [InlineData(199)]
        [InlineData(801)]
        public void Validate_WidthOutOfRange_Throws(int width)
        {
            GadgetSettings settings = GadgetSettings.CreateDefault();
            settings.Width = width;

            Assert.Throws<InvalidDataException>(() => settings.Validate());
        }

        [Fact]
        public void Validate_UnknownRepeatedOrMisplacedIds_Throw()
        {
            GadgetSettings unknownGroup = GadgetSettings.CreateDefault();
            unknownGroup.Groups.Add(new GroupSetting { Id = "nope" });
            GadgetSettings repeatedGroup = GadgetSettings.CreateDefault();
            repeatedGroup.Groups.Add(new GroupSetting { Id = MeterCatalog.CpuGroup });
            GadgetSettings misplacedItem = GadgetSettings.CreateDefault();
            misplacedItem.Groups[0].Items.Add(new ItemSetting { Id = MeterCatalog.Ram, Visible = true });
            GadgetSettings repeatedItem = GadgetSettings.CreateDefault();
            repeatedItem.Groups[0].Items.Add(new ItemSetting { Id = MeterCatalog.CpuCores, Visible = true });

            Assert.Throws<InvalidDataException>(() => unknownGroup.Validate());
            Assert.Throws<InvalidDataException>(() => repeatedGroup.Validate());
            Assert.Throws<InvalidDataException>(() => misplacedItem.Validate());
            Assert.Throws<InvalidDataException>(() => repeatedItem.Validate());
        }

        [Fact]
        public void Validate_AppendsMissingGroupAndItem_WithDefaults()
        {
            GadgetSettings settings = GadgetSettings.CreateDefault();
            settings.Groups.RemoveAll(g => g.Id == MeterCatalog.NetworkGroup);
            settings.Groups.Single(g => g.Id == MeterCatalog.SystemGroup).Items.RemoveAll(i => i.Id == MeterCatalog.Time);

            settings.Validate();

            Assert.Equal(MeterCatalog.NetworkGroup, settings.Groups.Last().Id);
            Assert.Equal(MeterCatalog.Time, settings.Groups.Single(g => g.Id == MeterCatalog.SystemGroup).Items.Last().Id);
            Assert.False(settings.IsItemVisible(MeterCatalog.Time));
        }

        [Fact]
        public void Clone_IsIndependentOfTheOriginal()
        {
            GadgetSettings original = GadgetSettings.CreateDefault();
            GadgetSettings copy = original.Clone();

            copy.SetItemVisible(MeterCatalog.CpuCores, false);
            copy.MoveGroup(MeterCatalog.GpuGroup, -1);

            Assert.True(original.IsItemVisible(MeterCatalog.CpuCores));
            Assert.Equal(MeterCatalog.CpuGroup, original.Groups[0].Id);
        }

        [Fact]
        public void Store_RoundTrip_KeepsEveryValue_AndWritesEdgeAsText()
        {
            string folder = TempFolder();
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
                written.UseCompactCores = false;
                written.SetItemVisible(MeterCatalog.NetworkRates, true);
                written.SetGroupVisible(MeterCatalog.GpuGroup, false);
                written.SetCollapsed(MeterCatalog.CpuGroup, true);
                written.MoveGroup(MeterCatalog.SystemGroup, -2);
                written.MoveItem(MeterCatalog.CpuCores, -1);
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
                Assert.False(read.UseCompactCores);
                Assert.True(read.IsItemVisible(MeterCatalog.NetworkRates));
                Assert.False(read.IsGroupVisible(MeterCatalog.GpuGroup));
                Assert.True(read.IsCollapsed(MeterCatalog.CpuGroup));
                Assert.Equal(written.Groups.Select(g => g.Id), read.Groups.Select(g => g.Id));
                Assert.Equal(MeterCatalog.CpuCores, read.Groups.Single(g => g.Id == MeterCatalog.CpuGroup).Items[0].Id);
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
        public void Store_OldFlatFile_LoadsAsDefaults()
        {
            string folder = TempFolder();
            string file = Path.Combine(folder, "settings.json");
            try
            {
                Directory.CreateDirectory(folder);
                File.WriteAllText(file, "{ \"Edge\": \"Left\", \"Width\": 220, \"Meters\": [ { \"Id\": \"cpu.cores\", \"Visible\": false } ] }");

                GadgetSettings read = new SettingsStore(file).Load();

                Assert.Equal(DockEdge.Left, read.Edge);
                Assert.Equal(MeterCatalog.Groups.Select(g => g.Id), read.Groups.Select(g => g.Id));
                Assert.True(read.IsItemVisible(MeterCatalog.CpuCores));
            }
            finally
            {
                Directory.Delete(folder, true);
            }
        }

        [Fact]
        public void Store_MissingFile_GivesDefaults()
        {
            string file = Path.Combine(TempFolder(), "s.json");

            GadgetSettings settings = new SettingsStore(file).Load();

            Assert.Equal(MeterCatalog.Groups.Count, settings.Groups.Count);
        }

        [Fact]
        public void Store_BrokenJson_ThrowsInvalidData()
        {
            string folder = TempFolder();
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
