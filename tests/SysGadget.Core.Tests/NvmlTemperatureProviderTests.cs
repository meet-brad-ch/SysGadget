using System.Collections.Generic;
using SysGadget.Core;
using Xunit;

namespace SysGadget.Core.Tests
{
    /// <summary>Reads the real NVIDIA GPUs of the machine running the tests (it has one).</summary>
    public class NvmlTemperatureProviderTests
    {
        [Fact]
        public void Read_GivesAPlausibleTemperaturePerGpu_WithItsName()
        {
            Assert.True(NvmlTemperatureProvider.IsInstalled());
            using (NvmlTemperatureProvider provider = new NvmlTemperatureProvider())
            {
                IReadOnlyList<TemperatureReading> readings = provider.Read();

                Assert.NotEmpty(readings);
                foreach (TemperatureReading reading in readings)
                {
                    Assert.Contains("NVIDIA", reading.HardwareName);
                    Assert.Equal("GPU", reading.SensorName);
                    Assert.InRange(reading.Celsius, 10.0, 110.0);
                }
            }
        }
    }
}
