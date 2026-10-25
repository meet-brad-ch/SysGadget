using System;
using System.Collections.Generic;
using SysGadget.Core;
using Xunit;

namespace SysGadget.Core.Tests
{
    public class RowBuilderTests
    {
        [Theory]
        [InlineData(0.0, "0 B")]
        [InlineData(512.0, "512 B")]
        [InlineData(1536.0, "1.5 KB")]
        [InlineData(10.0 * 1024, "10 KB")]
        [InlineData(5.0 * 1024 * 1024 * 1024, "5.0 GB")]
        [InlineData(-5.0, "0 B")]
        public void Size_UsesBinaryUnits(double bytes, string expected)
        {
            Assert.Equal(expected, ByteFormat.Size(bytes));
        }

        [Fact]
        public void Rate_AppendsPerSecond()
        {
            Assert.Equal("2.0 MB/s", ByteFormat.Rate(2.0 * 1024 * 1024));
        }

        [Fact]
        public void Uptime_ShowsDaysOnlyWhenThereAreSome()
        {
            Assert.Equal("3d 04:05", ByteFormat.Uptime(new TimeSpan(3, 4, 5, 6)));
            Assert.Equal("04:05:06", ByteFormat.Uptime(new TimeSpan(4, 5, 6)));
        }

        [Fact]
        public void CpuCores_LabelsByPosition_AndRoundsText()
        {
            IReadOnlyList<MeterRow> rows = MeterRowBuilder.CpuCores(new[]
            {
                new CoreLoad(0, 0, 12.4),
                new CoreLoad(1, 0, 99.6),
            });

            Assert.Equal("Core 0", rows[0].Label);
            Assert.Equal("12 %", rows[0].ValueText);
            Assert.Equal("Core 1", rows[1].Label);
            Assert.Equal("100 %", rows[1].ValueText);
            Assert.True(rows[1].HasBar);
        }

        [Fact]
        public void CpuTotal_IsTheAverage_AndZeroForNoCores()
        {
            Assert.Equal(50.0, MeterRowBuilder.CpuTotal(new[] { new CoreLoad(0, 0, 20.0), new CoreLoad(0, 1, 80.0) })[0].Percent, 6);
            Assert.Equal(0.0, MeterRowBuilder.CpuTotal(new CoreLoad[0])[0].Percent, 6);
        }

        [Fact]
        public void Memory_ShowsUsedOverTotal()
        {
            MeterRow row = MeterRowBuilder.Memory(new MemorySample(8UL << 30, 2UL << 30, 16UL << 30, 4UL << 30))[0];

            Assert.Equal("6.0 GB / 8.0 GB", row.ValueText);
            Assert.Equal(75.0, row.Percent, 6);
        }

        [Fact]
        public void Network_ShowsOnlyTheTwoBusiestAdapters_WithoutBars()
        {
            IReadOnlyList<MeterRow> rows = MeterRowBuilder.Network(new[]
            {
                new NetworkSample("a", 2048, 1024),
                new NetworkSample("b", 10, 10),
                new NetworkSample("c", 1, 1),
            });

            Assert.Equal(2, rows.Count);
            Assert.False(rows[0].HasBar);
            Assert.Equal("↓ 2.0 KB/s  ↑ 1.0 KB/s", rows[0].ValueText);
        }

        [Fact]
        public void Temperatures_ClampTheBar_AndShowDegrees()
        {
            MeterRow row = MeterRowBuilder.Temperatures(new[] { new TemperatureReading("CPU", "Package", 123.4) })[0];

            Assert.Equal("CPU Package", row.Label);
            Assert.Equal("123 °C", row.ValueText);
            Assert.Equal(100.0, row.Percent, 6);
        }

        [Fact]
        public void Disks_AndGpuRows_CarryNamesAndBars()
        {
            MeterRow disk = MeterRowBuilder.Disks(new[] { new DiskSample("1 c:", 40.0, 1024, 0) })[0];
            GpuSample gpu = new GpuSample(new GpuAdapterInfo("l", "GPU 1", 8UL << 30), 33.0, 2UL << 30);

            Assert.Equal("R 1.0 KB/s  W 0 B/s", disk.ValueText);
            Assert.Equal("33 %", MeterRowBuilder.GpuLoad(new[] { gpu })[0].ValueText);
            Assert.Equal("2.0 GB / 8.0 GB", MeterRowBuilder.GpuMemory(new[] { gpu })[0].ValueText);
        }

        [Fact]
        public void Clock_GivesTimeAndUptimeRows_WithoutBars()
        {
            IReadOnlyList<MeterRow> rows = MeterRowBuilder.Clock(new DateTime(2026, 10, 25, 14, 5, 9), TimeSpan.FromHours(5));

            Assert.Equal(2, rows.Count);
            Assert.False(rows[0].HasBar);
            Assert.Equal("05:00:00", rows[1].ValueText);
        }
    }
}
