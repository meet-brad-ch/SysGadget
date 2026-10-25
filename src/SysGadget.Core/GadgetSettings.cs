using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SysGadget.Core
{
    /// <summary>All user settings of the gadget.</summary>
    public sealed class GadgetSettings
    {
        /// <summary>Smallest allowed width: label, the bar at its minimum and the value still fit.</summary>
        public const int MinWidth = 200;

        /// <summary>Largest allowed width in device-independent pixels.</summary>
        public const int MaxWidth = 800;

        /// <summary>Smallest allowed update interval.</summary>
        public const int MinIntervalMs = 250;

        /// <summary>Largest allowed update interval.</summary>
        public const int MaxIntervalMs = 10000;

        /// <summary>Smallest allowed opacity.</summary>
        public const double MinOpacity = 0.2;

        /// <summary>Edge to dock to.</summary>
        public DockEdge Edge { get; set; } = DockEdge.Right;

        /// <summary>Zero-based index of the monitor, in the order Windows lists them.</summary>
        public int MonitorIndex { get; set; }

        /// <summary>Left position of a floating window in screen pixels; null places it at the top right.</summary>
        public double? FloatingLeft { get; set; }

        /// <summary>Top position of a floating window in screen pixels; null places it at the top right.</summary>
        public double? FloatingTop { get; set; }

        /// <summary>Keep the gadget above other windows.</summary>
        public bool AlwaysOnTop { get; set; } = true;

        /// <summary>
        /// Window width in device-independent pixels for left, right and floating; the width of
        /// each card for top and bottom docking.
        /// </summary>
        public int Width { get; set; } = 240;

        /// <summary>Window opacity, <see cref="MinOpacity"/> to 1.</summary>
        public double Opacity { get; set; } = 0.92;

        /// <summary>Time between samples.</summary>
        public int IntervalMs { get; set; } = 1000;

        /// <summary>Use LibreHardwareMonitor for temperatures and power; asks for admin rights.</summary>
        public bool UseLibreHardwareMonitor { get; set; }

        /// <summary>Show the cores as a compact strip of thin bars instead of one row per core.</summary>
        public bool UseCompactCores { get; set; }

        /// <summary>Groups in display order, each with its items.</summary>
        public List<GroupSetting> Groups { get; set; } = new List<GroupSetting>();

        /// <summary>Creates the settings of a fresh install.</summary>
        /// <returns>Defaults with every catalogue group and item present.</returns>
        public static GadgetSettings CreateDefault()
        {
            GadgetSettings settings = new GadgetSettings();
            foreach (GroupDefinition group in MeterCatalog.Groups)
            {
                settings.Groups.Add(DefaultGroup(group));
            }

            return settings;
        }

        /// <summary>Makes an independent copy, so edits can be applied as a whole.</summary>
        /// <returns>The copy.</returns>
        public GadgetSettings Clone()
        {
            GadgetSettings copy = (GadgetSettings)MemberwiseClone();
            copy.Groups = Groups.Select(g => new GroupSetting
            {
                Id = g.Id,
                Visible = g.Visible,
                Collapsed = g.Collapsed,
                Items = g.Items.Select(i => new ItemSetting { Id = i.Id, Visible = i.Visible }).ToList(),
            }).ToList();
            return copy;
        }

        /// <summary>Whether a group is shown.</summary>
        /// <param name="groupId">The group id.</param>
        /// <returns>True when shown.</returns>
        public bool IsGroupVisible(string groupId)
        {
            return FindGroup(groupId).Visible;
        }

        /// <summary>Shows or hides a whole group; its items keep their own state.</summary>
        /// <param name="groupId">The group id.</param>
        /// <param name="visible">The new state.</param>
        public void SetGroupVisible(string groupId, bool visible)
        {
            FindGroup(groupId).Visible = visible;
        }

        /// <summary>Whether an item is shown, by its own setting (the group may still be hidden).</summary>
        /// <param name="itemId">The item id.</param>
        /// <returns>True when shown.</returns>
        public bool IsItemVisible(string itemId)
        {
            return FindItem(itemId).Visible;
        }

        /// <summary>Shows or hides an item.</summary>
        /// <param name="itemId">The item id.</param>
        /// <param name="visible">The new state.</param>
        public void SetItemVisible(string itemId, bool visible)
        {
            FindItem(itemId).Visible = visible;
        }

        /// <summary>Whether a group shows only its header.</summary>
        /// <param name="groupId">The group id.</param>
        /// <returns>True when collapsed.</returns>
        public bool IsCollapsed(string groupId)
        {
            return FindGroup(groupId).Collapsed;
        }

        /// <summary>Collapses or expands a group.</summary>
        /// <param name="groupId">The group id.</param>
        /// <param name="collapsed">The new state.</param>
        public void SetCollapsed(string groupId, bool collapsed)
        {
            FindGroup(groupId).Collapsed = collapsed;
        }

        /// <summary>Moves a group up or down; a move past either end is ignored.</summary>
        /// <param name="groupId">The group id.</param>
        /// <param name="delta">-1 moves it up, +1 down.</param>
        public void MoveGroup(string groupId, int delta)
        {
            Move(Groups, FindGroup(groupId), delta);
        }

        /// <summary>Moves an item up or down inside its group; a move past either end is ignored.</summary>
        /// <param name="itemId">The item id.</param>
        /// <param name="delta">-1 moves it up, +1 down.</param>
        public void MoveItem(string itemId, int delta)
        {
            GroupSetting group = FindGroup(MeterCatalog.GroupOf(itemId).Id);
            Move(group.Items, FindItem(itemId), delta);
        }

        /// <summary>
        /// Checks the values and adds catalogue groups and items that an older settings file lacks
        /// (appended at the end with their defaults).
        /// </summary>
        /// <exception cref="InvalidDataException">A value is out of range, or an id is unknown, repeated or in the wrong group.</exception>
        public void Validate()
        {
            Require(Width >= MinWidth && Width <= MaxWidth, "Width must be " + MinWidth + " to " + MaxWidth + ".");
            Require(IntervalMs >= MinIntervalMs && IntervalMs <= MaxIntervalMs,
                "IntervalMs must be " + MinIntervalMs + " to " + MaxIntervalMs + ".");
            Require(Opacity >= MinOpacity && Opacity <= 1.0, "Opacity must be " + MinOpacity + " to 1.");
            Require(MonitorIndex >= 0, "MonitorIndex must not be negative.");
            ValidateGroups();
            AddMissing();
        }

        private void ValidateGroups()
        {
            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (GroupSetting group in Groups)
            {
                Require(MeterCatalog.Groups.Any(d => d.Id == group.Id), "Unknown group id '" + group.Id + "'.");
                Require(seen.Add(group.Id), "Group '" + group.Id + "' is listed twice.");
                ValidateItems(group);
            }
        }

        private static void ValidateItems(GroupSetting group)
        {
            GroupDefinition definition = MeterCatalog.GetGroup(group.Id);
            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (ItemSetting item in group.Items)
            {
                Require(definition.Items.Any(d => d.Id == item.Id),
                    "Item '" + item.Id + "' does not belong to group '" + group.Id + "'.");
                Require(seen.Add(item.Id), "Item '" + item.Id + "' is listed twice.");
            }
        }

        private void AddMissing()
        {
            foreach (GroupDefinition definition in MeterCatalog.Groups)
            {
                GroupSetting? group = Groups.FirstOrDefault(g => g.Id == definition.Id);
                if (group == null)
                {
                    Groups.Add(DefaultGroup(definition));
                    continue;
                }

                foreach (ItemDefinition item in definition.Items)
                {
                    if (!group.Items.Any(i => i.Id == item.Id))
                    {
                        group.Items.Add(new ItemSetting { Id = item.Id, Visible = item.VisibleByDefault });
                    }
                }
            }
        }

        private static GroupSetting DefaultGroup(GroupDefinition definition)
        {
            return new GroupSetting
            {
                Id = definition.Id,
                Visible = true,
                Items = definition.Items.Select(i => new ItemSetting { Id = i.Id, Visible = i.VisibleByDefault }).ToList(),
            };
        }

        private GroupSetting FindGroup(string groupId)
        {
            GroupSetting? group = Groups.FirstOrDefault(g => string.Equals(g.Id, groupId, StringComparison.Ordinal));
            return group ?? throw new KeyNotFoundException("Group '" + groupId + "' is not in the settings.");
        }

        private ItemSetting FindItem(string itemId)
        {
            GroupSetting group = FindGroup(MeterCatalog.GroupOf(itemId).Id);
            ItemSetting? item = group.Items.FirstOrDefault(i => string.Equals(i.Id, itemId, StringComparison.Ordinal));
            return item ?? throw new KeyNotFoundException("Item '" + itemId + "' is not in the settings.");
        }

        private static void Move<T>(List<T> list, T entry, int delta)
        {
            int from = list.IndexOf(entry);
            int to = from + delta;
            if (to < 0 || to >= list.Count)
            {
                return;
            }

            list.RemoveAt(from);
            list.Insert(to, entry);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidDataException(message);
            }
        }
    }
}
