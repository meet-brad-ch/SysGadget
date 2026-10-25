namespace SysGadget.Core
{
    /// <summary>Load and dedicated memory of one graphics adapter at one sample.</summary>
    public sealed class GpuSample
    {
        /// <summary>Creates a sample.</summary>
        /// <param name="adapter">The adapter measured.</param>
        /// <param name="loadPercent">Busiest engine type, 0 to 100.</param>
        /// <param name="dedicatedUsedBytes">Dedicated video memory in use.</param>
        public GpuSample(GpuAdapterInfo adapter, double loadPercent, ulong dedicatedUsedBytes)
        {
            Adapter = adapter;
            LoadPercent = loadPercent;
            DedicatedUsedBytes = dedicatedUsedBytes;
        }

        /// <summary>The adapter measured.</summary>
        public GpuAdapterInfo Adapter { get; }

        /// <summary>Busiest engine type, 0 to 100.</summary>
        public double LoadPercent { get; }

        /// <summary>Dedicated video memory in use.</summary>
        public ulong DedicatedUsedBytes { get; }

        /// <summary>Dedicated video memory in use, 0 to 100.</summary>
        public double MemoryPercent => Adapter.DedicatedBytes == 0
            ? 0.0
            : System.Math.Min(100.0, 100.0 * DedicatedUsedBytes / Adapter.DedicatedBytes);
    }
}
