using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace SysGadget.Core
{
    /// <summary>
    /// Instance name of the "GPU Engine" counter set
    /// ("pid_11536_luid_0x00000000_0x00014b7e_phys_0_eng_0_engtype_3d") or of the
    /// "GPU Adapter Memory" counter set ("luid_0x00000000_0x00014b7e_phys_0").
    /// </summary>
    public sealed class GpuInstanceName
    {
        private static readonly Regex EnginePattern = new Regex(
            @"^pid_\d+_luid_(0x[0-9a-f]+_0x[0-9a-f]+)_phys_\d+_eng_\d+_engtype_(.+)$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private static readonly Regex AdapterPattern = new Regex(
            @"^luid_(0x[0-9a-f]+_0x[0-9a-f]+)_phys_\d+$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private GpuInstanceName(string luid, string engineType)
        {
            Luid = luid;
            EngineType = engineType;
        }

        /// <summary>Adapter LUID as "0xHIGH_0xLOW", lower case.</summary>
        public string Luid { get; }

        /// <summary>Engine type ("3d", "copy", "videodecode", ...), lower case; empty for adapter instances.</summary>
        public string EngineType { get; }

        /// <summary>Formats a LUID the way the counters spell it.</summary>
        /// <param name="high">High part of the LUID.</param>
        /// <param name="low">Low part of the LUID.</param>
        /// <returns>The LUID as "0xHIGH_0xLOW".</returns>
        public static string FormatLuid(int high, uint low)
        {
            return string.Format(CultureInfo.InvariantCulture, "0x{0:x8}_0x{1:x8}", high, low);
        }

        /// <summary>Parses a "GPU Engine" instance name.</summary>
        /// <param name="instance">The counter instance name.</param>
        /// <returns>The parsed name.</returns>
        /// <exception cref="FormatException">The name is not a GPU engine instance.</exception>
        public static GpuInstanceName ParseEngine(string instance)
        {
            Match match = EnginePattern.Match(instance ?? throw new ArgumentNullException(nameof(instance)));
            if (!match.Success)
            {
                throw new FormatException("Not a GPU engine instance name: '" + instance + "'");
            }

            return new GpuInstanceName(
                match.Groups[1].Value.ToLowerInvariant(),
                match.Groups[2].Value.ToLowerInvariant());
        }

        /// <summary>Parses a "GPU Adapter Memory" instance name.</summary>
        /// <param name="instance">The counter instance name.</param>
        /// <returns>The parsed name.</returns>
        /// <exception cref="FormatException">The name is not a GPU adapter instance.</exception>
        public static GpuInstanceName ParseAdapter(string instance)
        {
            Match match = AdapterPattern.Match(instance ?? throw new ArgumentNullException(nameof(instance)));
            if (!match.Success)
            {
                throw new FormatException("Not a GPU adapter instance name: '" + instance + "'");
            }

            return new GpuInstanceName(match.Groups[1].Value.ToLowerInvariant(), string.Empty);
        }
    }
}
