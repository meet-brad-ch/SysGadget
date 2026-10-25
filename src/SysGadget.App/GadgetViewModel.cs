using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using SysGadget.Core;

namespace SysGadget.App
{
    /// <summary>What the gadget window shows: the visible groups and an error line.</summary>
    internal sealed class GadgetViewModel : ObservableObject
    {
        private string? status;
        private double sectionWidth = 224.0;

        /// <summary>The visible groups in display order.</summary>
        public ObservableCollection<GroupViewModel> Groups { get; } = new ObservableCollection<GroupViewModel>();

        /// <summary>A visible error, or null when all is well.</summary>
        public string? Status
        {
            get => status;
            set
            {
                Set(ref status, value);
                Raise(nameof(HasStatus));
            }
        }

        /// <summary>Whether the error line is shown.</summary>
        public bool HasStatus => status != null;

        /// <summary>
        /// Width of one card. The cards wrap, so they stack in a narrow window (left or right
        /// docking) and sit side by side in a wide one (top or bottom docking).
        /// </summary>
        public double SectionWidth
        {
            get => sectionWidth;
            set => Set(ref sectionWidth, value);
        }

        /// <summary>
        /// Shows a new sample. The cards are rebuilt only when the set or order of groups changes,
        /// and then get their rainbow colours by position.
        /// </summary>
        /// <param name="sections">The sampled groups.</param>
        public void Update(IReadOnlyList<GroupSection> sections)
        {
            if (!Groups.Select(g => g.Id).SequenceEqual(sections.Select(s => s.Id)))
            {
                Groups.Clear();
                for (int i = 0; i < sections.Count; i++)
                {
                    Groups.Add(new GroupViewModel(sections[i].Id, sections[i].Title, RainbowPalette.Accent(i, sections.Count)));
                }
            }

            for (int i = 0; i < sections.Count; i++)
            {
                Groups[i].Update(sections[i]);
            }
        }

        /// <summary>Finds the view of a group.</summary>
        /// <param name="groupId">The group id.</param>
        /// <returns>The view, or null when the group is not shown.</returns>
        public GroupViewModel? Find(string groupId)
        {
            return Groups.FirstOrDefault(g => g.Id == groupId);
        }
    }
}
