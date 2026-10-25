using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace SysGadget.Core
{
    /// <summary>Size and free space of a disk, summed over its drive letters.</summary>
    public sealed class DiskSpace
    {
        private static readonly Regex DriveLetter = new Regex(@"^[A-Za-z]:$", RegexOptions.CultureInvariant);

        /// <summary>Creates the space record.</summary>
        /// <param name="totalBytes">Size of the drive letters together.</param>
        /// <param name="freeBytes">Free space of the drive letters together.</param>
        public DiskSpace(ulong totalBytes, ulong freeBytes)
        {
            TotalBytes = totalBytes;
            FreeBytes = freeBytes;
        }

        /// <summary>Size of the drive letters together.</summary>
        public ulong TotalBytes { get; }

        /// <summary>Free space of the drive letters together.</summary>
        public ulong FreeBytes { get; }

        /// <summary>Space in use.</summary>
        public ulong UsedBytes => TotalBytes - FreeBytes;

        /// <summary>Space in use, 0 to 100.</summary>
        public double UsedPercent => TotalBytes == 0 ? 0.0 : 100.0 * UsedBytes / TotalBytes;

        /// <summary>The drive letters in a physical disk name, for example "C:" and "E:" in "1 C: E:".</summary>
        /// <param name="diskName">The disk name from the PhysicalDisk counters.</param>
        /// <returns>The drive letters; none for a disk without a mounted volume.</returns>
        public static IReadOnlyList<string> DriveLettersOf(string diskName)
        {
            List<string> letters = new List<string>();
            foreach (string word in diskName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (DriveLetter.IsMatch(word))
                {
                    letters.Add(word.ToUpperInvariant());
                }
            }

            return letters;
        }

        /// <summary>Reads size and free space of a disk's drive letters.</summary>
        /// <param name="diskName">The disk name from the PhysicalDisk counters.</param>
        /// <returns>The space, or null when the disk has no drive letter that is ready.</returns>
        public static DiskSpace? Of(string diskName)
        {
            ulong total = 0;
            ulong free = 0;
            bool any = false;
            foreach (string letter in DriveLettersOf(diskName))
            {
                DriveInfo drive = new DriveInfo(letter);
                if (!drive.IsReady)
                {
                    continue; // e.g. an empty card reader: no space to report
                }

                total += (ulong)drive.TotalSize;
                free += (ulong)drive.AvailableFreeSpace;
                any = true;
            }

            return any ? new DiskSpace(total, free) : null;
        }
    }
}
