using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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
        private readonly CrashLog crashLog;
        private readonly GadgetViewModel viewModel = new GadgetViewModel();
        private readonly GadgetWindow window;
        private readonly TrayController tray = new TrayController();
        private readonly DispatcherTimer timer = new DispatcherTimer();
        private readonly AppBarDocker docker;
        private GadgetSettings settings;
        private readonly Replaceable<MeterSampler> samplers;
        private SettingsWindow? settingsWindow;
        private bool sampling;

        /// <summary>Creates the gadget with loaded settings.</summary>
        /// <param name="store">Where the settings are saved.</param>
        /// <param name="settings">The settings in force.</param>
        /// <param name="crashLog">Where sampling errors are written.</param>
        public GadgetController(SettingsStore store, GadgetSettings settings, CrashLog crashLog)
        {
            this.store = store;
            this.crashLog = crashLog;
            this.settings = settings;
            samplers = new Replaceable<MeterSampler>(CreateSampler());
            window = new GadgetWindow(viewModel);
            window.CreateHandle();
            docker = new AppBarDocker(window.Source);

            docker.PositionChanged += ApplyLayout;
            window.DpiChanged += (sender, args) => ApplyLayout();
            window.SizeChanged += (sender, args) => KeepDefaultFloatingPlace();
            window.SettingsRequested += ShowSettings;
            tray.SettingsRequested += ShowSettings;
            window.ExitRequested += RequestExit;
            tray.ExitRequested += RequestExit;
            window.TopmostToggled += SetTopmost;
            tray.TopmostToggled += SetTopmost;
            window.ThinToggled += SetThin;
            tray.ThinToggled += SetThin;
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
            samplers.Dispose();
            window.Close();
        }

        private static MeterSampler CreateSampler()
        {
            Func<ITemperatureSource>? temperatures = Elevation.IsAdministrator() ? Elevation.OpenTemperatureSource : null;
            return new MeterSampler(new MeterRegistry(temperatures, NvmlTemperatureProvider.IsInstalled()));
        }

        private void ApplySettings(GadgetSettings next)
        {
            if (next.UseLibreHardwareMonitor && !Elevation.IsAdministrator() && AskForElevation())
            {
                return;
            }

            settings = next;
            store.Save(settings);
            samplers.Replace(CreateSampler()); // a sample still running finishes on the old one
            timer.Interval = TimeSpan.FromMilliseconds(settings.IntervalMs);
            window.ShowTopmostState(settings.AlwaysOnTop);
            tray.ShowTopmostState(settings.AlwaysOnTop);
            window.ShowThinState(settings.ThinBar);
            tray.ShowThinState(settings.ThinBar);
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
            viewModel.PanelOrientation = SideBySide() ? Orientation.Horizontal : Orientation.Vertical;
            viewModel.Thin = settings.ThinBar;

            if (settings.Edge == DockEdge.Floating)
            {
                PlaceFloating(screen, scale);
            }
            else if (settings.ReserveSpace)
            {
                PlaceReserved(screen, scale);
            }
            else
            {
                PlaceOverlay(screen, scale);
            }
        }

        /// <summary>
        /// A floating window that was never dragged keeps its default place (centred columns) while
        /// its size changes, for example when the first sample fills it.
        /// </summary>
        private void KeepDefaultFloatingPlace()
        {
            if (settings.Edge == DockEdge.Floating && !settings.FloatingLeft.HasValue)
            {
                ApplyLayout();
            }
        }

        /// <summary>Whether the cards sit side by side as columns.</summary>
        private bool SideBySide()
        {
            switch (settings.Edge)
            {
                case DockEdge.Top:
                case DockEdge.Bottom:
                    return true;
                case DockEdge.Floating:
                    return settings.Horizontal;
                default:
                    return false;
            }
        }

        /// <summary>
        /// A free window at its saved place; without one, columns start centred at the top of the
        /// work area and a stack starts at the top right.
        /// </summary>
        private void PlaceFloating(Screen screen, double scale)
        {
            docker.Undock();
            Rectangle area = screen.WorkingArea;
            if (settings.Horizontal)
            {
                window.SizeToContent = SizeToContent.WidthAndHeight;
            }
            else
            {
                window.SizeToContent = SizeToContent.Height;
                window.Width = settings.Width;
            }

            window.UpdateLayout();
            double widthPx = window.ActualWidth * scale;
            double heightPx = window.ActualHeight * scale;
            double defaultLeft = settings.Horizontal ? area.Left + ((area.Width - widthPx) / 2) : area.Right - widthPx - FloatingMarginPx;
            Rectangle wanted = new Rectangle(
                (int)Math.Round(settings.FloatingLeft ?? defaultLeft),
                (int)Math.Round(settings.FloatingTop ?? area.Top + FloatingMarginPx),
                (int)Math.Ceiling(widthPx),
                (int)Math.Ceiling(heightPx));

            // A saved place may lie off screen (monitor removed, window grew): keep it on its monitor.
            Rectangle kept = WindowPlacement.KeepInside(wanted, Screen.FromRectangle(wanted).WorkingArea);
            window.Left = kept.Left / scale;
            window.Top = kept.Top / scale;
        }

        /// <summary>Against the edge, only as tall as the cards, reserving no screen space.</summary>
        private void PlaceOverlay(Screen screen, double scale)
        {
            docker.Undock();
            Rectangle area = screen.WorkingArea;
            bool sideBar = settings.Edge == DockEdge.Left || settings.Edge == DockEdge.Right;
            window.SizeToContent = SizeToContent.Height;
            window.Width = sideBar ? settings.Width : area.Width / scale;
            window.UpdateLayout();
            double widthPx = window.ActualWidth * scale;
            double heightPx = window.ActualHeight * scale;
            window.Left = (settings.Edge == DockEdge.Right ? area.Right - widthPx : area.Left) / scale;
            window.Top = (settings.Edge == DockEdge.Bottom ? area.Bottom - heightPx : area.Top) / scale;
        }

        /// <summary>As an application bar: Windows reserves the whole edge, so maximised windows stop at it.</summary>
        private void PlaceReserved(Screen screen, double scale)
        {
            bool sideBar = settings.Edge == DockEdge.Left || settings.Edge == DockEdge.Right;
            int thickness = sideBar
                ? (int)(settings.Width * scale)
                : MeasureHeightPx(screen.WorkingArea.Width / scale, scale);
            window.SizeToContent = SizeToContent.Manual;
            docker.Dock(settings.Edge, screen.Bounds, screen.WorkingArea, thickness, settings.AlwaysOnTop);
        }

        /// <summary>
        /// Whether the window must be placed again when its content height changes: a reserved top or
        /// bottom bar takes the content height as its thickness, and a bottom overlay keeps its lower
        /// edge on the work area. The other modes grow downwards by themselves.
        /// </summary>
        private bool LayoutFollowsContent()
        {
            bool horizontal = settings.Edge == DockEdge.Top || settings.Edge == DockEdge.Bottom;
            return settings.ReserveSpace ? horizontal : settings.Edge == DockEdge.Bottom;
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
            MeterSampler active = samplers.Acquire();
            try
            {
                GadgetSettings snapshot = settings;
                System.Collections.Generic.IReadOnlyList<GroupSection> sections = await Task.Run(
                    () => active.Sample(snapshot, new SampleContext(DateTime.Now, TimeSpan.FromMilliseconds(Environment.TickCount64))));
                if (!ReferenceEquals(active, samplers.Current))
                {
                    return; // the settings changed while sampling; the next tick uses the new sampler
                }

                viewModel.Update(sections);
                viewModel.Status = null;
                if (LayoutFollowsContent())
                {
                    ApplyLayout();
                }
            }
            catch (Exception error)
            {
                timer.Stop();
                viewModel.Status = "Sampling error: " + error.Message + "\n" + Record(error) + "Open Settings and press Apply to retry.";
            }
            finally
            {
                samplers.Release(active); // disposes it now if the settings replaced it meanwhile
                sampling = false;
            }
        }

        /// <summary>Writes a sampling error to the crash log; a failure to write is reported in the text, not hidden.</summary>
        private string Record(Exception error)
        {
            try
            {
                crashLog.Write("sampling", error);
                return "Details in " + CrashLog.DefaultPath() + ".\n";
            }
            catch (System.IO.IOException logError)
            {
                return "The crash log could not be written: " + logError.Message + "\n";
            }
            catch (UnauthorizedAccessException logError)
            {
                return "The crash log could not be written: " + logError.Message + "\n";
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
            settingsWindow = new SettingsWindow(settings, monitors, ApplyFromSettingsWindow);
            settingsWindow.Closed += (sender, args) => settingsWindow = null;
            settingsWindow.Show();
        }

        /// <summary>
        /// Applies the settings window's copy. The window does not edit the floating position, so the
        /// live one is kept: dragging the gadget while the window is open must survive OK.
        /// </summary>
        private void ApplyFromSettingsWindow(GadgetSettings edited)
        {
            edited.FloatingLeft = settings.FloatingLeft;
            edited.FloatingTop = settings.FloatingTop;
            ApplySettings(edited);
        }

        private void SetThin(bool thin)
        {
            GadgetSettings next = settings.Clone();
            next.ThinBar = thin;
            ApplySettings(next);
            settingsWindow?.TakeSwitches(settings);
        }

        private void SetTopmost(bool topmost)
        {
            GadgetSettings next = settings.Clone();
            next.AlwaysOnTop = topmost;
            ApplySettings(next);
            settingsWindow?.TakeSwitches(settings);
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
