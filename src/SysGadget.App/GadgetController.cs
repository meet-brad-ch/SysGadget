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
        private readonly MeterRegistry registry = CreateRegistry();
        private readonly AutostartRegistration autostart = new AutostartRegistration(Environment.ProcessPath!);
        private readonly Replaceable<MeterSampler> samplers;
        private SettingsWindow? settingsWindow;
        private bool sampling;
        private int? reportedMissingMonitor;

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
            GadgetSettings withInstances = settings.Clone();
            withInstances.AddInstances(registry.FindInstances()); // disks added since the last run
            ApplySettings(withInstances);
            window.Show();
            ApplyLayout();
            if (settings.ShowsTemperatures() && !Elevation.IsAdministrator() && AskForElevation())
            {
                return; // the elevated copy takes over
            }

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

        private static MeterRegistry CreateRegistry()
        {
            Func<ITemperatureSource>? temperatures = Elevation.IsAdministrator() ? Elevation.OpenTemperatureSource : null;
            return new MeterRegistry(temperatures, NvmlTemperatureProvider.IsInstalled());
        }

        private MeterSampler CreateSampler()
        {
            return new MeterSampler(registry);
        }

        private void ApplySettings(GadgetSettings next)
        {
            bool wasShowingTemperatures = settings.ShowsTemperatures();
            settings = next;
            store.Save(settings); // saved first: an elevated restart must read the new settings
            if (settings.ShowsTemperatures() && !wasShowingTemperatures && !Elevation.IsAdministrator() && AskForElevation())
            {
                return;
            }

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

        /// <summary>
        /// Offers to restart as administrator. The question is owned by the settings window (or the
        /// gadget), so it cannot open behind those always-on-top windows.
        /// </summary>
        /// <returns>True when the elevated copy started and this one is ending.</returns>
        private bool AskForElevation()
        {
            Window owner = (Window?)settingsWindow ?? window;
            MessageBoxResult answer = MessageBox.Show(
                owner,
                "Temperatures are read through LibreHardwareMonitor, which needs administrator rights.\n\nRestart SysGadget as administrator now?",
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

        /// <summary>
        /// The saved monitor, or the primary one when the saved one is not connected. The notice of
        /// that comes once per missing monitor, not at every layout pass.
        /// </summary>
        private Screen PickScreen()
        {
            Screen[] screens = Screen.AllScreens;
            int primary = Array.FindIndex(screens, s => s.Primary);
            MonitorChoice choice = WindowPlacement.ChooseMonitor(settings.MonitorIndex, screens.Length, primary);
            if (choice.SavedMissing && reportedMissingMonitor != settings.MonitorIndex)
            {
                tray.Notify("Monitor " + (settings.MonitorIndex + 1) + " is not connected; SysGadget is on the primary monitor. Pick a monitor in Settings.");
            }

            reportedMissingMonitor = choice.SavedMissing ? settings.MonitorIndex : (int?)null;
            return screens[choice.Index];
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
                viewModel.Status = "Sampling error: " + error.Message + "\n" + crashLog.WriteAndDescribe("sampling", error) + "\nOpen Settings and press Apply to retry.";
            }
            finally
            {
                samplers.Release(active); // disposes it now if the settings replaced it meanwhile
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

            GadgetSettings withInstances = settings.Clone();
            if (withInstances.AddInstances(registry.FindInstances()))
            {
                ApplySettings(withInstances); // a disk plugged in meanwhile gets its entry in the tree
            }

            string[] monitors = Screen.AllScreens
                .Select((s, i) => (i + 1) + ": " + s.Bounds.Width + "x" + s.Bounds.Height + (s.Primary ? " (primary)" : string.Empty))
                .ToArray();
            settingsWindow = new SettingsWindow(settings, monitors, ApplyFromSettingsWindow, autostart.IsEnabled, autostart.Set);
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
