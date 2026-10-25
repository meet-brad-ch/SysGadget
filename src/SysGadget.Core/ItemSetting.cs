namespace SysGadget.Core
{
    /// <summary>Visibility of one item; the position in its group's list is the display order.</summary>
    public sealed class ItemSetting
    {
        /// <summary>Item id from <see cref="MeterCatalog"/>.</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>Whether the item is shown. A hidden item is not sampled.</summary>
        public bool Visible { get; set; }
    }
}
