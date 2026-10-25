using System;
using System.Globalization;

namespace SysGadget.Core
{
    /// <summary>
    /// Instance name of the "Processor Information" counter set. A core is named
    /// "group,index" (for example "0,3"); aggregate instances ("_Total", "0,_Total")
    /// are not cores.
    /// </summary>
    public sealed class ProcessorInstanceName
    {
        private ProcessorInstanceName(int group, int index)
        {
            Group = group;
            Index = index;
        }

        /// <summary>Processor group number.</summary>
        public int Group { get; }

        /// <summary>Core index inside the group.</summary>
        public int Index { get; }

        /// <summary>Parses an instance name that names one core.</summary>
        /// <param name="instance">The counter instance name.</param>
        /// <returns>The parsed name, or null when the instance is an aggregate.</returns>
        /// <exception cref="FormatException">The name is neither a core nor a known aggregate.</exception>
        public static ProcessorInstanceName? ParseCore(string instance)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            if (instance.EndsWith("_Total", StringComparison.Ordinal))
            {
                return null;
            }

            string[] parts = instance.Split(',');
            if (parts.Length != 2
                || !int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out int group)
                || !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out int index))
            {
                throw new FormatException("Not a processor instance name: '" + instance + "'");
            }

            return new ProcessorInstanceName(group, index);
        }
    }
}
