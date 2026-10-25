namespace SysGadget.Core
{
    /// <summary>Physical memory and commit charge at one sample.</summary>
    public sealed class MemorySample
    {
        /// <summary>Creates a sample.</summary>
        /// <param name="totalBytes">Installed physical memory.</param>
        /// <param name="availableBytes">Physical memory free for use.</param>
        /// <param name="commitLimitBytes">System commit limit (RAM plus page files).</param>
        /// <param name="commitUsedBytes">Memory committed right now.</param>
        public MemorySample(ulong totalBytes, ulong availableBytes, ulong commitLimitBytes, ulong commitUsedBytes)
        {
            TotalBytes = totalBytes;
            AvailableBytes = availableBytes;
            CommitLimitBytes = commitLimitBytes;
            CommitUsedBytes = commitUsedBytes;
        }

        /// <summary>Installed physical memory.</summary>
        public ulong TotalBytes { get; }

        /// <summary>Physical memory free for use.</summary>
        public ulong AvailableBytes { get; }

        /// <summary>Physical memory in use.</summary>
        public ulong UsedBytes => TotalBytes - AvailableBytes;

        /// <summary>Physical memory in use, 0 to 100.</summary>
        public double UsedPercent => TotalBytes == 0 ? 0.0 : 100.0 * UsedBytes / TotalBytes;

        /// <summary>System commit limit (RAM plus page files).</summary>
        public ulong CommitLimitBytes { get; }

        /// <summary>Memory committed right now.</summary>
        public ulong CommitUsedBytes { get; }

        /// <summary>Commit charge, 0 to 100.</summary>
        public double CommitPercent => CommitLimitBytes == 0 ? 0.0 : 100.0 * CommitUsedBytes / CommitLimitBytes;
    }
}
