namespace SysGadget.Core
{
    /// <summary>Activity of one physical disk over the last interval.</summary>
    public sealed class DiskSample
    {
        /// <summary>Creates a sample.</summary>
        /// <param name="name">Disk name, for example "1 c:".</param>
        /// <param name="busyPercent">Time the disk was busy, 0 to 100.</param>
        /// <param name="readBytesPerSecond">Read rate.</param>
        /// <param name="writeBytesPerSecond">Write rate.</param>
        public DiskSample(string name, double busyPercent, double readBytesPerSecond, double writeBytesPerSecond)
        {
            Name = name;
            BusyPercent = busyPercent;
            ReadBytesPerSecond = readBytesPerSecond;
            WriteBytesPerSecond = writeBytesPerSecond;
        }

        /// <summary>Disk name, for example "1 c:".</summary>
        public string Name { get; }

        /// <summary>Time the disk was busy, 0 to 100.</summary>
        public double BusyPercent { get; }

        /// <summary>Read rate.</summary>
        public double ReadBytesPerSecond { get; }

        /// <summary>Write rate.</summary>
        public double WriteBytesPerSecond { get; }
    }
}
