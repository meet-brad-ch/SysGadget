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
    }
}
