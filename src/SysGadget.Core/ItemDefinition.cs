namespace SysGadget.Core
{
    /// <summary>One element inside a group, for example "Total" in the CPU group. Pure data.</summary>
    public sealed class ItemDefinition
    {
        /// <summary>Creates a definition.</summary>
        /// <param name="id">Stable id used in the settings file, for example "cpu.cores".</param>
        /// <param name="title">Name shown in the settings window.</param>
        /// <param name="sourceId">Id of the sample source the item reads from.</param>
        /// <param name="visibleByDefault">Whether a fresh install (or a newly found instance) shows it.</param>
        /// <param name="perInstance">
        /// True when the item repeats per hardware instance (one entry per disk); the instances are
        /// found at run time and each gets its own settings entry.
        /// </param>
        public ItemDefinition(string id, string title, string sourceId, bool visibleByDefault, bool perInstance = false)
        {
            Id = id;
            Title = title;
            SourceId = sourceId;
            VisibleByDefault = visibleByDefault;
            PerInstance = perInstance;
        }

        /// <summary>Stable id used in the settings file.</summary>
        public string Id { get; }

        /// <summary>Name shown in the settings window.</summary>
        public string Title { get; }

        /// <summary>Id of the sample source the item reads from.</summary>
        public string SourceId { get; }

        /// <summary>Whether a fresh install (or a newly found instance) shows it.</summary>
        public bool VisibleByDefault { get; }

        /// <summary>True when the item repeats per hardware instance, each with its own settings entry.</summary>
        public bool PerInstance { get; }
    }
}
