using System.Collections.Generic;
using System.Collections.ObjectModel;
using SysGadget.Core;

namespace SysGadget.App
{
    /// <summary>The view of one <see cref="MeterSection"/>.</summary>
    internal sealed class SectionViewModel : ObservableObject
    {
        /// <summary>Creates the view of a section.</summary>
        /// <param name="id">Meter id.</param>
        /// <param name="title">Heading.</param>
        public SectionViewModel(string id, string title)
        {
            Id = id;
            Title = title;
        }

        /// <summary>Meter id.</summary>
        public string Id { get; }

        /// <summary>Heading.</summary>
        public string Title { get; }

        /// <summary>The rows, updated in place so the display does not flicker.</summary>
        public ObservableCollection<RowViewModel> Rows { get; } = new ObservableCollection<RowViewModel>();

        /// <summary>Adjusts the rows to a new sample.</summary>
        /// <param name="rows">The rows to show.</param>
        public void Update(IReadOnlyList<MeterRow> rows)
        {
            while (Rows.Count > rows.Count)
            {
                Rows.RemoveAt(Rows.Count - 1);
            }

            while (Rows.Count < rows.Count)
            {
                Rows.Add(new RowViewModel());
            }

            for (int i = 0; i < rows.Count; i++)
            {
                Rows[i].Update(rows[i]);
            }
        }
    }
}
