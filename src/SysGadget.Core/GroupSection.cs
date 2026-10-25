using System.Collections.Generic;

namespace SysGadget.Core
{
    /// <summary>What one visible group shows: a title and the rows of its shown items.</summary>
    public sealed class GroupSection
    {
        /// <summary>Creates a section.</summary>
        /// <param name="id">Group id from <see cref="MeterCatalog"/>.</param>
        /// <param name="title">Heading.</param>
        /// <param name="rows">The rows of the shown items, in display order.</param>
        public GroupSection(string id, string title, IReadOnlyList<MeterRow> rows)
        {
            Id = id;
            Title = title;
            Rows = rows;
        }

        /// <summary>Group id from <see cref="MeterCatalog"/>.</summary>
        public string Id { get; }

        /// <summary>Heading.</summary>
        public string Title { get; }

        /// <summary>The rows of the shown items, in display order.</summary>
        public IReadOnlyList<MeterRow> Rows { get; }
    }
}
