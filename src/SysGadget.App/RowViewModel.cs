using SysGadget.Core;

namespace SysGadget.App
{
    /// <summary>The view of one <see cref="MeterRow"/>.</summary>
    internal sealed class RowViewModel : ObservableObject
    {
        private const double WarnPercent = 75.0;
        private const double CriticalPercent = 90.0;

        private string label = string.Empty;
        private string valueText = string.Empty;
        private double percent;
        private bool hasBar;
        private int level;

        /// <summary>Left text.</summary>
        public string Label
        {
            get => label;
            private set => Set(ref label, value);
        }

        /// <summary>Right text.</summary>
        public string ValueText
        {
            get => valueText;
            private set => Set(ref valueText, value);
        }

        /// <summary>Bar fill, 0 to 100.</summary>
        public double Percent
        {
            get => percent;
            private set => Set(ref percent, value);
        }

        /// <summary>Whether the row draws a bar.</summary>
        public bool HasBar
        {
            get => hasBar;
            private set => Set(ref hasBar, value);
        }

        /// <summary>0 normal, 1 high (warning colour), 2 critical.</summary>
        public int Level
        {
            get => level;
            private set => Set(ref level, value);
        }

        /// <summary>"Label: value", shown as the tooltip of a strip cell.</summary>
        public string ToolTip => label + ": " + valueText;

        /// <summary>Copies a row into this view-model.</summary>
        /// <param name="row">The row to show.</param>
        public void Update(MeterRow row)
        {
            Label = row.Label;
            ValueText = row.ValueText;
            Percent = row.Percent;
            HasBar = row.HasBar;
            Level = !row.HasBar ? 0 : row.Percent >= CriticalPercent ? 2 : row.Percent >= WarnPercent ? 1 : 0;
            Raise(nameof(ToolTip));
        }
    }
}
