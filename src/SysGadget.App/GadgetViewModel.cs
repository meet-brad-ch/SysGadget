using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Controls;
using SysGadget.Core;

namespace SysGadget.App
{
    /// <summary>What the gadget window shows: the visible groups and an error line.</summary>
    internal sealed class GadgetViewModel : ObservableObject
    {
        private string? status;
        // card padding and border (20) + bar margins and minimum width (50) + room for a value like "2.2 / 24 GB" (70)
        private const double RowReserveWidth = 140.0;
        private const double MinLabelWidth = 64.0;

        // card padding and border (20) + gap and room for a value like "63 °C" (42); a longer value
        // ("Sun 10:35:12") has a short label beside it
        private const double TextRowReserveWidth = 62.0;

        private double sectionWidth = 224.0;
        private Orientation panelOrientation = Orientation.Vertical;
        private bool thin;

        /// <summary>Thin bar: each card is one line, title next to its first item.</summary>
        public bool Thin
        {
            get => thin;
            set => Set(ref thin, value);
        }

        /// <summary>Columns side by side (horizontal) or cards stacked (vertical).</summary>
        public Orientation PanelOrientation
        {
            get => panelOrientation;
            set => Set(ref panelOrientation, value);
        }

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

        /// <summary>Width of one card (one column when the cards sit side by side).</summary>
        public double SectionWidth
        {
            get => sectionWidth;
            set
            {
                Set(ref sectionWidth, value);
                Raise(nameof(LabelMaxWidth));
                Raise(nameof(TextLabelMaxWidth));
            }
        }

        /// <summary>
        /// Widest a row label may get: what the card leaves after the bar at its minimum and the value,
        /// so wider cards show longer labels (sensor names) before they are cut with "...".
        /// </summary>
        public double LabelMaxWidth => Math.Max(MinLabelWidth, sectionWidth - RowReserveWidth);

        /// <summary>
        /// Widest a label may get in a row without a bar (temperatures, times): only the value needs
        /// room, so long sensor names fit.
        /// </summary>
        public double TextLabelMaxWidth => Math.Max(MinLabelWidth, sectionWidth - TextRowReserveWidth);

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
    }
}
