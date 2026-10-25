namespace SysGadget.Core
{
    /// <summary>Transfer rates and space of one physical disk.</summary>
    public sealed class DiskSample
    {
        /// <summary>Creates a sample.</summary>
        /// <param name="name">Disk name, for example "1 C:".</param>
        /// <param name="readBytesPerSecond">Read rate over the last interval.</param>
        /// <param name="writeBytesPerSecond">Write rate over the last interval.</param>
        /// <param name="space">Size and free space of the disk's drive letters, or null when it has none.</param>
        public DiskSample(string name, double readBytesPerSecond, double writeBytesPerSecond, DiskSpace? space = null)
        {
            Name = name;
            ReadBytesPerSecond = readBytesPerSecond;
            WriteBytesPerSecond = writeBytesPerSecond;
            Space = space;
        }

        /// <summary>Disk name, for example "1 C:".</summary>
        public string Name { get; }

        /// <summary>Read rate over the last interval.</summary>
        public double ReadBytesPerSecond { get; }

        /// <summary>Write rate over the last interval.</summary>
        public double WriteBytesPerSecond { get; }

        /// <summary>Size and free space of the disk's drive letters, or null when it has none.</summary>
        public DiskSpace? Space { get; }
    }
}
