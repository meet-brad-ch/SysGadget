using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;
using SysGadget.Core;
using Xunit;

namespace SysGadget.App.Tests
{
    public class GadgetWindowTests
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct NativeRect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr window, int message, IntPtr wParam, IntPtr lParam);

        /// <summary>Windows asks the window before every drag step; an off-screen place comes back flush with the work area.</summary>
        [Fact]
        public void Moving_PastTheScreenEdge_IsPushedBackFlush()
        {
            StaRunner.Run(() =>
            {
                GadgetWindow window = new GadgetWindow(new GadgetViewModel());
                window.CreateHandle();
                window.DragEnabled = true;
                System.Drawing.Rectangle area = System.Windows.Forms.Screen.PrimaryScreen!.WorkingArea;
                NativeRect proposed = new NativeRect
                {
                    Left = area.Left - 300,
                    Top = area.Top - 50,
                    Right = area.Left - 300 + 1280,
                    Bottom = area.Top - 50 + 44,
                };
                IntPtr buffer = Marshal.AllocHGlobal(Marshal.SizeOf<NativeRect>());
                try
                {
                    Marshal.StructureToPtr(proposed, buffer, false);
                    SendMessage(window.Source.Handle, 0x0216, IntPtr.Zero, buffer); // WM_MOVING
                    NativeRect kept = Marshal.PtrToStructure<NativeRect>(buffer);

                    Assert.Equal(area.Left, kept.Left);
                    Assert.Equal(area.Top, kept.Top);
                    Assert.Equal(1280, kept.Right - kept.Left);
                    Assert.Equal(44, kept.Bottom - kept.Top);
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
                    window.Close();
                }
            });
        }

        /// <summary>
        /// Regression: ProgressBar.Value bound two-way by default to the read-only RowViewModel.Percent,
        /// which threw a XamlParseException as soon as the first row was laid out and crashed the gadget.
        /// The window is shown far off-screen so the dispatcher really builds every template: card header,
        /// line rows with and without bars, the compact strip, as cards or as the thin bar.
        /// </summary>
        [Theory]
        [InlineData(System.Windows.Controls.Orientation.Vertical, false)]
        [InlineData(System.Windows.Controls.Orientation.Horizontal, false)]
        [InlineData(System.Windows.Controls.Orientation.Horizontal, true)]
        public void Layout_OfEveryTemplate_RaisesNoDispatcherException_AndFillsTheWindow(
            System.Windows.Controls.Orientation orientation, bool thin)
        {
            StaRunner.Run(() =>
            {
                List<Exception> dispatcherErrors = new List<Exception>();
                Dispatcher.CurrentDispatcher.UnhandledException += (sender, args) =>
                {
                    dispatcherErrors.Add(args.Exception);
                    args.Handled = true;
                };

                GadgetViewModel viewModel = new GadgetViewModel { PanelOrientation = orientation, Thin = thin };
                GadgetWindow window = new GadgetWindow(viewModel)
                {
                    WindowStartupLocation = WindowStartupLocation.Manual,
                    Left = -20000,
                    Top = 0,
                    ShowActivated = false,
                    // as GadgetController sets it: columns size the window in both directions
                    SizeToContent = orientation == System.Windows.Controls.Orientation.Horizontal
                        ? SizeToContent.WidthAndHeight
                        : SizeToContent.Height,
                };
                viewModel.Update(new[]
                {
                    new GroupSection(MeterCatalog.CpuGroup, "CPU", new[]
                    {
                        new MeterRow("Total", "20 %", 20.0, true),
                        new MeterRow("Core 0", "12 %", 12.0, true, true),
                        new MeterRow("Core 1", "95 %", 95.0, true, true),
                    }),
                    new GroupSection(MeterCatalog.GpuGroup, "GPU", new[]
                    {
                        new MeterRow("Load", "80 %", 80.0, true),
                        new MeterRow("Down", "1.0 KB/s", 0.0, false),
                    }),
                    new GroupSection(MeterCatalog.SystemGroup, "System", new[]
                    {
                        new MeterRow("Time", "Sun 08:50:19", 0.0, false),
                    }),
                });

                window.Show();
                Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ContextIdle);
                double height = window.ActualHeight;
                double columnsWidth = ((FrameworkElement)window.Content).ActualWidth;
                window.Close();

                Assert.Empty(dispatcherErrors);
                Assert.True(height > (thin ? 30 : 80), "the cards were not laid out, height " + height);
                if (orientation == System.Windows.Controls.Orientation.Horizontal)
                {
                    Assert.True(columnsWidth > 3 * viewModel.SectionWidth, "the three cards are not side by side, width " + columnsWidth);
                }
            });
        }
    }
}
