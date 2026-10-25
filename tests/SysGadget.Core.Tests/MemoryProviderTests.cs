using SysGadget.Core;
using Xunit;

namespace SysGadget.Core.Tests
{
    /// <summary>Reads the real memory state of the machine running the tests.</summary>
    public class MemoryProviderTests
    {
        [Fact]
        public void Read_ReturnsConsistentSample()
        {
            MemorySample sample = new MemoryProvider().Read();

            Assert.True(sample.TotalBytes > 0);
            Assert.True(sample.AvailableBytes <= sample.TotalBytes);
            Assert.InRange(sample.UsedPercent, 0.0, 100.0);
            Assert.True(sample.CommitLimitBytes >= sample.TotalBytes);
            Assert.InRange(sample.CommitPercent, 0.0, 100.0);
        }

        [Fact]
        public void MemorySample_Percentages_FollowTheBytes()
        {
            MemorySample sample = new MemorySample(1000, 250, 2000, 500);

            Assert.Equal(750UL, sample.UsedBytes);
            Assert.Equal(75.0, sample.UsedPercent, 6);
            Assert.Equal(25.0, sample.CommitPercent, 6);
        }
    }
}
