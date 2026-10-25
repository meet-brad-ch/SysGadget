using System.Collections.Generic;
using System.Threading;
using SysGadget.Core;
using Xunit;

namespace SysGadget.Core.Tests
{
    /// <summary>Reads the real counters of the machine running the tests.</summary>
    public class ActivityProviderTests
    {
        [Fact]
        public void RateSnapshot_Of_UnknownCounter_Throws()
        {
            RateSnapshot snapshot = new RateSnapshot(new Dictionary<string, Dictionary<string, double>>());

            Assert.Throws<KeyNotFoundException>(() => snapshot.Of("nope"));
        }

        [Fact]
        public void CategoryRateReader_UnknownCategory_Throws()
        {
            Assert.Throws<System.InvalidOperationException>(() => new CategoryRateReader("No Such Set", "x"));
        }

        [Fact]
        public void NetworkProvider_Read_ReturnsAdaptersSortedByThroughput_NonNegative()
        {
            NetworkProvider provider = new NetworkProvider();
            Thread.Sleep(1000);
            IReadOnlyList<NetworkSample> samples = provider.Read();

            Assert.NotEmpty(samples);
            for (int i = 0; i < samples.Count; i++)
            {
                Assert.True(samples[i].ReceivedBytesPerSecond >= 0.0);
                Assert.True(samples[i].SentBytesPerSecond >= 0.0);
                if (i > 0)
                {
                    Assert.True(samples[i - 1].TotalBytesPerSecond >= samples[i].TotalBytesPerSecond);
                }
            }
        }

        [Fact]
        public void DiskProvider_Read_ReturnsDisksWithoutTotal_InRange()
        {
            DiskProvider provider = new DiskProvider();
            Thread.Sleep(1000);
            IReadOnlyList<DiskSample> samples = provider.Read();

            Assert.NotEmpty(samples);
            foreach (DiskSample sample in samples)
            {
                Assert.NotEqual("_total", sample.Name.ToLowerInvariant());
                Assert.True(sample.ReadBytesPerSecond >= 0.0);
                Assert.True(sample.WriteBytesPerSecond >= 0.0);
            }
        }
    }
}
