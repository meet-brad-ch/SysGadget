using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using SysGadget.Core;

namespace SysGadget.App
{
    /// <summary>Edits a copy of the settings; the gadget changes only on OK or Apply.</summary>
    internal sealed partial class SettingsWindow : Window
    {
        private readonly GadgetSettings working;
        private readonly Action<GadgetSettings> apply;
        private List<SettingsTreeNode> tree = new List<SettingsTreeNode>();

        /// <summary>Creates the window.</summary>
        /// <param name="current">The settings now in force; they are copied, not edited.</param>
        /// <param name="monitorNames">Display names of the monitors, in Windows order.</param>
        /// <param name="apply">Called with a copy of the edited settings.</param>
        public SettingsWindow(GadgetSettings current, string[] monitorNames, Action<GadgetSettings> apply)
        {
            InitializeComponent();
            this.apply = apply;
            working = current.Clone();
            EdgeBox.ItemsSource = Enum.GetValues(typeof(DockEdge));
            MonitorBox.ItemsSource = monitorNames;
            IntervalBox.ItemsSource = new[] { 250, 500, 1000, 2000, 5000 };
            DataContext = working;
            RebuildTree(null);
            UpdateModeOptions();
        }

        /// <summary>
        /// Takes over the switches the user changed on the gadget while this window was open
        /// (double-click, menus), so OK does not set them back.
        /// </summary>
        /// <param name="live">The settings now in force.</param>
        public void TakeSwitches(GadgetSettings live)
        {
            working.ThinBar = live.ThinBar;
            working.AlwaysOnTop = live.AlwaysOnTop;
            DataContext = null; // the settings class does not notify; rebinding shows the new values
            DataContext = working;
        }

        private void RebuildTree(string? selectedId)
        {
            tree = SettingsTreeNode.Build(working, SettingsTreeNode.ClosedGroups(tree), selectedId);
            MeterTree.ItemsSource = tree;
        }

        /// <summary>Greys out the options that do not apply to the chosen position.</summary>
        private void UpdateModeOptions()
        {
            bool floating = working.Edge == DockEdge.Floating;
            ColumnsBox.IsEnabled = floating;
            ReserveBox.IsEnabled = !floating;
        }

        private void OnEdgeChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateModeOptions();
        }

        private void OnMoveUp(object sender, RoutedEventArgs e)
        {
            Move((SettingsTreeNode)((FrameworkElement)sender).DataContext, -1);
        }

        private void OnMoveDown(object sender, RoutedEventArgs e)
        {
            Move((SettingsTreeNode)((FrameworkElement)sender).DataContext, 1);
        }

        private void Move(SettingsTreeNode node, int delta)
        {
            if (node.IsGroup)
            {
                working.MoveGroup(node.Id, delta);
            }
            else
            {
                working.MoveItem(node.Id, delta);
            }

            RebuildTree(node.Id);
        }

        private void OnApply(object sender, RoutedEventArgs e)
        {
            apply(working.Clone());
        }

        private void OnOk(object sender, RoutedEventArgs e)
        {
            apply(working.Clone());
            Close();
        }

        private void OnCancel(object sender, RoutedEventArgs e)
        {
            Close(); // IsCancel alone closes only modal dialogs; this window is not modal
        }
    }
}
