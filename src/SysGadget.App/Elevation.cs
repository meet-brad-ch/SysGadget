using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Security.Principal;
using SysGadget.Core;
using SysGadget.LibreHardware;

namespace SysGadget.App
{
    /// <summary>Administrator rights, which LibreHardwareMonitor needs for its driver.</summary>
    internal static class Elevation
    {
        /// <summary>Whether this process runs as administrator.</summary>
        /// <returns>True when elevated.</returns>
        public static bool IsAdministrator()
        {
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
            {
                return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
            }
        }

        /// <summary>Starts a second, elevated copy of this program (UAC prompt).</summary>
        /// <returns>True when the elevated copy started; false when the user declined the prompt.</returns>
        public static bool RestartElevated()
        {
            ProcessStartInfo start = new ProcessStartInfo
            {
                FileName = Environment.ProcessPath!,
                UseShellExecute = true,
                Verb = "runas",
            };

            try
            {
                Process.Start(start);
                return true;
            }
            catch (Win32Exception error) when (error.NativeErrorCode == 1223)
            {
                return false; // ERROR_CANCELLED: the user declined the UAC prompt.
            }
        }

        /// <summary>Opens the LibreHardwareMonitor source; only call it when elevated.</summary>
        /// <returns>The temperature source.</returns>
        public static ITemperatureSource OpenTemperatureSource()
        {
            return new LibreHardwareTemperatureSource();
        }
    }
}
