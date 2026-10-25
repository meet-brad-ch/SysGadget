using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using SysGadget.Core;

namespace SysGadget.App
{
    /// <summary>Edits a copy of the settings; the gadget changes only on OK or Apply.</summary>
    internal sealed partial class SettingsWindow : Window
    {
        /// <summary>Tag of a tree row's root element, so a pointer position can be traced back to its row.</summary>
        public const string RowTag = "SettingsRow";

        private readonly GadgetSettings working;
        private readonly Action<GadgetSettings> apply;
        private readonly Action<bool> applyAutostart;
        private List<SettingsTreeNode> tree = new List<SettingsTreeNode>();
        private SettingsTreeNode? pressed;
        private SettingsTreeNode? marked;
        private Point dragStart;

        /// <summary>Creates the window.</summary>
        /// <param name="current">The settings now in force; they are copied, not edited.</param>
        /// <param name="monitorNames">Display names of the monitors, in Windows order.</param>
        /// <param name="apply">Called with a copy of the edited settings.</param>
        /// <param name="startsWithWindows">Whether the start-with-Windows entry exists now.</param>
        /// <param name="applyAutostart">Called with the start-with-Windows choice on OK or Apply.</param>
        public SettingsWindow(
            GadgetSettings current,
            string[] monitorNames,
            Action<GadgetSettings> apply,
            bool startsWithWindows,
            Action<bool> applyAutostart)
        {
            InitializeComponent();
            this.apply = apply;
            this.applyAutostart = applyAutostart;
            AutostartBox.IsChecked = startsWithWindows;
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

        /// <summary>Puts a dragged row before or after the row it was dropped on, then redraws the tree.</summary>
        /// <param name="dragged">The node that was dragged.</param>
        /// <param name="target">The node it was dropped on.</param>
        /// <param name="after">True when dropped on the lower half of the target row.</param>
        /// <exception cref="InvalidOperationException">The target does not accept the node (another level or group).</exception>
        internal void Reorder(SettingsTreeNode dragged, SettingsTreeNode target, bool after)
        {
            if (!target.Accepts(dragged))
            {
                throw new InvalidOperationException("'" + dragged.Id + "' cannot be dropped on '" + target.Id + "'.");
            }

            if (dragged.IsGroup)
            {
                working.PlaceGroup(dragged.Id, target.Id, after);
            }
            else
            {
                working.PlaceItem(dragged.Id, target.Id, after);
            }

            RebuildTree(dragged.Id);
        }

        private void OnTreeMouseDown(object sender, MouseButtonEventArgs e)
        {
            DependencyObject? source = e.OriginalSource as DependencyObject;
            pressed = Ancestor<CheckBox>(source) == null ? RowAt(source)?.DataContext as SettingsTreeNode : null;
            dragStart = e.GetPosition(MeterTree);
        }

        private void OnTreeMouseMove(object sender, MouseEventArgs e)
        {
            if (pressed == null || e.LeftButton != MouseButtonState.Pressed)
            {
                return;
            }

            Vector moved = e.GetPosition(MeterTree) - dragStart;
            if (Math.Abs(moved.X) < SystemParameters.MinimumHorizontalDragDistance
                && Math.Abs(moved.Y) < SystemParameters.MinimumVerticalDragDistance)
            {
                return;
            }

            SettingsTreeNode dragged = pressed;
            pressed = null;
            DragDrop.DoDragDrop(MeterTree, new DataObject(typeof(SettingsTreeNode), dragged), DragDropEffects.Move);
            MarkDrop(null, DropMark.None);
        }

        private void OnTreeDragOver(object sender, DragEventArgs e)
        {
            DropPlace? place = PlaceOf(e);
            e.Effects = place == null ? DragDropEffects.None : DragDropEffects.Move;
            MarkDrop(place?.Target, place == null ? DropMark.None : place.After ? DropMark.After : DropMark.Before);
            e.Handled = true;
        }

        private void OnTreeDragLeave(object sender, DragEventArgs e)
        {
            MarkDrop(null, DropMark.None);
        }

        private void OnTreeDrop(object sender, DragEventArgs e)
        {
            DropPlace? place = PlaceOf(e);
            MarkDrop(null, DropMark.None);
            if (place != null)
            {
                Reorder(place.Dragged, place.Target, place.After);
            }

            e.Handled = true;
        }

        /// <summary>Where a drag would land, or null when the row under the pointer does not accept it.</summary>
        private static DropPlace? PlaceOf(DragEventArgs e)
        {
            FrameworkElement? row = RowAt(e.OriginalSource as DependencyObject);
            if (!(e.Data.GetData(typeof(SettingsTreeNode)) is SettingsTreeNode dragged)
                || !(row?.DataContext is SettingsTreeNode target)
                || !target.Accepts(dragged))
            {
                return null;
            }

            return new DropPlace(dragged, target, e.GetPosition(row).Y > row.ActualHeight / 2);
        }

        private void MarkDrop(SettingsTreeNode? target, DropMark mark)
        {
            if (marked != null && marked != target)
            {
                marked.DropMark = DropMark.None;
            }

            marked = target;
            if (target != null)
            {
                target.DropMark = mark;
            }
        }

        /// <summary>The row element (tagged in the template) that contains the element, or null.</summary>
        private static FrameworkElement? RowAt(DependencyObject? element)
        {
            for (DependencyObject? current = element; current != null; current = TreeParent(current))
            {
                if (current is FrameworkElement framework && Equals(framework.Tag, RowTag))
                {
                    return framework;
                }
            }

            return null;
        }

        private static T? Ancestor<T>(DependencyObject? element)
            where T : DependencyObject
        {
            for (DependencyObject? current = element; current != null; current = TreeParent(current))
            {
                if (current is T match)
                {
                    return match;
                }
            }

            return null;
        }

        private static DependencyObject? TreeParent(DependencyObject element)
        {
            // Text runs are not visuals; their parent is found through the logical tree.
            return element is Visual ? VisualTreeHelper.GetParent(element) : LogicalTreeHelper.GetParent(element);
        }

        private void OnApply(object sender, RoutedEventArgs e)
        {
            TryApply();
        }

        private void OnOk(object sender, RoutedEventArgs e)
        {
            if (TryApply())
            {
                Close();
            }
        }

        /// <summary>Applies a copy when every value is valid; otherwise says what is wrong and applies nothing.</summary>
        /// <returns>True when applied.</returns>
        private bool TryApply()
        {
            string? problem = Problem();
            if (problem != null)
            {
                MessageBox.Show(this, problem, "SysGadget settings", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            apply(working.Clone());
            try
            {
                applyAutostart(AutostartBox.IsChecked == true);
            }
            catch (Exception error) when (error is UnauthorizedAccessException || error is System.Security.SecurityException || error is System.IO.IOException)
            {
                // the registry refused the Run entry: the user is told, nothing claims success
                MessageBox.Show(this, "Start with Windows could not be changed: " + error.Message, "SysGadget settings",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        /// <summary>What is wrong with the edited values, or null.</summary>
        internal string? Problem()
        {
            if (Validation.GetHasError(BandwidthBox))
            {
                return "Bandwidth must be a whole number of Mbit/s, 1 to " + GadgetSettings.MaxNetworkMbit + ".";
            }

            try
            {
                working.Clone().Validate();
                return null;
            }
            catch (System.IO.InvalidDataException error)
            {
                return error.Message;
            }
        }

        private void OnCancel(object sender, RoutedEventArgs e)
        {
            Close(); // IsCancel alone closes only modal dialogs; this window is not modal
        }
    }
}
