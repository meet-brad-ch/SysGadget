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

        [Theory]
        [InlineData(41.0, 128.0, "41 / 128 GB")]
        [InlineData(0.5, 24.0, "512 MB / 24 GB")]
        public void Pair_NamesTheUnitOnce_WhenBothShareIt(double usedGb, double totalGb, string expected)
        {
            const double Gb = 1024.0 * 1024 * 1024;

            Assert.Equal(expected, ByteFormat.Pair(usedGb * Gb, totalGb * Gb));
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
        public void CpuCores_LabelsByPosition_RoundsText_AndMarksCompact()
        {
            CoreLoad[] loads = { new CoreLoad(0, 0, 12.4), new CoreLoad(1, 0, 99.6) };

            IReadOnlyList<MeterRow> rows = MeterRowBuilder.CpuCores(loads, false);
            IReadOnlyList<MeterRow> compact = MeterRowBuilder.CpuCores(loads, true);

            Assert.Equal("Core 0", rows[0].Label);
            Assert.Equal("12 %", rows[0].ValueText);
            Assert.Equal("Core 1", rows[1].Label);
            Assert.Equal("100 %", rows[1].ValueText);
            Assert.True(rows[1].HasBar);
            Assert.False(rows[0].Compact);
            Assert.True(compact[0].Compact);
            Assert.Equal(99.6, compact[1].Percent, 6);
        }

        [Fact]
        public void CpuTotal_IsTheAverage_LabelledTotal_AndZeroForNoCores()
        {
            MeterRow total = MeterRowBuilder.CpuTotal(new[] { new CoreLoad(0, 0, 20.0), new CoreLoad(0, 1, 80.0) })[0];

            Assert.Equal("Total", total.Label);
            Assert.Equal(50.0, total.Percent, 6);
            Assert.Equal(0.0, MeterRowBuilder.CpuTotal(new CoreLoad[0])[0].Percent, 6);
        }

        [Fact]
        public void Memory_AndCommit_ShowUsedOverTotal()
        {
            MemorySample sample = new MemorySample(8UL << 30, 2UL << 30, 16UL << 30, 4UL << 30);

            MeterRow ram = MeterRowBuilder.Memory(sample)[0];
            MeterRow commit = MeterRowBuilder.Commit(sample)[0];

            Assert.Equal("6.0 / 8.0 GB", ram.ValueText);
            Assert.Equal(75.0, ram.Percent, 6);
            Assert.Equal("4.0 / 16 GB", commit.ValueText);
        }

        [Fact]
        public void Network_ShowsDownAndUpOfTheBusiestAdapter_WithoutBars()
        {
            IReadOnlyList<MeterRow> rows = MeterRowBuilder.Network(new[]
            {
                new NetworkSample("a", 2048, 1024),
                new NetworkSample("b", 10, 10),
            });

            Assert.Equal(2, rows.Count);
            Assert.Equal("Down", rows[0].Label);
            Assert.Equal("2.0 KB/s", rows[0].ValueText);
            Assert.Equal("Up", rows[1].Label);
            Assert.Equal("1.0 KB/s", rows[1].ValueText);
            Assert.False(rows[0].HasBar);
            Assert.Empty(MeterRowBuilder.Network(new NetworkSample[0]));
        }

        [Fact]
        public void Temperatures_UseTheSensorName_ClampTheBar_AndShowDegrees()
        {
            MeterRow row = MeterRowBuilder.Temperatures(new[] { new TemperatureReading("CPU", "Package", 123.4) })[0];

            Assert.Equal("Package", row.Label);
            Assert.Equal("123 °C", row.ValueText);
            Assert.Equal(100.0, row.Percent, 6);
        }

        [Fact]
        public void Disks_GiveABusyBarRow_ThenARatesTextRow()
        {
            IReadOnlyList<MeterRow> rows = MeterRowBuilder.Disks(new[] { new DiskSample("1 c:", 40.0, 1024, 0) });

            Assert.Equal(2, rows.Count);
            Assert.Equal("1 c:", rows[0].Label);
            Assert.Equal("40 %", rows[0].ValueText);
            Assert.True(rows[0].HasBar);
            Assert.Equal("R 1.0 KB/s  W 0 B/s", rows[1].ValueText);
            Assert.False(rows[1].HasBar);
        }

        [Fact]
        public void GpuRows_UseItemLabelsForOneAdapter_AndShortNamesForMore()
        {
            GpuSample rtx = new GpuSample(new GpuAdapterInfo("a", "NVIDIA GeForce RTX 4090", 8UL << 30), 33.0, 2UL << 30);
            GpuSample intel = new GpuSample(new GpuAdapterInfo("b", "Intel(R) UHD Graphics 770", 1UL << 30), 5.0, 0);

            Assert.Equal("Load", MeterRowBuilder.GpuLoad(new[] { rtx })[0].Label);
            Assert.Equal("33 %", MeterRowBuilder.GpuLoad(new[] { rtx })[0].ValueText);
            Assert.Equal("VRAM", MeterRowBuilder.GpuMemory(new[] { rtx })[0].Label);
            Assert.Equal("2.0 / 8.0 GB", MeterRowBuilder.GpuMemory(new[] { rtx })[0].ValueText);
            Assert.Equal("RTX 4090", MeterRowBuilder.GpuLoad(new[] { rtx, intel })[0].Label);
            Assert.Equal("UHD Graphics 770", MeterRowBuilder.GpuMemory(new[] { rtx, intel })[1].Label);
        }

        [Fact]
        public void GpuTemperature_LabelsTempForOneGpu_ShortNamesForMore_AndClampsTheBar()
        {
            TemperatureReading rtx = new TemperatureReading("NVIDIA GeForce RTX 4090", "GPU", 54.0);
            TemperatureReading hot = new TemperatureReading("NVIDIA GeForce RTX 3060", "GPU", 104.0);

            MeterRow single = MeterRowBuilder.GpuTemperature(new[] { rtx })[0];
            IReadOnlyList<MeterRow> two = MeterRowBuilder.GpuTemperature(new[] { rtx, hot });

            Assert.Equal("Temp", single.Label);
            Assert.Equal("54 °C", single.ValueText);
            Assert.True(single.HasBar);
            Assert.Equal("RTX 4090", two[0].Label);
            Assert.Equal("RTX 3060", two[1].Label);
            Assert.Equal(100.0, two[1].Percent, 6);
        }

        [Theory]
        [InlineData("NVIDIA GeForce RTX 4090", "RTX 4090")]
        [InlineData("AMD Radeon RX 7900 XTX", "RX 7900 XTX")]
        [InlineData("NVIDIA", "NVIDIA")]
        public void ShortGpuName_DropsVendorWords(string name, string expected)
        {
            Assert.Equal(expected, MeterRowBuilder.ShortGpuName(name));
        }

        [Fact]
        public void TimeAndUptime_AreSingleRowsWithoutBars()
        {
            MeterRow time = MeterRowBuilder.Time(new DateTime(2026, 10, 25, 14, 5, 9))[0];
            MeterRow uptime = MeterRowBuilder.Uptime(TimeSpan.FromHours(5))[0];

            Assert.Equal("Time", time.Label);
            Assert.False(time.HasBar);
            Assert.Equal("Uptime", uptime.Label);
            Assert.Equal("05:00:00", uptime.ValueText);
        }
    }
}
