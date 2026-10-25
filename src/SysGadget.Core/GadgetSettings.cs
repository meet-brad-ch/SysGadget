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

        /// <summary>Largest allowed network bandwidth in Mbit/s.</summary>
        public const int MaxNetworkMbit = 100000;

        /// <summary>
        /// Bandwidth of the internet connection in Mbit/s: a full Down or Up bar. Faster moments
        /// keep the bar full.
        /// </summary>
        public int NetworkMbit { get; set; } = 500;

        /// <summary>Edge to dock to.</summary>
        public DockEdge Edge { get; set; } = DockEdge.Floating;

        /// <summary>
        /// Floating window: the groups sit side by side as columns (true) or stacked (false).
        /// Left and right docking always stack them; top and bottom docking always put them side by side.
        /// </summary>
        public bool Horizontal { get; set; } = true;

        /// <summary>Zero-based index of the monitor, in the order Windows lists them.</summary>
        public int MonitorIndex { get; set; }

        /// <summary>Left position of a floating window in screen pixels; null places it at the top right.</summary>
        public double? FloatingLeft { get; set; }

        /// <summary>Top position of a floating window in screen pixels; null places it at the top right.</summary>
        public double? FloatingTop { get; set; }

        /// <summary>
        /// Reserve the docked edge like the taskbar does, so maximised windows stop at the gadget.
        /// Windows reserves the full edge, so the gadget then fills it top to bottom. Off: the gadget
        /// sits against the edge, only as tall as its content, over other windows.
        /// </summary>
        public bool ReserveSpace { get; set; }

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

        /// <summary>
        /// Whether the LibreHardwareMonitor temperatures are shown (group and item ticked). This is the
        /// one switch for them: showing them needs administrator rights.
        /// </summary>
        /// <returns>True when the temperatures are to be shown.</returns>
        public bool ShowsTemperatures()
        {
            return IsGroupVisible(MeterCatalog.SensorsGroup) && IsItemVisible(MeterCatalog.Temperatures);
        }

        /// <summary>
        /// Thin bar: each group shows one item, on one line with the group title: the item chosen for
        /// the group (<see cref="SetThinItem"/>), else its first ticked item.
        /// </summary>
        public bool ThinBar { get; set; }

        /// <summary>Show the cores as a compact strip of thin bars instead of one row per core.</summary>
        public bool UseCompactCores { get; set; } = true;

        /// <summary>Groups in display order, each with its items.</summary>
        public List<GroupSetting> Groups { get; set; } = new List<GroupSetting>();

        /// <summary>Creates the settings of a fresh install.</summary>
        /// <returns>
        /// Defaults with every catalogue group and item present; per-instance items get their entries
        /// when the instances are found (<see cref="AddInstances"/>).
        /// </returns>
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
                ThinItem = g.ThinItem,
                Items = g.Items.Select(i => new ItemSetting { Id = i.Id, Instance = i.Instance, Visible = i.Visible }).ToList(),
            }).ToList();
            return copy;
        }

        /// <summary>
        /// Adds an entry for every found instance that has none yet, at the end of its group, shown or
        /// hidden as the item's default says.
        /// </summary>
        /// <param name="found">The instances found on this machine now.</param>
        /// <returns>True when an entry was added.</returns>
        public bool AddInstances(IEnumerable<InstanceRef> found)
        {
            bool added = false;
            foreach (InstanceRef instance in found)
            {
                GroupSetting group = FindGroup(MeterCatalog.GroupOf(instance.ItemId).Id);
                string key = ItemSetting.MakeKey(instance.ItemId, instance.Instance);
                if (!group.Items.Any(i => i.Key == key))
                {
                    group.Items.Add(new ItemSetting
                    {
                        Id = instance.ItemId,
                        Instance = instance.Instance,
                        Visible = MeterCatalog.GetItem(instance.ItemId).VisibleByDefault,
                    });
                    added = true;
                }
            }

            return added;
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

        /// <summary>Whether an entry is shown, by its own setting (the group may still be hidden).</summary>
        /// <param name="key">The entry key: an item id, or "id|instance" for one disk.</param>
        /// <returns>True when shown.</returns>
        public bool IsItemVisible(string key)
        {
            return FindItem(key).Visible;
        }

        /// <summary>Shows or hides an entry.</summary>
        /// <param name="key">The entry key: an item id, or "id|instance" for one disk.</param>
        /// <param name="visible">The new state.</param>
        public void SetItemVisible(string key, bool visible)
        {
            FindItem(key).Visible = visible;
            GroupSetting group = GroupOfKey(key);
            if (!visible && group.ThinItem == key)
            {
                group.ThinItem = string.Empty; // a hidden entry is not shown anywhere; the first ticked one takes over
            }
        }

        /// <summary>The entry the thin bar shows for a group.</summary>
        /// <param name="groupId">The group id.</param>
        /// <returns>The chosen entry, else the first ticked one; null when none is ticked.</returns>
        public ItemSetting? ThinEntry(string groupId)
        {
            return FindGroup(groupId).ThinEntry();
        }

        /// <summary>Makes an entry the one the thin bar shows for its group, and ticks it.</summary>
        /// <param name="key">The entry key: an item id, or "id|instance" for one disk.</param>
        public void SetThinItem(string key)
        {
            FindItem(key).Visible = true;
            GroupOfKey(key).ThinItem = key;
        }

        /// <summary>Puts a group right before or after another group (a drag and drop).</summary>
        /// <param name="groupId">The group to move.</param>
        /// <param name="targetGroupId">The group it is dropped on.</param>
        /// <param name="after">True places it after the target, false before.</param>
        public void PlaceGroup(string groupId, string targetGroupId, bool after)
        {
            Place(Groups, FindGroup(groupId), FindGroup(targetGroupId), after);
        }

        /// <summary>Puts an entry right before or after another entry of the same group (a drag and drop).</summary>
        /// <param name="key">The entry to move.</param>
        /// <param name="targetKey">The entry it is dropped on.</param>
        /// <param name="after">True places it after the target, false before.</param>
        /// <exception cref="InvalidOperationException">The entries belong to different groups.</exception>
        public void PlaceItem(string key, string targetKey, bool after)
        {
            string groupId = MeterCatalog.GroupOf(ItemSetting.ItemIdOf(key)).Id;
            if (MeterCatalog.GroupOf(ItemSetting.ItemIdOf(targetKey)).Id != groupId)
            {
                throw new InvalidOperationException("Item '" + key + "' cannot move into another group than '" + groupId + "'.");
            }

            Place(FindGroup(groupId).Items, FindItem(key), FindItem(targetKey), after);
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
            Require(NetworkMbit >= 1 && NetworkMbit <= MaxNetworkMbit, "Network bandwidth must be 1 to " + MaxNetworkMbit + " Mbit/s.");
            DropRetiredEntries();
            ValidateGroups();
            AddMissing();
        }

        /// <summary>
        /// Older files hold entries of items that were replaced (<see cref="MeterCatalog.RetiredItemIds"/>,
        /// for example one "storage.disks" entry per disk before read, write and free were split); they
        /// are dropped, and the disks get their new entries when found.
        /// </summary>
        private void DropRetiredEntries()
        {
            foreach (GroupSetting group in Groups)
            {
                group.Items.RemoveAll(i => MeterCatalog.RetiredItemIds.Contains(i.Id));
                if (MeterCatalog.RetiredItemIds.Contains(ItemSetting.ItemIdOf(group.ThinItem)))
                {
                    group.ThinItem = string.Empty;
                }
            }
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
                ItemDefinition? itemDefinition = definition.Items.FirstOrDefault(d => d.Id == item.Id);
                Require(itemDefinition != null, "Item '" + item.Id + "' does not belong to group '" + group.Id + "'.");
                Require(itemDefinition!.PerInstance == (item.Instance.Length > 0),
                    itemDefinition.PerInstance
                        ? "Item '" + item.Id + "' needs an instance."
                        : "Item '" + item.Id + "' has no instances, but '" + item.Instance + "' is given.");
                Require(seen.Add(item.Key), "Item '" + item.Key + "' is listed twice.");
            }

            Require(group.ThinItem.Length == 0 || group.Items.Any(i => i.Key == group.ThinItem && i.Visible),
                "Thin bar item '" + group.ThinItem + "' of group '" + group.Id + "' is not a ticked item of the group.");
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

                foreach (ItemDefinition item in definition.Items.Where(d => !d.PerInstance))
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
                Items = definition.Items
                    .Where(i => !i.PerInstance)
                    .Select(i => new ItemSetting { Id = i.Id, Visible = i.VisibleByDefault })
                    .ToList(),
            };
        }

        private GroupSetting FindGroup(string groupId)
        {
            GroupSetting? group = Groups.FirstOrDefault(g => string.Equals(g.Id, groupId, StringComparison.Ordinal));
            return group ?? throw new KeyNotFoundException("Group '" + groupId + "' is not in the settings.");
        }

        private GroupSetting GroupOfKey(string key)
        {
            return FindGroup(MeterCatalog.GroupOf(ItemSetting.ItemIdOf(key)).Id);
        }

        private ItemSetting FindItem(string key)
        {
            GroupSetting group = GroupOfKey(key);
            ItemSetting? item = group.Items.FirstOrDefault(i => string.Equals(i.Key, key, StringComparison.Ordinal));
            return item ?? throw new KeyNotFoundException("Item '" + key + "' is not in the settings.");
        }

        private static void Place<T>(List<T> list, T entry, T target, bool after)
            where T : class
        {
            if (ReferenceEquals(entry, target))
            {
                return; // dropped on itself
            }

            list.Remove(entry);
            int index = list.IndexOf(target);
            list.Insert(after ? index + 1 : index, entry);
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
