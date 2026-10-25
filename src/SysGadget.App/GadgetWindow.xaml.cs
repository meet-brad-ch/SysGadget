using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace SysGadget.App
{
    /// <summary>The borderless, transparent gadget window.</summary>
    internal sealed partial class GadgetWindow : Window
    {
        private const int GwlExStyle = -20;
        private const int WsExToolWindow = 0x00000080;

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

        /// <summary>Raised with the group id when the user clicks a group header.</summary>
        public event Action<string>? GroupHeaderClicked;

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
        }

        /// <summary>Shows the current Always on top state in the context menu.</summary>
        /// <param name="topmost">The state.</param>
        public void ShowTopmostState(bool topmost)
        {
            TopmostItem.IsChecked = topmost;
        }

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (DragEnabled)
            {
                DragMove();
                DragFinished?.Invoke();
            }
        }

        private void OnGroupHeaderMouseDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true; // a header click toggles the group and must not start a window drag
            GroupHeaderClicked?.Invoke(((GroupViewModel)((FrameworkElement)sender).DataContext).Id);
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
