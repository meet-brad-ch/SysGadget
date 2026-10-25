namespace SysGadget.Core
{
    /// <summary>One line of a meter: a label, a value text and optionally a bar.</summary>
    public sealed class MeterRow
    {
        /// <summary>Creates a row.</summary>
        /// <param name="label">Left text.</param>
        /// <param name="valueText">Right text.</param>
        /// <param name="percent">Bar fill, 0 to 100; ignored without a bar.</param>
        /// <param name="hasBar">Whether the row draws a bar.</param>
        public MeterRow(string label, string valueText, double percent, bool hasBar)
        {
            Label = label;
            ValueText = valueText;
            Percent = percent;
            HasBar = hasBar;
        }

        /// <summary>Left text.</summary>
        public string Label { get; }

        /// <summary>Right text.</summary>
        public string ValueText { get; }

        /// <summary>Bar fill, 0 to 100.</summary>
        public double Percent { get; }

        /// <summary>Whether the row draws a bar.</summary>
        public bool HasBar { get; }
    }
}
