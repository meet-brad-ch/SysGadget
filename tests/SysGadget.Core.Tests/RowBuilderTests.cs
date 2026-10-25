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
        public void Network_DownAndUpBars_AreTheShareOfTheBandwidth_FullAboveIt()
        {
            // 500 Mbit/s = 62.5 MB/s; down at 25 MB/s (40 %), up at 100 MB/s (above the bandwidth)
            IReadOnlyList<MeterRow> rows = MeterRowBuilder.Network(new[]
            {
                new NetworkSample("a", 25000000, 100000000),
                new NetworkSample("b", 10, 10),
            }, 500);

            Assert.Equal(2, rows.Count);
            Assert.Equal("Down", rows[0].Label);
            Assert.Equal(40.0, rows[0].Percent, 6);
            Assert.Equal(ByteFormat.Rate(25000000), rows[0].ValueText);
            Assert.True(rows[0].HasBar);
            Assert.Equal("Up", rows[1].Label);
            Assert.Equal(100.0, rows[1].Percent, 6);
            Assert.Empty(MeterRowBuilder.Network(new NetworkSample[0], 500));
        }

        [Fact]
        public void Temperatures_NameTheDevice_NoBar_AndUseTheDriveLimitsInsteadOfShowingThem()
        {
            IReadOnlyList<MeterRow> rows = MeterRowBuilder.Temperatures(new[]
            {
                new TemperatureReading("NVIDIA GeForce RTX 4090", "GPU Hot Spot", 60.0),
                new TemperatureReading("Samsung SSD 990 PRO 2TB", "Composite", 72.4),
                new TemperatureReading("Samsung SSD 990 PRO 2TB", "Warning Temperature", 81.0),
                new TemperatureReading("Samsung SSD 990 PRO 2TB", "Critical Temperature", 84.0),
            });

            Assert.Equal(new[] { "RTX 4090 Hot Spot", "990 PRO Composite" }, rows.Select(r => r.Label));
            Assert.Equal("60 °C", rows[0].ValueText);
            Assert.All(rows, r => Assert.False(r.HasBar));
            Assert.Equal(AlertLevel.Normal, rows[0].Alert); // no limits: below the fixed 75 °C
            Assert.Equal(AlertLevel.Warning, rows[1].Alert); // 72 °C is within 10 °C of the drive's 81 °C warning limit
        }

        [Theory]
        [InlineData("GPU Hot Spot", "Hot Spot")]
        [InlineData("GPU Memory Junction", "Memory")]
        [InlineData("Temperature 2", "2")]
        [InlineData("Temperature", "")]
        [InlineData("Composite", "Composite")]
        [InlineData("CPU Package", "Package")]
        [InlineData("GPU", "GPU")]
        public void ShortSensorName_DropsTheDevicePrefixAndTheWordTemperature(string sensor, string expected)
        {
            Assert.Equal(expected, MeterRowBuilder.ShortSensorName(sensor));
        }

        [Fact]
        public void Temperatures_SensorWithOnlyTheWordTemperature_IsLabelledWithTheDevice()
        {
            MeterRow row = Assert.Single(MeterRowBuilder.Temperatures(new[] { new TemperatureReading("KINGSTON SNV2S1000G", "Temperature", 50.0) }));

            Assert.Equal("SNV2S1000G", row.Label);
        }

        [Theory]
        [InlineData(70.0, null, null, AlertLevel.Normal)]
        [InlineData(75.0, null, null, AlertLevel.Warning)]
        [InlineData(90.0, null, null, AlertLevel.Critical)]
        [InlineData(70.9, 81.0, 84.0, AlertLevel.Normal)]
        [InlineData(71.0, 81.0, 84.0, AlertLevel.Warning)]
        [InlineData(83.9, 81.0, 84.0, AlertLevel.Warning)]
        [InlineData(84.0, 81.0, 84.0, AlertLevel.Critical)]
        [InlineData(85.0, 88.0, null, AlertLevel.Warning)]
        [InlineData(88.0, 88.0, null, AlertLevel.Critical)]
        [InlineData(86.0, null, 96.0, AlertLevel.Warning)]
        public void TemperatureAlert_UsesTheDeviceLimits_ElseFixedThresholds(double celsius, double? warning, double? critical, AlertLevel expected)
        {
            Assert.Equal(expected, MeterRowBuilder.TemperatureAlert(celsius, warning, critical));
        }

        [Fact]
        public void DiskReadAndWrite_OneRowPerDisk_NamedByDrive_NoBar_TaggedWithTheDisk()
        {
            DiskSample[] disks = { new DiskSample("0 D:", 1024, 0), new DiskSample("2", 0, 2048) };

            IReadOnlyList<MeterRow> reads = MeterRowBuilder.DiskRead(disks);
            IReadOnlyList<MeterRow> writes = MeterRowBuilder.DiskWrite(disks);

            Assert.Equal(new[] { "D: Read", "Disk 2 Read" }, reads.Select(r => r.Label));
            Assert.Equal("1.0 KB/s", reads[0].ValueText);
            Assert.Equal(new[] { "D: Write", "Disk 2 Write" }, writes.Select(r => r.Label));
            Assert.Equal("2.0 KB/s", writes[1].ValueText);
            Assert.All(reads.Concat(writes), r => Assert.False(r.HasBar));
            Assert.Equal(new[] { "0 D:", "2" }, reads.Select(r => r.Instance));
        }

        [Fact]
        public void DiskFree_ShowsFreeOfTotal_WithTheUsedShareAsBar_OrNoVolume()
        {
            DiskSpace space = new DiskSpace(1000UL << 30, 750UL << 30);

            IReadOnlyList<MeterRow> rows = MeterRowBuilder.DiskFree(new[]
            {
                new DiskSample("1 C:", 0, 0, space),
                new DiskSample("0 D:", 0, 0),
            });

            Assert.Equal("C: Free", rows[0].Label);
            Assert.Equal("750 GB of 1000 GB", rows[0].ValueText);
            Assert.Equal(25.0, rows[0].Percent, 6);
            Assert.True(rows[0].HasBar);
            Assert.Equal("D: Free", rows[1].Label);
            Assert.Equal("no volume", rows[1].ValueText);
            Assert.False(rows[1].HasBar);
            Assert.Equal(new[] { "1 C:", "0 D:" }, rows.Select(r => r.Instance));
        }

        [Theory]
        [InlineData("1 C:", new[] { "C:" })]
        [InlineData("0 D: e:", new[] { "D:", "E:" })]
        [InlineData("2", new string[0])]
        public void DriveLettersOf_ReadsTheLettersInADiskName(string diskName, string[] expected)
        {
            Assert.Equal(expected, DiskSpace.DriveLettersOf(diskName));
        }

        [Fact]
        public void DiskSpace_OfTheSystemDisk_IsPlausible()
        {
            string system = System.IO.Path.GetPathRoot(System.Environment.SystemDirectory)!.Substring(0, 2);

            DiskSpace? space = DiskSpace.Of("9 " + system);

            Assert.NotNull(space);
            Assert.True(space!.TotalBytes > space.FreeBytes);
            Assert.InRange(space.UsedPercent, 0.0, 100.0);
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
        public void GpuTemperature_LabelsTempForOneGpu_ShortNamesForMore_AndUsesTheGpuLimits()
        {
            TemperatureReading rtx = new TemperatureReading("NVIDIA GeForce RTX 4090", "GPU", 54.0, 88.0, 96.0);
            TemperatureReading hot = new TemperatureReading("NVIDIA GeForce RTX 3060", "GPU", 96.0, 88.0, 96.0);

            MeterRow single = MeterRowBuilder.GpuTemperature(new[] { rtx })[0];
            IReadOnlyList<MeterRow> two = MeterRowBuilder.GpuTemperature(new[] { rtx, hot });

            Assert.Equal("Temp", single.Label);
            Assert.Equal("54 °C", single.ValueText);
            Assert.False(single.HasBar);
            Assert.Equal(AlertLevel.Normal, single.Alert);
            Assert.Equal("RTX 4090", two[0].Label);
            Assert.Equal("RTX 3060", two[1].Label);
            Assert.Equal(AlertLevel.Critical, two[1].Alert);
        }

        [Theory]
        [InlineData("NVIDIA GeForce RTX 4090", "RTX 4090")]
        [InlineData("AMD Radeon RX 7900 XTX", "RX 7900 XTX")]
        [InlineData("NVIDIA", "NVIDIA")]
        [InlineData("Samsung SSD 990 PRO 2TB", "990 PRO")]
        [InlineData("WD_BLACK SN850X 1000GB", "WD_BLACK SN850X")]
        [InlineData("AMD Ryzen 9 7950X 16-Core Processor", "Ryzen 9 7950X")]
        [InlineData("Intel(R) UHD Graphics 770", "UHD Graphics 770")]
        [InlineData("Samsung SSD 990 PRO with Heatsink 4TB", "990 PRO")]
        [InlineData("KINGSTON SNV2S1000G", "SNV2S1000G")]
        public void ShortDeviceName_DropsVendorWordsAndCapacity(string name, string expected)
        {
            Assert.Equal(expected, MeterRowBuilder.ShortDeviceName(name));
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
