namespace SysGadget.Core
{
    /// <summary>Load of one logical processor at one sample.</summary>
    public sealed class CoreLoad
    {
        /// <summary>Creates a sample.</summary>
        /// <param name="group">Processor group the core belongs to.</param>
        /// <param name="index">Index of the core inside its group.</param>
        /// <param name="percent">Load in percent, 0 to 100.</param>
        public CoreLoad(int group, int index, double percent)
        {
            Group = group;
            Index = index;
            Percent = percent;
        }

        /// <summary>Processor group the core belongs to.</summary>
        public int Group { get; }

        /// <summary>Index of the core inside its group.</summary>
        public int Index { get; }

        /// <summary>Load in percent, 0 to 100.</summary>
        public double Percent { get; }
    }
}
