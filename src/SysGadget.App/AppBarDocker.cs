using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using SysGadget.Core;

namespace SysGadget.App
{
    /// <summary>
    /// Registers a window as a Windows application bar so the edge it docks to is reserved
    /// (maximised windows stop at it), like the old sidebar.
    /// </summary>
    internal sealed class AppBarDocker : IDisposable
    {
        private const uint AbmNew = 0;
        private const uint AbmRemove = 1;
        private const uint AbmQueryPos = 2;
        private const uint AbmSetPos = 3;
        private const int AbnPosChanged = 1;
        private const uint SwpNoActivate = 0x0010;
        private const uint SwpShowWindow = 0x0040;
        private static readonly IntPtr HwndTopmost = new IntPtr(-1);
        private static readonly IntPtr HwndNoTopmost = new IntPtr(-2);

        [StructLayout(LayoutKind.Sequential)]
        private struct Rect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct AppBarData
        {
            public uint Size;
            public IntPtr Window;
            public uint CallbackMessage;
            public uint Edge;
            public Rect Bounds;
            public IntPtr Param;
        }

        [DllImport("shell32.dll")]
        private static extern uint SHAppBarMessage(uint message, ref AppBarData data);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern uint RegisterWindowMessage(string name);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetWindowPos(IntPtr window, IntPtr insertAfter, int x, int y, int width, int height, uint flags);

        private readonly IntPtr handle;
        private readonly uint callbackMessage = RegisterWindowMessage("SysGadget.AppBar");
        private bool registered;

        /// <summary>Raised when Windows reports that the bar's position may have changed.</summary>
        public event Action? PositionChanged;

        /// <summary>Attaches to a window; the window handle must already exist.</summary>
        /// <param name="source">The window's HWND source.</param>
        public AppBarDocker(HwndSource source)
        {
            handle = source.Handle;
            source.AddHook(OnMessage);
        }

        /// <summary>Docks the window to an edge and moves it there.</summary>
        /// <param name="edge">Left, Right, Top or Bottom.</param>
        /// <param name="bounds">The monitor's full area in pixels.</param>
        /// <param name="workArea">The monitor's work area in pixels (without the taskbar).</param>
        /// <param name="thickness">Size across the docked edge in pixels.</param>
        /// <param name="topmost">Whether the window stays above others.</param>
        /// <returns>The pixel rectangle the window now occupies.</returns>
        public Rectangle Dock(DockEdge edge, Rectangle bounds, Rectangle workArea, int thickness, bool topmost)
        {
            AppBarData data = new AppBarData
            {
                Size = (uint)Marshal.SizeOf<AppBarData>(),
                Window = handle,
                CallbackMessage = callbackMessage,
                Edge = EdgeCode(edge),
            };

            if (!registered)
            {
                SHAppBarMessage(AbmNew, ref data);
                registered = true;
            }

            bool horizontal = edge == DockEdge.Left || edge == DockEdge.Right;
            data.Bounds = new Rect
            {
                Left = horizontal ? bounds.Left : workArea.Left,
                Right = horizontal ? bounds.Right : workArea.Right,
                Top = horizontal ? workArea.Top : bounds.Top,
                Bottom = horizontal ? workArea.Bottom : bounds.Bottom,
            };

            SHAppBarMessage(AbmQueryPos, ref data);
            switch (edge)
            {
                case DockEdge.Left:
                    data.Bounds.Right = data.Bounds.Left + thickness;
                    break;
                case DockEdge.Right:
                    data.Bounds.Left = data.Bounds.Right - thickness;
                    break;
                case DockEdge.Top:
                    data.Bounds.Bottom = data.Bounds.Top + thickness;
                    break;
                case DockEdge.Bottom:
                    data.Bounds.Top = data.Bounds.Bottom - thickness;
                    break;
                default:
                    throw new ArgumentException("Not a docking edge: " + edge, nameof(edge));
            }

            SHAppBarMessage(AbmSetPos, ref data);
            Rect rect = data.Bounds;
            SetWindowPos(handle, topmost ? HwndTopmost : HwndNoTopmost, rect.Left, rect.Top,
                rect.Right - rect.Left, rect.Bottom - rect.Top, SwpNoActivate | SwpShowWindow);
            return Rectangle.FromLTRB(rect.Left, rect.Top, rect.Right, rect.Bottom);
        }

        /// <summary>Gives the reserved edge back.</summary>
        public void Undock()
        {
            if (!registered)
            {
                return;
            }

            AppBarData data = new AppBarData { Size = (uint)Marshal.SizeOf<AppBarData>(), Window = handle };
            SHAppBarMessage(AbmRemove, ref data);
            registered = false;
        }

        /// <summary>Gives the reserved edge back.</summary>
        public void Dispose()
        {
            Undock();
        }

        private static uint EdgeCode(DockEdge edge)
        {
            switch (edge)
            {
                case DockEdge.Left:
                    return 0;
                case DockEdge.Top:
                    return 1;
                case DockEdge.Right:
                    return 2;
                case DockEdge.Bottom:
                    return 3;
                default:
                    throw new ArgumentException("Not a docking edge: " + edge, nameof(edge));
            }
        }

        private IntPtr OnMessage(IntPtr window, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (registered && message == (int)callbackMessage && wParam.ToInt32() == AbnPosChanged)
            {
                PositionChanged?.Invoke();
                handled = true;
            }

            return IntPtr.Zero;
        }
    }
}
