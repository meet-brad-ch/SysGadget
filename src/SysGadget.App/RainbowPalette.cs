using System;
using System.Windows.Media;

namespace SysGadget.App
{
    /// <summary>
    /// Colours the visible cards as a rainbow by position: the first card is red, the last violet,
    /// the others evenly between, whichever groups are shown.
    /// </summary>
    internal static class RainbowPalette
    {
        private const double LastHue = 270.0; // violet; going further would come back towards red
        private const double Saturation = 0.62;
        private const double Value = 0.98;

        /// <summary>The accent colour of one card.</summary>
        /// <param name="index">Position of the card, 0 for the top one.</param>
        /// <param name="count">Number of visible cards.</param>
        /// <returns>The colour; red when there is only one card.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The index is not inside 0..count-1.</exception>
        public static Color Accent(int index, int count)
        {
            if (index < 0 || index >= count)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, "Card index must be 0.." + (count - 1) + ".");
            }

            double hue = count == 1 ? 0.0 : LastHue * index / (count - 1);
            return FromHsv(hue, Saturation, Value);
        }

        private static Color FromHsv(double hue, double saturation, double value)
        {
            double chroma = value * saturation;
            double sector = hue / 60.0;
            double second = chroma * (1.0 - Math.Abs((sector % 2.0) - 1.0));
            double red = 0.0, green = 0.0, blue = 0.0;
            switch ((int)sector)
            {
                case 0: red = chroma; green = second; break;
                case 1: red = second; green = chroma; break;
                case 2: green = chroma; blue = second; break;
                case 3: green = second; blue = chroma; break;
                case 4: red = second; blue = chroma; break;
                default: red = chroma; blue = second; break;
            }

            double lift = value - chroma;
            return Color.FromRgb(ToByte(red + lift), ToByte(green + lift), ToByte(blue + lift));
        }

        private static byte ToByte(double channel)
        {
            return (byte)Math.Round(channel * 255.0);
        }
    }
}
