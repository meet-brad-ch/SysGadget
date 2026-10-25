using System;
using System.Globalization;
using System.IO;

namespace SysGadget.Core
{
    /// <summary>Appends unhandled exceptions to a text file so a crash leaves evidence.</summary>
    public sealed class CrashLog
    {
        private readonly string path;

        /// <summary>Creates a log for one file.</summary>
        /// <param name="path">Full path of the log file.</param>
        public CrashLog(string path)
        {
            this.path = path;
        }

        /// <summary>The default location: %APPDATA%\SysGadget\crash.log.</summary>
        /// <returns>The full path.</returns>
        public static string DefaultPath()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SysGadget",
                "crash.log");
        }

        /// <summary>Appends one entry; the folder is created when needed.</summary>
        /// <param name="source">Where the exception was caught, for example "UI thread".</param>
        /// <param name="error">The exception, written with its full text and inner exceptions.</param>
        /// <exception cref="IOException">The file cannot be written.</exception>
        public void Write(string source, Exception error)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            string entry = string.Format(
                CultureInfo.InvariantCulture,
                "=== {0:yyyy-MM-dd HH:mm:ss} [{1}] ==={2}{3}{2}{2}",
                DateTime.Now,
                source,
                Environment.NewLine,
                error);
            File.AppendAllText(path, entry);
        }
    }
}
