namespace SysGadget.Core
{
    /// <summary>Throughput of one network adapter over the last interval.</summary>
    public sealed class NetworkSample
    {
        /// <summary>Creates a sample.</summary>
        /// <param name="adapterName">Adapter name as Windows reports it.</param>
        /// <param name="receivedBytesPerSecond">Download rate.</param>
        /// <param name="sentBytesPerSecond">Upload rate.</param>
        public NetworkSample(string adapterName, double receivedBytesPerSecond, double sentBytesPerSecond)
        {
            AdapterName = adapterName;
            ReceivedBytesPerSecond = receivedBytesPerSecond;
            SentBytesPerSecond = sentBytesPerSecond;
        }

        /// <summary>Adapter name as Windows reports it.</summary>
        public string AdapterName { get; }

        /// <summary>Download rate.</summary>
        public double ReceivedBytesPerSecond { get; }

        /// <summary>Upload rate.</summary>
        public double SentBytesPerSecond { get; }

        /// <summary>Download plus upload.</summary>
        public double TotalBytesPerSecond => ReceivedBytesPerSecond + SentBytesPerSecond;
    }
}
