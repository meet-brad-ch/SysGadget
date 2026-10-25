using System;
using System.Drawing;
using System.Windows.Forms;

namespace SysGadget.App
{
    /// <summary>The notification-area icon with Settings, Always on top and Exit.</summary>
    internal sealed class TrayController : IDisposable
    {
        private readonly NotifyIcon icon;
        private readonly ToolStripMenuItem topmostItem;
        private readonly Icon drawnIcon;

        /// <summary>Creates the icon.</summary>
        public TrayController()
        {
            drawnIcon = DrawIcon();
            topmostItem = new ToolStripMenuItem("Always on top") { CheckOnClick = true };
            topmostItem.Click += (sender, args) => TopmostToggled?.Invoke(topmostItem.Checked);

            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Items.Add("Settings...", null, (sender, args) => SettingsRequested?.Invoke());
            menu.Items.Add(topmostItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Exit", null, (sender, args) => ExitRequested?.Invoke());

            icon = new NotifyIcon { Icon = drawnIcon, Text = "SysGadget", ContextMenuStrip = menu, Visible = true };
            icon.DoubleClick += (sender, args) => SettingsRequested?.Invoke();
        }

        /// <summary>Raised when the user picks Settings.</summary>
        public event Action? SettingsRequested;

        /// <summary>Raised when the user picks Exit.</summary>
        public event Action? ExitRequested;

        /// <summary>Raised with the new state when the user toggles Always on top.</summary>
        public event Action<bool>? TopmostToggled;

        /// <summary>Shows the current Always on top state in the menu.</summary>
        /// <param name="topmost">The state.</param>
        public void ShowTopmostState(bool topmost)
        {
            topmostItem.Checked = topmost;
        }

        /// <summary>Removes the icon.</summary>
        public void Dispose()
        {
            icon.Visible = false;
            icon.Dispose();
            drawnIcon.Dispose();
        }

        private static Icon DrawIcon()
        {
            using (Bitmap bitmap = new Bitmap(32, 32))
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.Transparent);
                int[] heights = { 14, 26, 20 };
                for (int i = 0; i < heights.Length; i++)
                {
                    using (Brush brush = new SolidBrush(Color.FromArgb(77, 163, 255)))
                    {
                        graphics.FillRectangle(brush, 3 + i * 10, 30 - heights[i], 8, heights[i]);
                    }
                }

                IntPtr handle = bitmap.GetHicon();
                try
                {
                    using (Icon temporary = Icon.FromHandle(handle))
                    {
                        return (Icon)temporary.Clone(); // the clone owns its own copy of the image
                    }
                }
                finally
                {
                    DestroyIcon(handle);
                }
            }
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        private static extern bool DestroyIcon(IntPtr handle);
    }
}
