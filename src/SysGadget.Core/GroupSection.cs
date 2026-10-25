using System.Collections.Generic;

namespace SysGadget.Core
{
    /// <summary>What one visible group shows: a titled header and the rows of its visible items.</summary>
    public sealed class GroupSection
    {
        /// <summary>Creates a section.</summary>
        /// <param name="id">Group id from <see cref="MeterCatalog"/>.</param>
        /// <param name="title">Heading.</param>
        /// <param name="collapsed">Whether only the header is shown.</param>
        /// <param name="rows">The rows of the visible items, in display order.</param>
        public GroupSection(string id, string title, bool collapsed, IReadOnlyList<MeterRow> rows)
        {
            Id = id;
            Title = title;
            Collapsed = collapsed;
            Rows = rows;
        }

        /// <summary>Group id from <see cref="MeterCatalog"/>.</summary>
        public string Id { get; }

        /// <summary>Heading.</summary>
        public string Title { get; }

        /// <summary>Whether only the header is shown.</summary>
        public bool Collapsed { get; }

        /// <summary>The rows of the visible items, in display order.</summary>
        public IReadOnlyList<MeterRow> Rows { get; }
    }
}
