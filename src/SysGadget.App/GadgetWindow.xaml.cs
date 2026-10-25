using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using SysGadget.Core;
using Screen = System.Windows.Forms.Screen;

namespace SysGadget.App
{
    /// <summary>The borderless, transparent gadget window.</summary>
    internal sealed partial class GadgetWindow : Window
    {
        private const int GwlExStyle = -20;
        private const int WsExToolWindow = 0x00000080;
        private const int WmMoving = 0x0216;

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeRect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
        private static extern IntPtr GetWindowLongPtr(IntPtr window, int index);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
        private static extern IntPtr SetWindowLongPtr(IntPtr window, int index, IntPtr value);

        /// <summary>Creates the window for a view-model.</summary>
        /// <param name="viewModel">What to show.</param>
        public GadgetWindow(GadgetViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        /// <summary>Raised when the user picks Settings in the context menu.</summary>
        public event Action? SettingsRequested;

        /// <summary>Raised when the user picks Exit in the context menu.</summary>
        public event Action? ExitRequested;

        /// <summary>Raised with the new state when the user toggles Always on top in the context menu.</summary>
        public event Action<bool>? TopmostToggled;

        /// <summary>Raised with the new state when the user toggles the thin bar (menu or double-click).</summary>
        public event Action<bool>? ThinToggled;

        /// <summary>Raised after the user dragged the window to a new place.</summary>
        public event Action? DragFinished;

        /// <summary>True when the window may be dragged (floating mode).</summary>
        public bool DragEnabled { get; set; }

        /// <summary>The window's native source; valid after <see cref="CreateHandle"/>.</summary>
        public HwndSource Source { get; private set; } = null!;

        /// <summary>Creates the native window without showing it.</summary>
        public void CreateHandle()
        {
            IntPtr handle = new WindowInteropHelper(this).EnsureHandle();
            Source = HwndSource.FromHwnd(handle)!;
            long style = GetWindowLongPtr(handle, GwlExStyle).ToInt64();
            SetWindowLongPtr(handle, GwlExStyle, new IntPtr(style | WsExToolWindow)); // keeps it out of Alt+Tab
            Source.AddHook(OnMessage);
        }

        /// <summary>Shows the current Always on top state in the context menu.</summary>
        /// <param name="topmost">The state.</param>
        public void ShowTopmostState(bool topmost)
        {
            TopmostItem.IsChecked = topmost;
        }

        /// <summary>Shows the current thin bar state in the context menu.</summary>
        /// <param name="thin">The state.</param>
        public void ShowThinState(bool thin)
        {
            ThinItem.IsChecked = thin;
        }

        /// <summary>
        /// Keeps a dragged window inside the work area of the monitor it is mostly on: Windows sends
        /// the proposed rectangle before every move, and the rectangle is moved back inside.
        /// </summary>
        private IntPtr OnMessage(IntPtr window, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (message != WmMoving || !DragEnabled)
            {
                return IntPtr.Zero;
            }

            NativeRect proposed = Marshal.PtrToStructure<NativeRect>(lParam);
            Rectangle rectangle = Rectangle.FromLTRB(proposed.Left, proposed.Top, proposed.Right, proposed.Bottom);
            Rectangle kept = WindowPlacement.KeepInside(rectangle, Screen.FromRectangle(rectangle).WorkingArea);
            Marshal.StructureToPtr(
                new NativeRect { Left = kept.Left, Top = kept.Top, Right = kept.Right, Bottom = kept.Bottom }, lParam, false);
            handled = true;
            return new IntPtr(1);
        }

        /// <summary>
        /// A double-click switches between the cards and the thin bar. A single press drags a floating
        /// window; the position is saved only when the window really moved.
        /// </summary>
        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                ThinToggled?.Invoke(!ThinItem.IsChecked);
                return;
            }

            if (!DragEnabled)
            {
                return;
            }

            double left = Left;
            double top = Top;
            DragMove();
            if (Left != left || Top != top)
            {
                DragFinished?.Invoke();
            }
        }

        private void OnThinClick(object sender, RoutedEventArgs e)
        {
            ThinToggled?.Invoke(ThinItem.IsChecked);
        }

        private void OnSettingsClick(object sender, RoutedEventArgs e)
        {
            SettingsRequested?.Invoke();
        }

        private void OnTopmostClick(object sender, RoutedEventArgs e)
        {
            TopmostToggled?.Invoke(TopmostItem.IsChecked);
        }

        private void OnExitClick(object sender, RoutedEventArgs e)
        {
            ExitRequested?.Invoke();
        }
    }
}
