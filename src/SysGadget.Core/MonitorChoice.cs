namespace SysGadget.Core
{
    /// <summary>The monitor the gadget is placed on.</summary>
    public sealed class MonitorChoice
    {
        /// <summary>Creates a choice.</summary>
        /// <param name="index">The zero-based monitor number to use.</param>
        /// <param name="savedMissing">Whether the saved monitor is not connected, so the primary one is used instead.</param>
        public MonitorChoice(int index, bool savedMissing)
        {
            Index = index;
            SavedMissing = savedMissing;
        }

        /// <summary>The zero-based monitor number to use.</summary>
        public int Index { get; }

        /// <summary>Whether the saved monitor is not connected, so the primary one is used instead.</summary>
        public bool SavedMissing { get; }
    }
}
