using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SysGadget.Core
{
    /// <summary>All user settings of the gadget.</summary>
    public sealed class GadgetSettings
    {
        /// <summary>Smallest allowed width in device-independent pixels.</summary>
        public const int MinWidth = 120;

        /// <summary>Largest allowed width in device-independent pixels.</summary>
        public const int MaxWidth = 800;

        /// <summary>Smallest allowed update interval.</summary>
        public const int MinIntervalMs = 250;

        /// <summary>Largest allowed update interval.</summary>
        public const int MaxIntervalMs = 10000;

        /// <summary>Smallest allowed opacity.</summary>
        public const double MinOpacity = 0.2;

        /// <summary>Edge to dock to.</summary>
        public DockEdge Edge { get; set; } = DockEdge.Right;

        /// <summary>Zero-based index of the monitor, in the order Windows lists them.</summary>
        public int MonitorIndex { get; set; }

        /// <summary>Keep the gadget above other windows.</summary>
        public bool AlwaysOnTop { get; set; } = true;

        /// <summary>Width in device-independent pixels (height for top and bottom docking).</summary>
        public int Width { get; set; } = 220;

        /// <summary>Window opacity, <see cref="MinOpacity"/> to 1.</summary>
        public double Opacity { get; set; } = 0.92;

        /// <summary>Time between samples.</summary>
        public int IntervalMs { get; set; } = 1000;

        /// <summary>Use LibreHardwareMonitor for temperatures and power; asks for admin rights.</summary>
        public bool UseLibreHardwareMonitor { get; set; }

        /// <summary>Meters in display order.</summary>
        public List<MeterSetting> Meters { get; set; } = new List<MeterSetting>();

        /// <summary>Creates the settings of a fresh install.</summary>
        /// <returns>Defaults with every catalogue meter present.</returns>
        public static GadgetSettings CreateDefault()
        {
            GadgetSettings settings = new GadgetSettings();
            foreach (MeterDefinition definition in MeterCatalog.All)
            {
                settings.Meters.Add(new MeterSetting { Id = definition.Id, Visible = definition.VisibleByDefault });
            }

            return settings;
        }

        /// <summary>Whether a meter is shown.</summary>
        /// <param name="id">The meter id.</param>
        /// <returns>True when shown.</returns>
        public bool IsVisible(string id)
        {
            return Find(id).Visible;
        }

        /// <summary>Shows or hides a meter.</summary>
        /// <param name="id">The meter id.</param>
        /// <param name="visible">The new state.</param>
        public void SetVisible(string id, bool visible)
        {
            Find(id).Visible = visible;
        }

        /// <summary>Moves a meter up or down in the display order.</summary>
        /// <param name="id">The meter id.</param>
        /// <param name="delta">-1 moves it up, +1 down; a move past either end is ignored.</param>
        public void Move(string id, int delta)
        {
            int from = Meters.IndexOf(Find(id));
            int to = from + delta;
            if (to < 0 || to >= Meters.Count)
            {
                return;
            }

            MeterSetting item = Meters[from];
            Meters.RemoveAt(from);
            Meters.Insert(to, item);
        }

        /// <summary>
        /// Checks the values and adds catalogue meters that an older settings file lacks
        /// (hidden unless visible by default, appended at the end).
        /// </summary>
        /// <exception cref="InvalidDataException">A value is out of range or a meter id is unknown or repeated.</exception>
        public void Validate()
        {
            Require(Width >= MinWidth && Width <= MaxWidth, "Width must be " + MinWidth + " to " + MaxWidth + ".");
            Require(IntervalMs >= MinIntervalMs && IntervalMs <= MaxIntervalMs,
                "IntervalMs must be " + MinIntervalMs + " to " + MaxIntervalMs + ".");
            Require(Opacity >= MinOpacity && Opacity <= 1.0, "Opacity must be " + MinOpacity + " to 1.");
            Require(MonitorIndex >= 0, "MonitorIndex must not be negative.");

            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (MeterSetting meter in Meters)
            {
                Require(MeterCatalog.All.Any(d => d.Id == meter.Id), "Unknown meter id '" + meter.Id + "'.");
                Require(seen.Add(meter.Id), "Meter '" + meter.Id + "' is listed twice.");
            }

            foreach (MeterDefinition definition in MeterCatalog.All)
            {
                if (!seen.Contains(definition.Id))
                {
                    Meters.Add(new MeterSetting { Id = definition.Id, Visible = definition.VisibleByDefault });
                }
            }
        }

        private MeterSetting Find(string id)
        {
            foreach (MeterSetting meter in Meters)
            {
                if (string.Equals(meter.Id, id, StringComparison.Ordinal))
                {
                    return meter;
                }
            }

            throw new KeyNotFoundException("Meter '" + id + "' is not in the settings.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidDataException(message);
            }
        }
    }
}
