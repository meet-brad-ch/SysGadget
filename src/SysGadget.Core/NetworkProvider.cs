using System.Collections.Generic;

namespace SysGadget.Core
{
    /// <summary>Reads the throughput of every network adapter from the "Network Interface" counters.</summary>
    public sealed class NetworkProvider
    {
        private const string ReceivedCounter = "Bytes Received/sec";
        private const string SentCounter = "Bytes Sent/sec";

        private readonly CategoryRateReader reader = new CategoryRateReader("Network Interface", ReceivedCounter, SentCounter);

        /// <summary>Samples every adapter. Rates cover the time since the previous call.</summary>
        /// <returns>One entry per adapter, busiest first.</returns>
        public IReadOnlyList<NetworkSample> Read()
        {
            RateSnapshot snapshot = reader.Read();
            IReadOnlyDictionary<string, double> sent = snapshot.Of(SentCounter);
            List<NetworkSample> result = new List<NetworkSample>();
            foreach (KeyValuePair<string, double> received in snapshot.Of(ReceivedCounter))
            {
                sent.TryGetValue(received.Key, out double sentRate);
                result.Add(new NetworkSample(received.Key, received.Value, sentRate));
            }

            result.Sort((a, b) => b.TotalBytesPerSecond.CompareTo(a.TotalBytesPerSecond));
            return result;
        }
    }
}
