using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using SysGadget.Core;
using Xunit;

namespace SysGadget.App.Tests
{
    public class ViewModelTests
    {
        [Fact]
        public void GroupViewModel_Update_SplitsCompactRows_AndSetsHasFlags()
        {
            GroupViewModel group = new GroupViewModel(MeterCatalog.CpuGroup, "CPU", Colors.Red);

            group.Update(new GroupSection(MeterCatalog.CpuGroup, "CPU", new[]
            {
                new MeterRow("Total", "20 %", 20.0, true),
                new MeterRow("Core 0", "95 %", 95.0, true, true),
            }));

            Assert.Equal("Total", group.Rows.Single().Label);
            Assert.Single(group.CompactRows);
            Assert.Equal("Core 0: 95 %", group.CompactRows[0].ToolTip);
            Assert.Equal(2, group.CompactRows[0].Level);
            Assert.True(group.HasRows);
            Assert.True(group.HasStrip);
            Assert.True(group.HasBody);

            group.Update(new GroupSection(MeterCatalog.CpuGroup, "CPU", new[] { new MeterRow("Total", "20 %", 20.0, true) }));

            Assert.True(group.HasRows);
            Assert.False(group.HasStrip);

            group.Update(new GroupSection(MeterCatalog.CpuGroup, "CPU", new MeterRow[0]));

            Assert.False(group.HasBody);
        }

        [Fact]
        public void GroupViewModel_Brushes_TintTheCardTowardsItsAccent()
        {
            Color accent = Color.FromRgb(0x4D, 0xA3, 0xFF);
            GroupViewModel cpu = new GroupViewModel(MeterCatalog.CpuGroup, "CPU", accent);

            Color card = ((SolidColorBrush)cpu.CardBrush).Color;
            Color border = ((SolidColorBrush)cpu.CardBorderBrush).Color;

            Assert.Equal(accent, ((SolidColorBrush)cpu.AccentBrush).Color);
            Assert.InRange(card.B, 0x27, border.B); // neutral card < tinted card < border < accent
            Assert.InRange(border.B, card.B, accent.B);
            Assert.True(cpu.CardBrush.IsFrozen);
        }

        [Fact]
        public void RainbowPalette_RunsFromRedToViolet_EvenlyOverTheCards()
        {
            Color first = RainbowPalette.Accent(0, 7);
            Color middle = RainbowPalette.Accent(3, 7);
            Color last = RainbowPalette.Accent(6, 7);

            Assert.True(first.R > first.G && first.R > first.B, "first card red: " + first);
            Assert.True(middle.G > middle.R && middle.G > middle.B, "middle card green: " + middle);
            Assert.True(last.B > last.G && last.R > last.G, "last card violet: " + last);
            Assert.Equal(first, RainbowPalette.Accent(0, 1));
            Assert.Equal(7, Enumerable.Range(0, 7).Select(i => RainbowPalette.Accent(i, 7)).Distinct().Count());
            Assert.Throws<System.ArgumentOutOfRangeException>(() => RainbowPalette.Accent(3, 3));
        }

        [Fact]
        public void GadgetViewModel_Update_ColoursTheCardsByPosition()
        {
            GadgetViewModel view = new GadgetViewModel();
            GroupSection cpu = new GroupSection(MeterCatalog.CpuGroup, "CPU", new MeterRow[0]);
            GroupSection memory = new GroupSection(MeterCatalog.MemoryGroup, "Memory", new MeterRow[0]);
            GroupSection gpu = new GroupSection(MeterCatalog.GpuGroup, "GPU", new MeterRow[0]);

            view.Update(new[] { cpu, memory, gpu });
            Color gpuWhenLast = ((SolidColorBrush)view.Groups[2].AccentBrush).Color;
            view.Update(new[] { gpu, cpu });

            Assert.Equal(RainbowPalette.Accent(2, 3), gpuWhenLast);
            Assert.Equal(RainbowPalette.Accent(0, 2), ((SolidColorBrush)view.Groups[0].AccentBrush).Color);
            Assert.Equal(RainbowPalette.Accent(1, 2), ((SolidColorBrush)view.Groups[1].AccentBrush).Color);
        }

        [Fact]
        public void GadgetViewModel_Update_KeepsCardsWhenGroupsStayTheSame_AndRebuildsOnChange()
        {
            GadgetViewModel view = new GadgetViewModel();
            GroupSection cpu = new GroupSection(MeterCatalog.CpuGroup, "CPU", new MeterRow[0]);
            GroupSection gpu = new GroupSection(MeterCatalog.GpuGroup, "GPU", new MeterRow[0]);

            view.Update(new[] { cpu, gpu });
            GroupViewModel first = view.Groups[0];
            view.Update(new[] { cpu, gpu });

            Assert.Same(first, view.Groups[0]);

            view.Update(new[] { gpu });

            Assert.Equal(MeterCatalog.GpuGroup, view.Groups.Single().Id);
        }

        [Fact]
        public void SettingsTreeNode_IsFirstShown_MarksTheFirstTickedItem_AndFollowsTheTicks()
        {
            GadgetSettings working = GadgetSettings.CreateDefault();
            List<SettingsTreeNode> tree = SettingsTreeNode.Build(working, new HashSet<string>(), null);
            SettingsTreeNode cpu = tree.Single(n => n.Id == MeterCatalog.CpuGroup);
            SettingsTreeNode total = cpu.Children.Single(c => c.Id == MeterCatalog.CpuTotal);
            SettingsTreeNode cores = cpu.Children.Single(c => c.Id == MeterCatalog.CpuCores);
            List<string> raised = new List<string>();
            cores.PropertyChanged += (sender, args) => raised.Add(args.PropertyName!);

            Assert.True(total.IsFirstShown);
            Assert.False(cores.IsFirstShown);
            Assert.False(cpu.IsFirstShown);

            total.IsChecked = false;

            Assert.True(cores.IsFirstShown);
            Assert.Contains(nameof(SettingsTreeNode.IsFirstShown), raised);
        }

        [Fact]
        public void SettingsTreeNode_Accepts_GroupsAmongGroups_AndItemsInsideTheirOwnGroup()
        {
            List<SettingsTreeNode> tree = SettingsTreeNode.Build(GadgetSettings.CreateDefault(), new HashSet<string>(), null);
            SettingsTreeNode cpu = tree.Single(n => n.Id == MeterCatalog.CpuGroup);
            SettingsTreeNode memory = tree.Single(n => n.Id == MeterCatalog.MemoryGroup);
            SettingsTreeNode total = cpu.Children[0];
            SettingsTreeNode cores = cpu.Children[1];
            SettingsTreeNode ram = memory.Children[0];

            Assert.True(memory.Accepts(cpu));
            Assert.True(total.Accepts(cores));
            Assert.False(total.Accepts(total));
            Assert.False(ram.Accepts(cores));
            Assert.False(cpu.Accepts(total));
            Assert.False(total.Accepts(cpu));
        }

        [Fact]
        public void SettingsTreeNode_EachDisk_IsItsOwnEntry_ThatCanBeTickedAndAccepted()
        {
            GadgetSettings working = GadgetSettings.CreateDefault();
            working.AddInstances(new[] { new InstanceRef(MeterCatalog.Disks, "0 D:"), new InstanceRef(MeterCatalog.Disks, "1 C:") });

            List<SettingsTreeNode> tree = SettingsTreeNode.Build(working, new HashSet<string>(), null);
            SettingsTreeNode storage = tree.Single(n => n.Id == MeterCatalog.StorageGroup);

            Assert.Equal(new[] { "Disk 0 D:", "Disk 1 C:" }, storage.Children.Select(c => c.Title));
            Assert.True(storage.Children[0].Accepts(storage.Children[1]));
            Assert.True(storage.Children[0].IsFirstShown);

            storage.Children[0].IsChecked = false;

            Assert.False(working.IsItemVisible("storage.disks|0 D:"));
            Assert.True(storage.Children[1].IsFirstShown);
        }

        [Fact]
        public void SettingsTreeNode_Checkboxes_WriteIntoTheSettings_InDisplayOrder()
        {
            GadgetSettings working = GadgetSettings.CreateDefault();
            working.PlaceGroup(MeterCatalog.GpuGroup, MeterCatalog.CpuGroup, false);

            List<SettingsTreeNode> tree = SettingsTreeNode.Build(working, new HashSet<string> { MeterCatalog.CpuGroup }, MeterCatalog.Ram);

            Assert.Equal(MeterCatalog.GpuGroup, tree[0].Id);
            Assert.Equal("GPU", tree[0].Title);
            Assert.False(tree.Single(n => n.Id == MeterCatalog.CpuGroup).IsExpanded);
            SettingsTreeNode memory = tree.Single(n => n.Id == MeterCatalog.MemoryGroup);
            Assert.True(memory.Children.Single(c => c.Id == MeterCatalog.Ram).IsSelected);

            memory.IsChecked = false;
            memory.Children.Single(c => c.Id == MeterCatalog.Commit).IsChecked = true;

            Assert.False(working.IsGroupVisible(MeterCatalog.MemoryGroup));
            Assert.True(working.IsItemVisible(MeterCatalog.Commit));
            Assert.Equal(new[] { MeterCatalog.CpuGroup }, SettingsTreeNode.ClosedGroups(tree));
        }
    }
}
