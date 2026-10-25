using System.Collections.Generic;

namespace SysGadget.Core
{
    /// <summary>Visibility, collapse state and item order of one group; the list position is the display order.</summary>
    public sealed class GroupSetting
    {
        /// <summary>Group id from <see cref="MeterCatalog"/>.</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>Whether the group is shown at all.</summary>
        public bool Visible { get; set; } = true;

        /// <summary>Whether only the header is shown.</summary>
        public bool Collapsed { get; set; }

        /// <summary>The items in display order.</summary>
        public List<ItemSetting> Items { get; set; } = new List<ItemSetting>();
    }
}
