using System.Text.Json.Serialization;

namespace SysGadget.Core
{
    /// <summary>
    /// Visibility of one item, or of one instance of a per-instance item (one disk); the position in
    /// its group's list is the display order.
    /// </summary>
    public sealed class ItemSetting
    {
        private const char KeySeparator = '|';

        /// <summary>Item id from <see cref="MeterCatalog"/>.</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>The instance (for example the disk "1 C:") of a per-instance item; empty otherwise.</summary>
        public string Instance { get; set; } = string.Empty;

        /// <summary>Whether the item is shown. A hidden item is not sampled.</summary>
        public bool Visible { get; set; }

        /// <summary>Unique key of the entry: the item id, plus the instance for a per-instance item.</summary>
        [JsonIgnore]
        public string Key => MakeKey(Id, Instance);

        /// <summary>Builds the key of an entry.</summary>
        /// <param name="itemId">The item id.</param>
        /// <param name="instance">The instance, or empty.</param>
        /// <returns>"id" or "id|instance".</returns>
        public static string MakeKey(string itemId, string instance)
        {
            return instance.Length == 0 ? itemId : itemId + KeySeparator + instance;
        }

        /// <summary>The item id inside a key.</summary>
        /// <param name="key">A key from <see cref="MakeKey"/>.</param>
        /// <returns>The item id.</returns>
        public static string ItemIdOf(string key)
        {
            int separator = key.IndexOf(KeySeparator);
            return separator < 0 ? key : key.Substring(0, separator);
        }
    }
}
