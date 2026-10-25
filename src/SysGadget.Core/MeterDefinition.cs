namespace SysGadget.Core
{
    /// <summary>One meter the gadget can show.</summary>
    public sealed class MeterDefinition
    {
        /// <summary>Creates a definition.</summary>
        /// <param name="id">Stable id used in the settings file.</param>
        /// <param name="title">Name shown in the settings window.</param>
        /// <param name="visibleByDefault">Whether a fresh install shows it.</param>
        public MeterDefinition(string id, string title, bool visibleByDefault)
        {
            Id = id;
            Title = title;
            VisibleByDefault = visibleByDefault;
        }

        /// <summary>Stable id used in the settings file.</summary>
        public string Id { get; }

        /// <summary>Name shown in the settings window.</summary>
        public string Title { get; }

        /// <summary>Whether a fresh install shows it.</summary>
        public bool VisibleByDefault { get; }
    }
}
