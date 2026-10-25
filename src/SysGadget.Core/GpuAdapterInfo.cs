namespace SysGadget.Core
{
    /// <summary>A hardware graphics adapter as DXGI reports it.</summary>
    public sealed class GpuAdapterInfo
    {
        /// <summary>Creates the description.</summary>
        /// <param name="luid">Adapter LUID as the counters spell it.</param>
        /// <param name="name">Adapter name.</param>
        /// <param name="dedicatedBytes">Dedicated video memory.</param>
        public GpuAdapterInfo(string luid, string name, ulong dedicatedBytes)
        {
            Luid = luid;
            Name = name;
            DedicatedBytes = dedicatedBytes;
        }

        /// <summary>Adapter LUID as the counters spell it.</summary>
        public string Luid { get; }

        /// <summary>Adapter name.</summary>
        public string Name { get; }

        /// <summary>Dedicated video memory.</summary>
        public ulong DedicatedBytes { get; }
    }
}
