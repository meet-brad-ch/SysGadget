namespace SysGadget.Core
{
    /// <summary>One element inside a group, for example "Total" in the CPU group. Pure data.</summary>
    public sealed class ItemDefinition
    {
        /// <summary>Creates a definition.</summary>
        /// <param name="id">Stable id used in the settings file, for example "cpu.cores".</param>
        /// <param name="title">Name shown in the settings window.</param>
        /// <param name="sourceId">Id of the sample source the item reads from.</param>
        /// <param name="visibleByDefault">Whether a fresh install shows it.</param>
        public ItemDefinition(string id, string title, string sourceId, bool visibleByDefault)
        {
            Id = id;
            Title = title;
            SourceId = sourceId;
            VisibleByDefault = visibleByDefault;
        }

        /// <summary>Stable id used in the settings file.</summary>
        public string Id { get; }

        /// <summary>Name shown in the settings window.</summary>
        public string Title { get; }

        /// <summary>Id of the sample source the item reads from.</summary>
        public string SourceId { get; }

        /// <summary>Whether a fresh install shows it.</summary>
        public bool VisibleByDefault { get; }
    }
}
