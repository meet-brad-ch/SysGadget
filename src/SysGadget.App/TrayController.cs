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
        private readonly ToolStripMenuItem thinItem;
        private readonly Icon appIcon;

        /// <summary>Creates the icon.</summary>
        public TrayController()
        {
            appIcon = LoadAppIcon();
            topmostItem = new ToolStripMenuItem("Always on top") { CheckOnClick = true };
            topmostItem.Click += (sender, args) => TopmostToggled?.Invoke(topmostItem.Checked);
            thinItem = new ToolStripMenuItem("Thin bar") { CheckOnClick = true };
            thinItem.Click += (sender, args) => ThinToggled?.Invoke(thinItem.Checked);

            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Items.Add("Settings...", null, (sender, args) => SettingsRequested?.Invoke());
            menu.Items.Add(topmostItem);
            menu.Items.Add(thinItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Exit", null, (sender, args) => ExitRequested?.Invoke());

            icon = new NotifyIcon { Icon = appIcon, Text = "SysGadget", ContextMenuStrip = menu, Visible = true };
            icon.DoubleClick += (sender, args) => SettingsRequested?.Invoke();
        }

        /// <summary>Raised when the user picks Settings.</summary>
        public event Action? SettingsRequested;

        /// <summary>Raised when the user picks Exit.</summary>
        public event Action? ExitRequested;

        /// <summary>Raised with the new state when the user toggles Always on top.</summary>
        public event Action<bool>? TopmostToggled;

        /// <summary>Raised with the new state when the user toggles Thin bar.</summary>
        public event Action<bool>? ThinToggled;

        /// <summary>Shows the current thin bar state in the menu.</summary>
        /// <param name="thin">The state.</param>
        public void ShowThinState(bool thin)
        {
            thinItem.Checked = thin;
        }

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
            appIcon.Dispose();
        }

        /// <summary>The app icon (tools/make_icon.ps1) at the size Windows uses in the notification area.</summary>
        private static Icon LoadAppIcon()
        {
            using (System.IO.Stream? stream = typeof(TrayController).Assembly.GetManifestResourceStream("SysGadget.App.SysGadget.ico"))
            {
                if (stream == null)
                {
                    throw new InvalidOperationException("The embedded resource SysGadget.App.SysGadget.ico is missing.");
                }

                return new Icon(stream, SystemInformation.SmallIconSize);
            }
        }
    }
}
