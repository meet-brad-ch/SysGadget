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
        public void CreateDefault_MirrorsTheCatalogue_GroupsAndItemsInOrder_WithoutInstances()
        {
            GadgetSettings settings = GadgetSettings.CreateDefault();

            Assert.Equal(MeterCatalog.Groups.Select(g => g.Id), settings.Groups.Select(g => g.Id));
            for (int i = 0; i < settings.Groups.Count; i++)
            {
                Assert.Equal(
                    MeterCatalog.Groups[i].Items.Where(d => !d.PerInstance).Select(d => d.Id),
                    settings.Groups[i].Items.Select(s => s.Id));
            }

            Assert.True(settings.IsItemVisible(MeterCatalog.CpuCores));
            Assert.False(settings.IsItemVisible(MeterCatalog.Temperatures));
            Assert.True(settings.IsGroupVisible(MeterCatalog.SensorsGroup));
            Assert.True(settings.UseCompactCores);
            Assert.False(settings.ReserveSpace);
            Assert.Equal(DockEdge.Floating, settings.Edge);
            Assert.True(settings.Horizontal);
            Assert.False(settings.ThinBar);
            Assert.Equal(500, settings.NetworkMbit);
        }

        [Fact]
        public void Visibility_ChangesOnlyItsTarget()
        {
            GadgetSettings settings = GadgetSettings.CreateDefault();

            settings.SetItemVisible(MeterCatalog.GpuLoad, false);
            settings.SetGroupVisible(MeterCatalog.MemoryGroup, false);

            Assert.False(settings.IsItemVisible(MeterCatalog.GpuLoad));
            Assert.True(settings.IsItemVisible(MeterCatalog.GpuMemory));
            Assert.False(settings.IsGroupVisible(MeterCatalog.MemoryGroup));
            Assert.True(settings.IsItemVisible(MeterCatalog.Ram));
        }

        [Fact]
        public void PlaceGroup_PutsItBeforeOrAfterTheTarget_InBothDirections()
        {
            GadgetSettings settings = GadgetSettings.CreateDefault();

            settings.PlaceGroup(MeterCatalog.SystemGroup, MeterCatalog.CpuGroup, false);
            Assert.Equal(new[] { MeterCatalog.SystemGroup, MeterCatalog.CpuGroup }, settings.Groups.Take(2).Select(g => g.Id));

            settings.PlaceGroup(MeterCatalog.SystemGroup, MeterCatalog.GpuGroup, true);
            Assert.Equal(
                new[] { MeterCatalog.CpuGroup, MeterCatalog.MemoryGroup, MeterCatalog.GpuGroup, MeterCatalog.SystemGroup },
                settings.Groups.Take(4).Select(g => g.Id));

            settings.PlaceGroup(MeterCatalog.CpuGroup, MeterCatalog.CpuGroup, true);
            Assert.Equal(MeterCatalog.CpuGroup, settings.Groups[0].Id);
            Assert.Equal(MeterCatalog.Groups.Count, settings.Groups.Count);
        }

        [Fact]
        public void AddInstances_AddsEachEntryOnce_AtTheEnd_InTheOrderGiven_WithTheItemDefault()
        {
            GadgetSettings settings = GadgetSettings.CreateDefault();
            InstanceRef[] found =
            {
                new InstanceRef(MeterCatalog.DiskRead, "0 D:"),
                new InstanceRef(MeterCatalog.DiskWrite, "0 D:"),
                new InstanceRef(MeterCatalog.DiskFree, "0 D:"),
                new InstanceRef(MeterCatalog.DiskRead, "1 C:"),
            };

            Assert.True(settings.AddInstances(found));
            Assert.False(settings.AddInstances(found));

            GroupSetting storage = settings.Groups.Single(g => g.Id == MeterCatalog.StorageGroup);
            Assert.Equal(
                new[] { "storage.read|0 D:", "storage.write|0 D:", "storage.free|0 D:", "storage.read|1 C:" },
                storage.Items.Select(i => i.Key));
            Assert.True(settings.IsItemVisible("storage.free|0 D:"));

            settings.PlaceItem("storage.read|1 C:", "storage.read|0 D:", false);
            Assert.Equal("storage.read|1 C:", storage.Items[0].Key);
        }

        [Fact]
        public void Validate_DropsRetiredDiskEntries_AndRefusesBadInstances()
        {
            GadgetSettings old = GadgetSettings.CreateDefault();
            List<ItemSetting> oldStorage = old.Groups.Single(g => g.Id == MeterCatalog.StorageGroup).Items;
            oldStorage.Add(new ItemSetting { Id = "storage.disks", Visible = true });
            oldStorage.Add(new ItemSetting { Id = "storage.disks", Instance = "1 C:", Visible = true });
            GadgetSettings instanceOnPlainItem = GadgetSettings.CreateDefault();
            instanceOnPlainItem.Groups[0].Items[0].Instance = "x";
            GadgetSettings repeatedDisk = GadgetSettings.CreateDefault();
            repeatedDisk.AddInstances(new[] { new InstanceRef(MeterCatalog.DiskRead, "0 D:") });
            repeatedDisk.Groups.Single(g => g.Id == MeterCatalog.StorageGroup).Items
                .Add(new ItemSetting { Id = MeterCatalog.DiskRead, Instance = "0 D:", Visible = true });
            GadgetSettings diskWithoutInstance = GadgetSettings.CreateDefault();
            diskWithoutInstance.Groups.Single(g => g.Id == MeterCatalog.StorageGroup).Items
                .Add(new ItemSetting { Id = MeterCatalog.DiskFree, Visible = true });

            old.Validate();

            Assert.Empty(old.Groups.Single(g => g.Id == MeterCatalog.StorageGroup).Items);
            Assert.Throws<InvalidDataException>(() => instanceOnPlainItem.Validate());
            Assert.Throws<InvalidDataException>(() => repeatedDisk.Validate());
            Assert.Throws<InvalidDataException>(() => diskWithoutInstance.Validate());
        }

        [Fact]
        public void ItemSetting_Keys_JoinAndSplit()
        {
            Assert.Equal("cpu.total", ItemSetting.MakeKey("cpu.total", string.Empty));
            Assert.Equal("storage.read|1 C:", ItemSetting.MakeKey("storage.read", "1 C:"));
            Assert.Equal("storage.read", ItemSetting.ItemIdOf("storage.read|1 C:"));
            Assert.Equal("cpu.total", ItemSetting.ItemIdOf("cpu.total"));
        }

        [Fact]
        public void PlaceItem_ReordersInsideTheGroup_AndRefusesAnotherGroup()
        {
            GadgetSettings settings = GadgetSettings.CreateDefault();

            settings.PlaceItem(MeterCatalog.Uptime, MeterCatalog.Time, false);
            GroupSetting system = settings.Groups.Single(g => g.Id == MeterCatalog.SystemGroup);
            Assert.Equal(new[] { MeterCatalog.Uptime, MeterCatalog.Time }, system.Items.Select(i => i.Id));

            settings.PlaceItem(MeterCatalog.Uptime, MeterCatalog.Time, true);
            Assert.Equal(new[] { MeterCatalog.Time, MeterCatalog.Uptime }, system.Items.Select(i => i.Id));

            Assert.Throws<InvalidOperationException>(() => settings.PlaceItem(MeterCatalog.Ram, MeterCatalog.Time, false));
        }

        [Fact]
        public void ShowsTemperatures_NeedsTheSensorsGroupAndTheTemperatureItem()
        {
            GadgetSettings settings = GadgetSettings.CreateDefault();
            Assert.False(settings.ShowsTemperatures());

            settings.SetItemVisible(MeterCatalog.Temperatures, true);
            Assert.True(settings.ShowsTemperatures());

            settings.SetGroupVisible(MeterCatalog.SensorsGroup, false);
            Assert.False(settings.ShowsTemperatures());
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

        [Theory]
        [InlineData(0)]
        [InlineData(100001)]
        public void Validate_BandwidthOutOfRange_Throws(int mbit)
        {
            GadgetSettings settings = GadgetSettings.CreateDefault();
            settings.NetworkMbit = mbit;

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
            copy.PlaceGroup(MeterCatalog.GpuGroup, MeterCatalog.CpuGroup, false);

            Assert.True(original.IsItemVisible(MeterCatalog.CpuCores));
            Assert.Equal(MeterCatalog.CpuGroup, original.Groups[0].Id);
        }

        [Fact]
        public void ThinItem_AnyEntryCanBeChosen_WhereverItSits_ItIsTicked_AndCloneKeepsIt()
        {
            GadgetSettings settings = GadgetSettings.CreateDefault();
            settings.SetItemVisible(MeterCatalog.Uptime, true); // Time and Uptime start unticked

            Assert.Equal(MeterCatalog.Uptime, settings.ThinEntry(MeterCatalog.SystemGroup)!.Key); // no choice: the first ticked

            settings.SetThinItem(MeterCatalog.Time);
            settings.PlaceItem(MeterCatalog.Time, MeterCatalog.Uptime, true);
            GadgetSettings copy = settings.Clone();

            Assert.True(settings.IsItemVisible(MeterCatalog.Time));
            Assert.Equal(MeterCatalog.Time, settings.ThinEntry(MeterCatalog.SystemGroup)!.Key);
            Assert.Equal(MeterCatalog.Time, copy.ThinEntry(MeterCatalog.SystemGroup)!.Key);
            Assert.Equal(MeterCatalog.CpuTotal, settings.ThinEntry(MeterCatalog.CpuGroup)!.Key); // other groups keep theirs
        }

        [Fact]
        public void ThinItem_UntickingIt_ClearsTheChoice_TheFirstTickedTakesOver()
        {
            GadgetSettings settings = GadgetSettings.CreateDefault();
            settings.SetThinItem(MeterCatalog.CpuCores);

            settings.SetItemVisible(MeterCatalog.CpuCores, false);
            settings.SetItemVisible(MeterCatalog.CpuCores, true);

            Assert.Equal(string.Empty, settings.Groups.Single(g => g.Id == MeterCatalog.CpuGroup).ThinItem);
            Assert.Equal(MeterCatalog.CpuTotal, settings.ThinEntry(MeterCatalog.CpuGroup)!.Key);
        }

        [Fact]
        public void Validate_ThinItemUnknownOrUnticked_Throws()
        {
            GadgetSettings unknown = GadgetSettings.CreateDefault();
            unknown.Groups.Single(g => g.Id == MeterCatalog.CpuGroup).ThinItem = MeterCatalog.Ram;
            GadgetSettings unticked = GadgetSettings.CreateDefault();
            unticked.SetItemVisible(MeterCatalog.CpuCores, false);
            unticked.Groups.Single(g => g.Id == MeterCatalog.CpuGroup).ThinItem = MeterCatalog.CpuCores;

            Assert.Throws<InvalidDataException>(() => unknown.Validate());
            Assert.Throws<InvalidDataException>(() => unticked.Validate());
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
                written.UseCompactCores = false;
                written.ReserveSpace = true;
                written.Horizontal = false;
                written.ThinBar = true;
                written.NetworkMbit = 1000;
                written.SetItemVisible(MeterCatalog.NetworkRates, true);
                written.SetGroupVisible(MeterCatalog.GpuGroup, false);
                written.PlaceGroup(MeterCatalog.SystemGroup, MeterCatalog.NetworkGroup, false);
                written.PlaceItem(MeterCatalog.CpuCores, MeterCatalog.CpuTotal, false);
                written.SetThinItem(MeterCatalog.Uptime);
                new SettingsStore(file).Save(written);

                GadgetSettings read = new SettingsStore(file).Load();

                Assert.Contains("\"Bottom\"", File.ReadAllText(file));
                Assert.Equal(DockEdge.Bottom, read.Edge);
                Assert.Equal(1, read.MonitorIndex);
                Assert.False(read.AlwaysOnTop);
                Assert.Equal(300, read.Width);
                Assert.Equal(0.5, read.Opacity, 6);
                Assert.Equal(2000, read.IntervalMs);
                Assert.False(read.UseCompactCores);
                Assert.True(read.ReserveSpace);
                Assert.False(read.Horizontal);
                Assert.True(read.ThinBar);
                Assert.Equal(1000, read.NetworkMbit);
                Assert.True(read.IsItemVisible(MeterCatalog.NetworkRates));
                Assert.False(read.IsGroupVisible(MeterCatalog.GpuGroup));
                Assert.Equal(written.Groups.Select(g => g.Id), read.Groups.Select(g => g.Id));
                Assert.Equal(MeterCatalog.CpuCores, read.Groups.Single(g => g.Id == MeterCatalog.CpuGroup).Items[0].Id);
                Assert.Equal(MeterCatalog.Uptime, read.ThinEntry(MeterCatalog.SystemGroup)!.Key);
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
