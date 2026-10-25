using System;
using System.Collections.Generic;
using System.Linq;

namespace SysGadget.Core
{
    /// <summary>Visibility and item order of one group; the list position is the display order.</summary>
    public sealed class GroupSetting
    {
        /// <summary>Group id from <see cref="MeterCatalog"/>.</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>Whether the group is shown at all.</summary>
        public bool Visible { get; set; } = true;

        /// <summary>The items in display order.</summary>
        public List<ItemSetting> Items { get; set; } = new List<ItemSetting>();

        /// <summary>
        /// Key of the entry the thin bar shows, chosen by the user; empty shows the first ticked entry.
        /// The chosen entry is always ticked: unticking it clears the choice.
        /// </summary>
        public string ThinItem { get; set; } = string.Empty;

        /// <summary>The entry the thin bar shows: the chosen one, else the first ticked one.</summary>
        /// <returns>The entry, or null when no entry is ticked.</returns>
        /// <exception cref="InvalidOperationException">The chosen key is not an entry of this group.</exception>
        public ItemSetting? ThinEntry()
        {
            if (ThinItem.Length == 0)
            {
                return Items.FirstOrDefault(i => i.Visible);
            }

            return Items.FirstOrDefault(i => i.Key == ThinItem)
                ?? throw new InvalidOperationException("Thin bar item '" + ThinItem + "' is not in group '" + Id + "'.");
        }
    }
}
