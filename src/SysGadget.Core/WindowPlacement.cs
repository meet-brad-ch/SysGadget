using System;
using System.Drawing;

namespace SysGadget.Core
{
    /// <summary>Keeps a window on screen: it may touch the edges of an area but not cross them.</summary>
    public static class WindowPlacement
    {
        /// <summary>Moves a window rectangle the least distance needed to lie inside an area; the size never changes.</summary>
        /// <param name="window">The window rectangle in screen pixels.</param>
        /// <param name="area">The allowed area in screen pixels, normally a monitor's work area.</param>
        /// <returns>The moved rectangle. A window larger than the area is aligned to its left and top edges.</returns>
        public static Rectangle KeepInside(Rectangle window, Rectangle area)
        {
            int left = Math.Max(area.Left, Math.Min(window.Left, area.Right - window.Width));
            int top = Math.Max(area.Top, Math.Min(window.Top, area.Bottom - window.Height));
            return new Rectangle(left, top, window.Width, window.Height);
        }

        /// <summary>
        /// The monitor to use for a saved monitor number. A number of a monitor that is no longer
        /// connected gives the primary monitor, marked as missing so the caller tells the user.
        /// </summary>
        /// <param name="savedIndex">The saved zero-based monitor number.</param>
        /// <param name="monitorCount">The number of connected monitors, at least 1.</param>
        /// <param name="primaryIndex">The zero-based number of the primary monitor.</param>
        /// <returns>The monitor to use and whether the saved one was missing.</returns>
        /// <exception cref="ArgumentOutOfRangeException">No monitor, or the primary number is not a connected monitor.</exception>
        public static MonitorChoice ChooseMonitor(int savedIndex, int monitorCount, int primaryIndex)
        {
            if (monitorCount < 1 || primaryIndex < 0 || primaryIndex >= monitorCount)
            {
                throw new ArgumentOutOfRangeException(nameof(primaryIndex), "Primary monitor " + primaryIndex + " of " + monitorCount + " monitors.");
            }

            bool connected = savedIndex >= 0 && savedIndex < monitorCount;
            return new MonitorChoice(connected ? savedIndex : primaryIndex, !connected);
        }
    }
}
