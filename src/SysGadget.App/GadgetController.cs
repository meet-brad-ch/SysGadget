using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using SysGadget.Core;
using Screen = System.Windows.Forms.Screen;

namespace SysGadget.App
{
    /// <summary>Owns the window, tray icon, sampler and timer, and applies the settings to them.</summary>
    internal sealed class GadgetController : IDisposable
    {
        private const int FloatingMarginPx = 12;
        private const int ContentPaddingDip = 16;

        private readonly SettingsStore store;
        private readonly GadgetViewModel viewModel = new GadgetViewModel();
        private readonly GadgetWindow window;
        private readonly TrayController tray = new TrayController();
        private readonly DispatcherTimer timer = new DispatcherTimer();
        private readonly AppBarDocker docker;
        private GadgetSettings settings;
        private MeterSampler sampler;
        private SettingsWindow? settingsWindow;
        private bool sampling;

        /// <summary>Creates the gadget with loaded settings.</summary>
        /// <param name="store">Where the settings are saved.</param>
        /// <param name="settings">The settings in force.</param>
        public GadgetController(SettingsStore store, GadgetSettings settings)
        {
            this.store = store;
            this.settings = settings;
            sampler = CreateSampler();
            window = new GadgetWindow(viewModel);
            window.CreateHandle();
            docker = new AppBarDocker(window.Source);

            docker.PositionChanged += ApplyLayout;
            window.DpiChanged += (sender, args) => ApplyLayout();
            window.SettingsRequested += ShowSettings;
            tray.SettingsRequested += ShowSettings;
            window.ExitRequested += RequestExit;
            tray.ExitRequested += RequestExit;
            window.TopmostToggled += SetTopmost;
            tray.TopmostToggled += SetTopmost;
            window.DragFinished += SaveFloatingPosition;
            timer.Tick += OnTick;
        }

        /// <summary>Raised when the user asks to quit.</summary>
        public event Action? ExitRequested;

        /// <summary>Shows the gadget and starts sampling.</summary>
        public void Start()
        {
            ApplySettings(settings);
            window.Show();
            ApplyLayout();
            OnTick(this, EventArgs.Empty);
        }

        /// <summary>Removes the docked space and the tray icon.</summary>
        public void Dispose()
        {
            timer.Stop();
            docker.Dispose();
            tray.Dispose();
            sampler.Dispose();
            window.Close();
        }

        private static MeterSampler CreateSampler()
        {
            return new MeterSampler(Elevation.IsAdministrator() ? (Func<ITemperatureSource>)Elevation.OpenTemperatureSource : null);
        }

        private void ApplySettings(GadgetSettings next)
        {
            if (next.UseLibreHardwareMonitor && !Elevation.IsAdministrator() && AskForElevation())
            {
                return;
            }

            settings = next;
            store.Save(settings);
            sampler.Dispose();
            sampler = CreateSampler();
            timer.Interval = TimeSpan.FromMilliseconds(settings.IntervalMs);
            window.ShowTopmostState(settings.AlwaysOnTop);
            tray.ShowTopmostState(settings.AlwaysOnTop);
            ApplyLayout();
            viewModel.Status = null;
            timer.Start();
        }

        private bool AskForElevation()
        {
            MessageBoxResult answer = MessageBox.Show(
                "LibreHardwareMonitor needs administrator rights for temperatures.\n\nRestart SysGadget as administrator now?",
                "SysGadget",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (answer == MessageBoxResult.Yes && Elevation.RestartElevated())
            {
                RequestExit();
                return true;
            }

            return false;
        }

        private void ApplyLayout()
        {
            Screen screen = PickScreen();
            double scale = VisualTreeHelper.GetDpi(window).DpiScaleX;
            viewModel.SectionWidth = settings.Width - ContentPaddingDip;
            window.Opacity = settings.Opacity;
            window.Topmost = settings.AlwaysOnTop;
            window.DragEnabled = settings.Edge == DockEdge.Floating;

            if (settings.Edge == DockEdge.Floating)
            {
                docker.Undock();
                window.SizeToContent = SizeToContent.Height;
                window.Width = settings.Width;
                int left = settings.FloatingLeft.HasValue
                    ? (int)settings.FloatingLeft.Value
                    : screen.WorkingArea.Right - (int)(settings.Width * scale) - FloatingMarginPx;
                int top = settings.FloatingTop.HasValue ? (int)settings.FloatingTop.Value : screen.WorkingArea.Top + FloatingMarginPx;
                window.Left = left / scale;
                window.Top = top / scale;
                return;
            }

            bool sideBar = settings.Edge == DockEdge.Left || settings.Edge == DockEdge.Right;
            int thickness = sideBar
                ? (int)(settings.Width * scale)
                : MeasureHeightPx(screen.WorkingArea.Width / scale, scale);
            window.SizeToContent = SizeToContent.Manual;
            docker.Dock(settings.Edge, screen.Bounds, screen.WorkingArea, thickness, settings.AlwaysOnTop);
        }

        private int MeasureHeightPx(double widthDip, double scale)
        {
            FrameworkElement content = (FrameworkElement)window.Content;
            content.Measure(new System.Windows.Size(widthDip, double.PositiveInfinity));
            return Math.Max(1, (int)Math.Ceiling(content.DesiredSize.Height * scale));
        }

        private Screen PickScreen()
        {
            Screen[] screens = Screen.AllScreens;
            return settings.MonitorIndex < screens.Length ? screens[settings.MonitorIndex] : Screen.PrimaryScreen ?? screens[0];
        }

        private async void OnTick(object? sender, EventArgs e)
        {
            if (sampling)
            {
                return;
            }

            sampling = true;
            try
            {
                GadgetSettings snapshot = settings;
                MeterSampler active = sampler;
                System.Collections.Generic.IReadOnlyList<MeterSection> sections = await Task.Run(
                    () => active.Sample(snapshot, DateTime.Now, TimeSpan.FromMilliseconds(Environment.TickCount64)));
                if (!ReferenceEquals(active, sampler))
                {
                    return; // the settings changed while sampling; the next tick uses the new sampler
                }

                viewModel.Update(sections);
                viewModel.Status = null;
                if (settings.Edge == DockEdge.Top || settings.Edge == DockEdge.Bottom)
                {
                    ApplyLayout();
                }
            }
            catch (Exception error)
            {
                timer.Stop();
                viewModel.Status = "Sensor error: " + error.Message + "\nOpen Settings and press Apply to retry.";
            }
            finally
            {
                sampling = false;
            }
        }

        private void ShowSettings()
        {
            if (settingsWindow != null)
            {
                settingsWindow.Activate();
                return;
            }

            string[] monitors = Screen.AllScreens
                .Select((s, i) => (i + 1) + ": " + s.Bounds.Width + "x" + s.Bounds.Height + (s.Primary ? " (primary)" : string.Empty))
                .ToArray();
            settingsWindow = new SettingsWindow(settings, monitors, ApplySettings);
            settingsWindow.Closed += (sender, args) => settingsWindow = null;
            settingsWindow.Show();
        }

        private void SetTopmost(bool topmost)
        {
            GadgetSettings next = settings.Clone();
            next.AlwaysOnTop = topmost;
            ApplySettings(next);
        }

        private void SaveFloatingPosition()
        {
            double scale = VisualTreeHelper.GetDpi(window).DpiScaleX;
            GadgetSettings next = settings.Clone();
            next.FloatingLeft = Math.Round(window.Left * scale);
            next.FloatingTop = Math.Round(window.Top * scale);
            settings = next;
            store.Save(settings);
        }

        private void RequestExit()
        {
            ExitRequested?.Invoke();
        }
    }
}
