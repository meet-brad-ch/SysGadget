namespace SysGadget.Core
{
    /// <summary>Activity and space of one physical disk.</summary>
    public sealed class DiskSample
    {
        /// <summary>Creates a sample.</summary>
        /// <param name="name">Disk name, for example "1 C:".</param>
        /// <param name="busyPercent">Time the disk was busy over the last interval, 0 to 100.</param>
        /// <param name="readBytesPerSecond">Read rate.</param>
        /// <param name="writeBytesPerSecond">Write rate.</param>
        /// <param name="space">Size and free space of the disk's drive letters, or null when it has none.</param>
        public DiskSample(string name, double busyPercent, double readBytesPerSecond, double writeBytesPerSecond, DiskSpace? space = null)
        {
            Name = name;
            BusyPercent = busyPercent;
            ReadBytesPerSecond = readBytesPerSecond;
            WriteBytesPerSecond = writeBytesPerSecond;
            Space = space;
        }

        /// <summary>Disk name, for example "1 C:".</summary>
        public string Name { get; }

        /// <summary>Time the disk was busy over the last interval, 0 to 100.</summary>
        public double BusyPercent { get; }

        /// <summary>Read rate.</summary>
        public double ReadBytesPerSecond { get; }

        /// <summary>Write rate.</summary>
        public double WriteBytesPerSecond { get; }

        /// <summary>Size and free space of the disk's drive letters, or null when it has none.</summary>
        public DiskSpace? Space { get; }
    }
}
