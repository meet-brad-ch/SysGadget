using System.Collections.Generic;
using LibreHardwareMonitor.Hardware;
using SysGadget.Core;

namespace SysGadget.LibreHardware
{
    /// <summary>
    /// Reads temperatures through LibreHardwareMonitorLib. The library loads a kernel
    /// driver, so the process must run with administrator rights.
    /// </summary>
    public sealed class LibreHardwareTemperatureSource : ITemperatureSource
    {
        private readonly Computer computer = new Computer
        {
            IsCpuEnabled = true,
            IsGpuEnabled = true,
            IsStorageEnabled = true,
            IsMotherboardEnabled = true,
        };

        /// <summary>Opens the hardware monitor.</summary>
        public LibreHardwareTemperatureSource()
        {
            computer.Open();
        }

        /// <inheritdoc />
        public IReadOnlyList<TemperatureReading> Read()
        {
            List<TemperatureReading> readings = new List<TemperatureReading>();
            foreach (IHardware hardware in computer.Hardware)
            {
                Collect(hardware, readings);
            }

            return readings;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            computer.Close();
        }

        private static void Collect(IHardware hardware, List<TemperatureReading> readings)
        {
            hardware.Update();
            foreach (ISensor sensor in hardware.Sensors)
            {
                if (sensor.SensorType == SensorType.Temperature && sensor.Value.HasValue)
                {
                    // Max is LibreHardwareMonitor's highest value since the monitor opened
                    readings.Add(new TemperatureReading(hardware.Name, sensor.Name, sensor.Value.Value, sensor.Max));
                }
            }

            foreach (IHardware child in hardware.SubHardware)
            {
                Collect(child, readings);
            }
        }
    }
}
