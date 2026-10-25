using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using SysGadget.Core;

namespace SysGadget.App
{
    /// <summary>The view of one <see cref="GroupSection"/>: a card with a title header, rows and an optional core strip.</summary>
    internal sealed class GroupViewModel : ObservableObject
    {
        private bool isCollapsed;

        /// <summary>Creates the view of a group.</summary>
        /// <param name="id">Group id.</param>
        /// <param name="title">Heading.</param>
        public GroupViewModel(string id, string title)
        {
            Id = id;
            Title = title;
        }

        /// <summary>Group id.</summary>
        public string Id { get; }

        /// <summary>Heading.</summary>
        public string Title { get; }

        /// <summary>The line rows, updated in place so the display does not flicker.</summary>
        public ObservableCollection<RowViewModel> Rows { get; } = new ObservableCollection<RowViewModel>();

        /// <summary>The compact strip cells (cores in strip mode).</summary>
        public ObservableCollection<RowViewModel> CompactRows { get; } = new ObservableCollection<RowViewModel>();

        /// <summary>Whether only the header is shown.</summary>
        public bool IsCollapsed
        {
            get => isCollapsed;
            set
            {
                Set(ref isCollapsed, value);
                RaiseBody();
            }
        }

        /// <summary>Whether the line rows are shown.</summary>
        public bool ShowRows => !isCollapsed && Rows.Count > 0;

        /// <summary>Whether the core strip is shown.</summary>
        public bool ShowStrip => !isCollapsed && CompactRows.Count > 0;

        /// <summary>Whether anything is shown below the header.</summary>
        public bool ShowBody => ShowRows || ShowStrip;

        /// <summary>Whether the group has rows to hide, so the header offers collapsing.</summary>
        public bool CanCollapse => Rows.Count > 0 || CompactRows.Count > 0;

        /// <summary>Shows a new sample of the group.</summary>
        /// <param name="section">The sampled group.</param>
        public void Update(GroupSection section)
        {
            Sync(Rows, section.Rows.Where(r => !r.Compact).ToList());
            Sync(CompactRows, section.Rows.Where(r => r.Compact).ToList());
            IsCollapsed = section.Collapsed;
            RaiseBody();
        }

        private static void Sync(ObservableCollection<RowViewModel> target, IReadOnlyList<MeterRow> rows)
        {
            while (target.Count > rows.Count)
            {
                target.RemoveAt(target.Count - 1);
            }

            while (target.Count < rows.Count)
            {
                target.Add(new RowViewModel());
            }

            for (int i = 0; i < rows.Count; i++)
            {
                target[i].Update(rows[i]);
            }
        }

        private void RaiseBody()
        {
            Raise(nameof(ShowRows));
            Raise(nameof(ShowStrip));
            Raise(nameof(ShowBody));
            Raise(nameof(CanCollapse));
        }
    }
}
