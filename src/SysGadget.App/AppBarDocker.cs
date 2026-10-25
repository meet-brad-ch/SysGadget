using System;
using System.ComponentModel;
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

        [DllImport("user32.dll", SetLastError = true)]
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
                Send(AbmNew, ref data, "ABM_NEW");
                registered = true;
            }

            data.Bounds = Proposal(edge, bounds, workArea);
            Send(AbmQueryPos, ref data, "ABM_QUERYPOS");
            data.Bounds = Cut(data.Bounds, edge, thickness);
            Send(AbmSetPos, ref data, "ABM_SETPOS");
            Rect rect = data.Bounds;
            bool placed = SetWindowPos(handle, topmost ? HwndTopmost : HwndNoTopmost, rect.Left, rect.Top,
                rect.Right - rect.Left, rect.Bottom - rect.Top, SwpNoActivate | SwpShowWindow);
            if (!placed)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "SetWindowPos failed for the docked bar.");
            }

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
            Send(AbmRemove, ref data, "ABM_REMOVE");
            registered = false;
        }

        /// <summary>Gives the reserved edge back.</summary>
        public void Dispose()
        {
            Undock();
        }

        /// <summary>The whole edge proposed to Windows: the full monitor along the edge, the work area across it.</summary>
        private static Rect Proposal(DockEdge edge, Rectangle bounds, Rectangle workArea)
        {
            bool sideBar = edge == DockEdge.Left || edge == DockEdge.Right;
            Rectangle along = sideBar ? Rectangle.FromLTRB(bounds.Left, workArea.Top, bounds.Right, workArea.Bottom)
                                      : Rectangle.FromLTRB(workArea.Left, bounds.Top, workArea.Right, bounds.Bottom);
            return new Rect { Left = along.Left, Top = along.Top, Right = along.Right, Bottom = along.Bottom };
        }

        /// <summary>The granted area cut to the bar's thickness at its edge.</summary>
        private static Rect Cut(Rect granted, DockEdge edge, int thickness)
        {
            switch (edge)
            {
                case DockEdge.Left:
                    granted.Right = granted.Left + thickness;
                    break;
                case DockEdge.Right:
                    granted.Left = granted.Right - thickness;
                    break;
                case DockEdge.Top:
                    granted.Bottom = granted.Top + thickness;
                    break;
                case DockEdge.Bottom:
                    granted.Top = granted.Bottom - thickness;
                    break;
                default:
                    throw new ArgumentException("Not a docking edge: " + edge, nameof(edge));
            }

            return granted;
        }

        /// <summary>Sends an app bar message; ABM_NEW gives FALSE when Windows refuses the bar, the others always TRUE.</summary>
        /// <exception cref="InvalidOperationException">The message returned FALSE.</exception>
        private static void Send(uint message, ref AppBarData data, string name)
        {
            if (SHAppBarMessage(message, ref data) == 0)
            {
                throw new InvalidOperationException("SHAppBarMessage " + name + " failed.");
            }
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
