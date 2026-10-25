using System.Collections.Generic;

namespace SysGadget.Core
{
    /// <summary>A section of the gadget, for example CPU, with its items. Pure data.</summary>
    public sealed class GroupDefinition
    {
        /// <summary>Creates a definition.</summary>
        /// <param name="id">Stable id used in the settings file.</param>
        /// <param name="title">Heading of the section.</param>
        /// <param name="items">The items, in default display order.</param>
        public GroupDefinition(string id, string title, IReadOnlyList<ItemDefinition> items)
        {
            Id = id;
            Title = title;
            Items = items;
        }

        /// <summary>Stable id used in the settings file.</summary>
        public string Id { get; }

        /// <summary>Heading of the section.</summary>
        public string Title { get; }

        /// <summary>The items, in default display order.</summary>
        public IReadOnlyList<ItemDefinition> Items { get; }
    }
}
