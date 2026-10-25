using System.Collections.Generic;

namespace SysGadget.Core
{
    /// <summary>The values one <see cref="CategoryRateReader"/> call produced.</summary>
    public sealed class RateSnapshot
    {
        private readonly Dictionary<string, Dictionary<string, double>> values;

        /// <summary>Creates a snapshot.</summary>
        /// <param name="values">Values per counter name and instance name.</param>
        public RateSnapshot(Dictionary<string, Dictionary<string, double>> values)
        {
            this.values = values;
        }

        /// <summary>The values of one counter.</summary>
        /// <param name="counter">Counter name as given to the reader.</param>
        /// <returns>Value per instance name.</returns>
        /// <exception cref="KeyNotFoundException">The reader was not asked for this counter.</exception>
        public IReadOnlyDictionary<string, double> Of(string counter)
        {
            return values[counter];
        }
    }
}
