using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using SysGadget.Core;
using Xunit;

namespace SysGadget.App.Tests
{
    /// <summary>Drives the real settings window, opened non-modal as the gadget opens it, off-screen.</summary>
    public class SettingsWindowTests
    {
        private static SettingsWindow Open(GadgetSettings settings, List<GadgetSettings> applied)
        {
            SettingsWindow window = new SettingsWindow(settings, new[] { "1: test" }, applied.Add)
            {
                WindowStartupLocation = WindowStartupLocation.Manual,
                Left = -20000,
                ShowActivated = false,
            };
            window.Show();
            Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ContextIdle);
            return window;
        }

        private static void Click(Button button)
        {
            ((IInvokeProvider)new ButtonAutomationPeer(button).GetPattern(PatternInterface.Invoke)!).Invoke();
            Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ContextIdle);
        }

        private static IEnumerable<T> Descendants<T>(DependencyObject root)
            where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(root, i);
                if (child is T match)
                {
                    yield return match;
                }

                foreach (T deeper in Descendants<T>(child))
                {
                    yield return deeper;
                }
            }
        }

        private static SettingsTreeNode Node(SettingsWindow window, string id)
        {
            IEnumerable<SettingsTreeNode> groups = (IEnumerable<SettingsTreeNode>)window.MeterTree.ItemsSource;
            return groups.Concat(groups.SelectMany(g => g.Children)).Single(n => n.Id == id);
        }

        /// <summary>Regression: Cancel relied on IsCancel, which closes only modal dialogs, so it did nothing.</summary>
        [Fact]
        public void Cancel_ClosesTheWindow_WithoutApplying()
        {
            StaRunner.Run(() =>
            {
                List<GadgetSettings> applied = new List<GadgetSettings>();
                SettingsWindow window = Open(GadgetSettings.CreateDefault(), applied);
                bool closed = false;
                window.Closed += (sender, args) => closed = true;

                Click(window.CancelButton);

                Assert.True(closed);
                Assert.Empty(applied);
            });
        }

        [Fact]
        public void Reorder_ItemDroppedAboveAnother_AndGroupDroppedBelowAnother_OkAppliesTheOrder()
        {
            StaRunner.Run(() =>
            {
                List<GadgetSettings> applied = new List<GadgetSettings>();
                GadgetSettings settings = GadgetSettings.CreateDefault();
                SettingsWindow window = Open(settings, applied);

                window.Reorder(Node(window, MeterCatalog.CpuCores), Node(window, MeterCatalog.CpuTotal), false);
                window.Reorder(Node(window, MeterCatalog.CpuGroup), Node(window, MeterCatalog.GpuGroup), true);
                Click(Descendants<Button>(window).Single(b => (string)b.Content == "OK"));

                GadgetSettings result = applied.Single();
                Assert.Equal(new[] { MeterCatalog.CpuCores, MeterCatalog.CpuTotal },
                    result.Groups.Single(g => g.Id == MeterCatalog.CpuGroup).Items.Select(i => i.Id));
                Assert.Equal(new[] { MeterCatalog.MemoryGroup, MeterCatalog.GpuGroup, MeterCatalog.CpuGroup },
                    result.Groups.Take(3).Select(g => g.Id));
                Assert.Equal(MeterCatalog.CpuTotal, settings.Groups[0].Items[0].Id); // the window edits a copy
            });
        }

        [Fact]
        public void Reorder_ItemOntoAnotherGroupsItem_IsRefused()
        {
            StaRunner.Run(() =>
            {
                SettingsWindow window = Open(GadgetSettings.CreateDefault(), new List<GadgetSettings>());

                Assert.Throws<System.InvalidOperationException>(
                    () => window.Reorder(Node(window, MeterCatalog.Ram), Node(window, MeterCatalog.CpuTotal), false));
                window.Close();
            });
        }

        [Theory]
        [InlineData("abc", true)]
        [InlineData("0", true)]
        [InlineData("1000", false)]
        public void Bandwidth_InvalidInput_IsReported_ValidInputIsTaken(string typed, bool invalid)
        {
            StaRunner.Run(() =>
            {
                List<GadgetSettings> applied = new List<GadgetSettings>();
                SettingsWindow window = Open(GadgetSettings.CreateDefault(), applied);

                window.BandwidthBox.Text = typed;

                Assert.Equal(invalid, window.Problem() != null);
                if (!invalid)
                {
                    Click(Descendants<Button>(window).Single(b => (string)b.Content == "OK"));
                    Assert.Equal(1000, applied.Single().NetworkMbit);
                }
                else
                {
                    window.Close();
                }
            });
        }

        /// <summary>
        /// Regression: temperatures had two switches (the tree item and a separate checkbox); ticking the
        /// tree item alone left them off with "Turn on in Settings". The tree item is now the one switch.
        /// </summary>
        [Fact]
        public void TemperaturesItem_TickedInTheTree_TurnsTheTemperaturesOn()
        {
            StaRunner.Run(() =>
            {
                List<GadgetSettings> applied = new List<GadgetSettings>();
                SettingsWindow window = Open(GadgetSettings.CreateDefault(), applied);
                CheckBox box = Descendants<CheckBox>(window).Single(c =>
                    c.DataContext is SettingsTreeNode node && node.Id == MeterCatalog.Temperatures);

                ((IToggleProvider)new CheckBoxAutomationPeer(box).GetPattern(PatternInterface.Toggle)!).Toggle();
                Click(Descendants<Button>(window).Single(b => (string)b.Content == "OK"));

                Assert.True(applied.Single().ShowsTemperatures());
                Assert.DoesNotContain(Descendants<CheckBox>(window), c => (c.Content as string ?? string.Empty).Contains("LibreHardwareMonitor"));
            });
        }

        [Fact]
        public void TakeSwitches_ShowsTheGadgetsThinAndTopmostState()
        {
            StaRunner.Run(() =>
            {
                List<GadgetSettings> applied = new List<GadgetSettings>();
                SettingsWindow window = Open(GadgetSettings.CreateDefault(), applied);
                GadgetSettings live = GadgetSettings.CreateDefault();
                live.ThinBar = true;
                live.AlwaysOnTop = false;

                window.TakeSwitches(live);
                Click(Descendants<Button>(window).Single(b => (string)b.Content == "OK"));

                Assert.True(applied.Single().ThinBar);
                Assert.False(applied.Single().AlwaysOnTop);
            });
        }
    }
}
