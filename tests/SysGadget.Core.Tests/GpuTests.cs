using System;
using System.Collections.Generic;
using System.Threading;
using SysGadget.Core;
using Xunit;

namespace SysGadget.Core.Tests
{
    public class GpuTests
    {
        [Fact]
        public void ParseEngine_ReadsLuidAndEngineType()
        {
            GpuInstanceName name = GpuInstanceName.ParseEngine(
                "pid_11536_luid_0x00000000_0x00014B7E_phys_0_eng_3_engtype_VideoDecode");

            Assert.Equal("0x00000000_0x00014b7e", name.Luid);
            Assert.Equal("videodecode", name.EngineType);
        }

        [Fact]
        public void ParseAdapter_ReadsLuid()
        {
            GpuInstanceName name = GpuInstanceName.ParseAdapter("luid_0x00000000_0x00015fef_phys_0");

            Assert.Equal("0x00000000_0x00015fef", name.Luid);
            Assert.Equal(string.Empty, name.EngineType);
        }

        [Theory]
        [InlineData("")]
        [InlineData("pid_1_luid_0x0_0x1_phys_0")]
        [InlineData("luid_0x0_0x1")]
        public void Parse_Malformed_Throws(string instance)
        {
            Assert.Throws<FormatException>(() => GpuInstanceName.ParseEngine(instance));
            Assert.Throws<FormatException>(() => GpuInstanceName.ParseAdapter(instance));
        }

        [Fact]
        public void FormatLuid_MatchesCounterSpelling()
        {
            Assert.Equal("0x00000000_0x00014b7e", GpuInstanceName.FormatLuid(0, 0x14B7E));
        }

        [Fact]
        public void GpuSample_MemoryPercent_ClampsAndHandlesZeroTotal()
        {
            GpuSample half = new GpuSample(new GpuAdapterInfo("l", "n", 1000), 10.0, 500);
            GpuSample over = new GpuSample(new GpuAdapterInfo("l", "n", 1000), 10.0, 2000);
            GpuSample none = new GpuSample(new GpuAdapterInfo("l", "n", 0), 10.0, 500);

            Assert.Equal(50.0, half.MemoryPercent, 6);
            Assert.Equal(100.0, over.MemoryPercent, 6);
            Assert.Equal(0.0, none.MemoryPercent, 6);
        }

        [Fact]
        public void DxgiAdapterEnumerator_ListsAtLeastOneNamedAdapter()
        {
            IReadOnlyList<GpuAdapterInfo> adapters = new DxgiAdapterEnumerator().List();

            Assert.NotEmpty(adapters);
            foreach (GpuAdapterInfo adapter in adapters)
            {
                Assert.False(string.IsNullOrWhiteSpace(adapter.Name));
                Assert.StartsWith("0x", adapter.Luid);
            }
        }

        [Fact]
        public void GpuCounterProvider_Read_ReturnsOneSamplePerAdapter_InRange()
        {
            GpuCounterProvider provider = new GpuCounterProvider();
            Thread.Sleep(1000);
            IReadOnlyList<GpuSample> samples = provider.Read();

            Assert.Equal(provider.Adapters.Count, samples.Count);
            foreach (GpuSample sample in samples)
            {
                Assert.InRange(sample.LoadPercent, 0.0, 100.0);
                Assert.InRange(sample.MemoryPercent, 0.0, 100.0);
            }
        }
    }
}
