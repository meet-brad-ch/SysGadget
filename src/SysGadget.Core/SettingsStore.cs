using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SysGadget.Core
{
    /// <summary>Reads and writes the settings as a JSON file.</summary>
    public sealed class SettingsStore
    {
        private static readonly JsonSerializerOptions Options = CreateOptions();

        private readonly string path;

        /// <summary>Creates a store for one file.</summary>
        /// <param name="path">Full path of the settings file.</param>
        public SettingsStore(string path)
        {
            this.path = path;
        }

        /// <summary>The default location: %APPDATA%\SysGadget\settings.json.</summary>
        /// <returns>The full path.</returns>
        public static string DefaultPath()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SysGadget",
                "settings.json");
        }

        /// <summary>Loads and validates the settings; a missing file gives the defaults (first run).</summary>
        /// <returns>The validated settings.</returns>
        /// <exception cref="InvalidDataException">The file is not valid JSON for the settings, or a value is out of range.</exception>
        public GadgetSettings Load()
        {
            if (!File.Exists(path))
            {
                return GadgetSettings.CreateDefault();
            }

            GadgetSettings? settings;
            try
            {
                settings = JsonSerializer.Deserialize<GadgetSettings>(File.ReadAllText(path), Options);
            }
            catch (JsonException error)
            {
                throw new InvalidDataException("Settings file '" + path + "' is not valid: " + error.Message, error);
            }

            if (settings == null)
            {
                throw new InvalidDataException("Settings file '" + path + "' is empty.");
            }

            settings.Validate();
            return settings;
        }

        /// <summary>Validates and writes the settings, creating the folder when needed.</summary>
        /// <param name="settings">The settings to save.</param>
        /// <exception cref="InvalidDataException">A value is out of range.</exception>
        public void Save(GadgetSettings settings)
        {
            settings.Validate();
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, JsonSerializer.Serialize(settings, Options));
        }

        private static JsonSerializerOptions CreateOptions()
        {
            JsonSerializerOptions options = new JsonSerializerOptions { WriteIndented = true };
            options.Converters.Add(new JsonStringEnumConverter());
            return options;
        }
    }
}
