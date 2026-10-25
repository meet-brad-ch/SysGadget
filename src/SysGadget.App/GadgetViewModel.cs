using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using SysGadget.Core;

namespace SysGadget.App
{
    /// <summary>What the gadget window shows: the visible sections and an error line.</summary>
    internal sealed class GadgetViewModel : ObservableObject
    {
        private string? status;
        private double sectionWidth = 204.0;

        /// <summary>The visible meters in display order.</summary>
        public ObservableCollection<SectionViewModel> Sections { get; } = new ObservableCollection<SectionViewModel>();

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
        /// Width of one section. The sections wrap, so they stack in a narrow window (left or
        /// right docking) and sit side by side in a wide one (top or bottom docking).
        /// </summary>
        public double SectionWidth
        {
            get => sectionWidth;
            set => Set(ref sectionWidth, value);
        }

        /// <summary>Shows a new sample.</summary>
        /// <param name="sections">The sections to show.</param>
        public void Update(IReadOnlyList<MeterSection> sections)
        {
            if (!Sections.Select(s => s.Id).SequenceEqual(sections.Select(s => s.Id)))
            {
                Sections.Clear();
                foreach (MeterSection section in sections)
                {
                    Sections.Add(new SectionViewModel(section.Id, section.Title));
                }
            }

            for (int i = 0; i < sections.Count; i++)
            {
                Sections[i].Update(sections[i].Rows);
            }
        }
    }
}
