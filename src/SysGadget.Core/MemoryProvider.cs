using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace SysGadget.Core
{
    /// <summary>Reads physical memory and commit charge with GlobalMemoryStatusEx.</summary>
    public static class MemoryProvider
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct MemoryStatusEx
        {
            public uint Length;
            public uint MemoryLoad;
            public ulong TotalPhys;
            public ulong AvailPhys;
            public ulong TotalPageFile;
            public ulong AvailPageFile;
            public ulong TotalVirtual;
            public ulong AvailVirtual;
            public ulong AvailExtendedVirtual;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx buffer);

        /// <summary>Samples memory.</summary>
        /// <returns>The current sample.</returns>
        /// <exception cref="Win32Exception">The system call failed.</exception>
        public static MemorySample Read()
        {
            MemoryStatusEx status = new MemoryStatusEx { Length = (uint)Marshal.SizeOf<MemoryStatusEx>() };
            if (!GlobalMemoryStatusEx(ref status))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            return new MemorySample(
                status.TotalPhys,
                status.AvailPhys,
                status.TotalPageFile,
                status.TotalPageFile - status.AvailPageFile);
        }
    }
}
