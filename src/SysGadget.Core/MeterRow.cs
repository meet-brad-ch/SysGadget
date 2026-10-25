namespace SysGadget.Core
{
    /// <summary>One line of an item: a label, a value text and optionally a bar.</summary>
    public sealed class MeterRow
    {
        /// <summary>Creates a row.</summary>
        /// <param name="label">Left text.</param>
        /// <param name="valueText">Right text.</param>
        /// <param name="percent">Bar fill, 0 to 100; ignored without a bar.</param>
        /// <param name="hasBar">Whether the row draws a bar.</param>
        /// <param name="compact">True draws the row as one thin cell of a strip instead of a line.</param>
        /// <param name="instance">The instance the row belongs to (the disk), for per-instance items; empty otherwise.</param>
        /// <param name="alert">
        /// How alarming the value is, when the builder knows (temperatures against device limits);
        /// null lets the view judge a bar by its fill.
        /// </param>
        public MeterRow(
            string label,
            string valueText,
            double percent,
            bool hasBar,
            bool compact = false,
            string instance = "",
            AlertLevel? alert = null)
        {
            Label = label;
            ValueText = valueText;
            Percent = percent;
            HasBar = hasBar;
            Compact = compact;
            Instance = instance;
            Alert = alert;
        }

        /// <summary>How alarming the value is, when the builder knows; null lets the view judge a bar by its fill.</summary>
        public AlertLevel? Alert { get; }

        /// <summary>Left text.</summary>
        public string Label { get; }

        /// <summary>Right text.</summary>
        public string ValueText { get; }

        /// <summary>Bar fill, 0 to 100.</summary>
        public double Percent { get; }

        /// <summary>Whether the row draws a bar.</summary>
        public bool HasBar { get; }

        /// <summary>True draws the row as one thin cell of a strip instead of a line.</summary>
        public bool Compact { get; }

        /// <summary>The instance the row belongs to (the disk), for per-instance items; empty otherwise.</summary>
        public string Instance { get; }
    }
}
