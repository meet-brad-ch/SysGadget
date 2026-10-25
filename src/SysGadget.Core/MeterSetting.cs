namespace SysGadget.Core
{
    /// <summary>Visibility of one meter; the position in the list is the display order.</summary>
    public sealed class MeterSetting
    {
        /// <summary>Meter id from <see cref="MeterCatalog"/>.</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>Whether the meter is shown. A hidden meter is not sampled.</summary>
        public bool Visible { get; set; }
    }
}
