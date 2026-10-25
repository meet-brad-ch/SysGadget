using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
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
        }

        private void RebuildTree(string? selectedId)
        {
            tree = SettingsTreeNode.Build(working, SettingsTreeNode.ClosedGroups(tree), selectedId);
            MeterTree.ItemsSource = tree;
        }

        private void OnMoveUp(object sender, RoutedEventArgs e)
        {
            MoveSelected(-1);
        }

        private void OnMoveDown(object sender, RoutedEventArgs e)
        {
            MoveSelected(1);
        }

        private void MoveSelected(int delta)
        {
            if (!(MeterTree.SelectedItem is SettingsTreeNode selected))
            {
                return;
            }

            if (selected.IsGroup)
            {
                working.MoveGroup(selected.Id, delta);
            }
            else
            {
                working.MoveItem(selected.Id, delta);
            }

            RebuildTree(selected.Id);
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
    }
}
