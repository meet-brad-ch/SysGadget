using System.Collections.Generic;
using System.Linq;
using SysGadget.Core;
using Xunit;

namespace SysGadget.App.Tests
{
    public class ViewModelTests
    {
        [Fact]
        public void GroupViewModel_Update_SplitsCompactRows_AndSetsShowFlags()
        {
            GroupViewModel group = new GroupViewModel(MeterCatalog.CpuGroup, "CPU");

            group.Update(new GroupSection(MeterCatalog.CpuGroup, "CPU", false, new[]
            {
                new MeterRow("Total", "20 %", 20.0, true),
                new MeterRow("Core 0", "95 %", 95.0, true, true),
            }));

            Assert.Equal("Total", group.Rows.Single().Label);
            Assert.Single(group.CompactRows);
            Assert.Equal("Core 0: 95 %", group.CompactRows[0].ToolTip);
            Assert.Equal(2, group.CompactRows[0].Level);
            Assert.True(group.ShowRows);
            Assert.True(group.ShowStrip);
            Assert.True(group.CanCollapse);

            group.Update(new GroupSection(MeterCatalog.CpuGroup, "CPU", true, new[] { new MeterRow("Total", "20 %", 20.0, true) }));

            Assert.True(group.IsCollapsed);
            Assert.False(group.ShowBody);
            Assert.True(group.CanCollapse);

            group.Update(new GroupSection(MeterCatalog.CpuGroup, "CPU", false, new MeterRow[0]));

            Assert.False(group.CanCollapse);
        }

        [Fact]
        public void GadgetViewModel_Update_KeepsCardsWhenGroupsStayTheSame_AndRebuildsOnChange()
        {
            GadgetViewModel view = new GadgetViewModel();
            GroupSection cpu = new GroupSection(MeterCatalog.CpuGroup, "CPU", false, new MeterRow[0]);
            GroupSection gpu = new GroupSection(MeterCatalog.GpuGroup, "GPU", false, new MeterRow[0]);

            view.Update(new[] { cpu, gpu });
            GroupViewModel first = view.Groups[0];
            view.Update(new[] { cpu, gpu });

            Assert.Same(first, view.Groups[0]);

            view.Update(new[] { gpu });

            Assert.Equal(MeterCatalog.GpuGroup, view.Groups.Single().Id);
            Assert.NotNull(view.Find(MeterCatalog.GpuGroup));
            Assert.Null(view.Find(MeterCatalog.CpuGroup));
        }

        [Fact]
        public void SettingsTreeNode_Checkboxes_WriteIntoTheSettings_InDisplayOrder()
        {
            GadgetSettings working = GadgetSettings.CreateDefault();
            working.MoveGroup(MeterCatalog.GpuGroup, -2);

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
