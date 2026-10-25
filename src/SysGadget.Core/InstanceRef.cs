namespace SysGadget.Core
{
    /// <summary>A found instance of a per-instance item, for example the disk "1 C:" of the disk item.</summary>
    public sealed class InstanceRef
    {
        /// <summary>Creates the reference.</summary>
        /// <param name="itemId">The per-instance item.</param>
        /// <param name="instance">The instance name.</param>
        public InstanceRef(string itemId, string instance)
        {
            ItemId = itemId;
            Instance = instance;
        }

        /// <summary>The per-instance item.</summary>
        public string ItemId { get; }

        /// <summary>The instance name.</summary>
        public string Instance { get; }
    }
}
