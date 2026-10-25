using System;
using Microsoft.Win32;

namespace SysGadget.App
{
    /// <summary>
    /// The per-user "start with Windows" entry: a value under HKCU\...\CurrentVersion\Run. The installer
    /// writes the same value when its start-up task is ticked.
    /// </summary>
    internal sealed class AutostartRegistration
    {
        private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

        /// <summary>The Run value's name, the one the installer writes too.</summary>
        public const string DefaultValueName = "SysGadget";

        private readonly string executable;
        private readonly string valueName;

        /// <summary>Creates the registration for an executable.</summary>
        /// <param name="executable">The full path of SysGadget.exe.</param>
        /// <param name="valueName">The Run value's name; tests pass their own so the real entry is untouched.</param>
        public AutostartRegistration(string executable, string valueName = DefaultValueName)
        {
            this.executable = executable ?? throw new ArgumentNullException(nameof(executable));
            this.valueName = valueName ?? throw new ArgumentNullException(nameof(valueName));
        }

        /// <summary>Whether the entry exists and starts this executable.</summary>
        public bool IsEnabled
        {
            get
            {
                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKey))
                {
                    return key?.GetValue(valueName) is string value && string.Equals(value, Command, StringComparison.OrdinalIgnoreCase);
                }
            }
        }

        private string Command => "\"" + executable + "\"";

        /// <summary>Adds or removes the entry.</summary>
        /// <param name="enabled">True to start with Windows.</param>
        public void Set(bool enabled)
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RunKey, writable: true))
            {
                if (enabled)
                {
                    key.SetValue(valueName, Command, RegistryValueKind.String);
                }
                else
                {
                    key.DeleteValue(valueName, throwOnMissingValue: false);
                }
            }
        }
    }
}
