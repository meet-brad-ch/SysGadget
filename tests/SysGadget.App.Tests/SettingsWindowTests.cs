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

        private static Button ButtonOf(Window window, string nodeId, string content)
        {
            return Descendants<Button>(window).Single(b =>
                b.DataContext is SettingsTreeNode node && node.Id == nodeId && (string)b.Content == content);
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
        public void MoveUp_OnAnItemRow_ReordersItsGroup_AndOkAppliesTheOrder()
        {
            StaRunner.Run(() =>
            {
                List<GadgetSettings> applied = new List<GadgetSettings>();
                GadgetSettings settings = GadgetSettings.CreateDefault();
                SettingsWindow window = Open(settings, applied);

                Click(ButtonOf(window, MeterCatalog.CpuCores, "▲"));
                Click(Descendants<Button>(window).Single(b => (string)b.Content == "OK"));

                GroupSetting cpu = applied.Single().Groups.Single(g => g.Id == MeterCatalog.CpuGroup);
                Assert.Equal(new[] { MeterCatalog.CpuCores, MeterCatalog.CpuTotal }, cpu.Items.Select(i => i.Id));
                Assert.Equal(MeterCatalog.CpuTotal, settings.Groups[0].Items[0].Id); // the window edits a copy
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
