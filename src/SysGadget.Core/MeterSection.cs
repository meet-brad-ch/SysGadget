using System.Collections.Generic;

namespace SysGadget.Core
{
    /// <summary>A titled group of rows: what one visible meter shows.</summary>
    public sealed class MeterSection
    {
        /// <summary>Creates a section.</summary>
        /// <param name="id">Meter id from <see cref="MeterCatalog"/>.</param>
        /// <param name="title">Heading shown above the rows.</param>
        /// <param name="rows">The rows.</param>
        public MeterSection(string id, string title, IReadOnlyList<MeterRow> rows)
        {
            Id = id;
            Title = title;
            Rows = rows;
        }

        /// <summary>Meter id from <see cref="MeterCatalog"/>.</summary>
        public string Id { get; }

        /// <summary>Heading shown above the rows.</summary>
        public string Title { get; }

        /// <summary>The rows.</summary>
        public IReadOnlyList<MeterRow> Rows { get; }
    }
}
