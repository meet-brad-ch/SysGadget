using System;
using System.Globalization;

namespace SysGadget.Core
{
    /// <summary>Formats sizes, rates and durations for display.</summary>
    public static class ByteFormat
    {
        private static readonly string[] Units = { "B", "KB", "MB", "GB", "TB" };

        /// <summary>Formats a byte count with binary units, for example "12.3 GB".</summary>
        /// <param name="bytes">The byte count.</param>
        /// <returns>The text; one decimal below 10, none from 10 up.</returns>
        public static string Size(double bytes)
        {
            double value = Math.Max(0.0, bytes);
            int unit = 0;
            while (value >= 1024.0 && unit < Units.Length - 1)
            {
                value /= 1024.0;
                unit++;
            }

            string number = unit == 0 || value >= 10.0
                ? value.ToString("0", CultureInfo.InvariantCulture)
                : value.ToString("0.0", CultureInfo.InvariantCulture);
            return number + " " + Units[unit];
        }

        /// <summary>Formats a rate, for example "1.2 MB/s".</summary>
        /// <param name="bytesPerSecond">The rate.</param>
        /// <returns>The text.</returns>
        public static string Rate(double bytesPerSecond)
        {
            return Size(bytesPerSecond) + "/s";
        }

        /// <summary>Formats an uptime, for example "3d 04:05" or "04:05:09".</summary>
        /// <param name="uptime">The time since boot.</param>
        /// <returns>The text; days appear only when there is at least one.</returns>
        public static string Uptime(TimeSpan uptime)
        {
            return uptime.Days > 0
                ? string.Format(CultureInfo.InvariantCulture, "{0}d {1:00}:{2:00}", uptime.Days, uptime.Hours, uptime.Minutes)
                : string.Format(CultureInfo.InvariantCulture, "{0:00}:{1:00}:{2:00}", uptime.Hours, uptime.Minutes, uptime.Seconds);
        }
    }
}
