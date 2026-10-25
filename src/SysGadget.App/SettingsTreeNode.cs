using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using SysGadget.Core;

namespace SysGadget.App
{
    /// <summary>
    /// One node of the settings tree: a group or an item. Its checkbox writes straight into the
    /// working copy of the settings; titles come from the catalogue.
    /// </summary>
    internal sealed class SettingsTreeNode : ObservableObject
    {
        private readonly GadgetSettings working;
        private readonly SettingsTreeNode? parent;
        private bool isExpanded = true;
        private bool isSelected;
        private DropMark dropMark;

        private SettingsTreeNode(GadgetSettings working, string id, string title, bool isGroup, SettingsTreeNode? parent)
        {
            this.working = working;
            this.parent = parent;
            Id = id;
            Title = title;
            IsGroup = isGroup;
        }

        /// <summary>
        /// True for the first checked item of its group: the item the thin bar shows. Moving or
        /// checking items changes it, so the tree can mark it.
        /// </summary>
        public bool IsFirstShown => parent != null && IsChecked && parent.Children.FirstOrDefault(c => c.IsChecked) == this;

        /// <summary>Group or item id.</summary>
        public string Id { get; }

        /// <summary>Name from the catalogue.</summary>
        public string Title { get; }

        /// <summary>True for a group node, false for an item node.</summary>
        public bool IsGroup { get; }

        /// <summary>The item nodes of a group node; empty for an item node.</summary>
        public ObservableCollection<SettingsTreeNode> Children { get; } = new ObservableCollection<SettingsTreeNode>();

        /// <summary>Whether the group or item is shown.</summary>
        public bool IsChecked
        {
            get => IsGroup ? working.IsGroupVisible(Id) : working.IsItemVisible(Id);
            set
            {
                if (IsGroup)
                {
                    working.SetGroupVisible(Id, value);
                }
                else
                {
                    working.SetItemVisible(Id, value);
                }

                Raise(nameof(IsChecked));
                if (parent != null)
                {
                    foreach (SettingsTreeNode sibling in parent.Children)
                    {
                        sibling.Raise(nameof(IsFirstShown));
                    }
                }
            }
        }

        /// <summary>Where a row being dragged over this one would land; drawn as a line.</summary>
        public DropMark DropMark
        {
            get => dropMark;
            set => Set(ref dropMark, value);
        }

        /// <summary>Whether a dragged node may be dropped on this one: groups among groups, items inside their own group.</summary>
        /// <param name="dragged">The node being dragged.</param>
        /// <returns>True when the drop reorders something.</returns>
        public bool Accepts(SettingsTreeNode dragged)
        {
            return dragged != this && dragged.IsGroup == IsGroup && dragged.parent == parent;
        }

        /// <summary>Whether a group node shows its items.</summary>
        public bool IsExpanded
        {
            get => isExpanded;
            set => Set(ref isExpanded, value);
        }

        /// <summary>Whether the node is selected in the tree.</summary>
        public bool IsSelected
        {
            get => isSelected;
            set => Set(ref isSelected, value);
        }

        /// <summary>Builds the tree in the current display order.</summary>
        /// <param name="working">The settings the checkboxes edit.</param>
        /// <param name="collapsedGroupIds">Groups whose nodes start closed.</param>
        /// <param name="selectedId">Id of the node to select, or null.</param>
        /// <returns>One node per group, each with its item nodes.</returns>
        public static List<SettingsTreeNode> Build(GadgetSettings working, ISet<string> collapsedGroupIds, string? selectedId)
        {
            List<SettingsTreeNode> groups = new List<SettingsTreeNode>();
            foreach (GroupSetting group in working.Groups)
            {
                SettingsTreeNode node = new SettingsTreeNode(working, group.Id, MeterCatalog.GetGroup(group.Id).Title, true, null)
                {
                    IsExpanded = !collapsedGroupIds.Contains(group.Id),
                    IsSelected = group.Id == selectedId,
                };
                foreach (ItemSetting item in group.Items)
                {
                    node.Children.Add(new SettingsTreeNode(working, item.Id, MeterCatalog.GetItem(item.Id).Title, false, node)
                    {
                        IsSelected = item.Id == selectedId,
                    });
                }

                groups.Add(node);
            }

            return groups;
        }

        /// <summary>The ids of the group nodes that are closed.</summary>
        /// <param name="groups">The tree.</param>
        /// <returns>The closed group ids.</returns>
        public static HashSet<string> ClosedGroups(IEnumerable<SettingsTreeNode> groups)
        {
            return new HashSet<string>(groups.Where(g => !g.IsExpanded).Select(g => g.Id));
        }
    }
}
