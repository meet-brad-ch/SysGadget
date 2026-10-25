namespace SysGadget.Core
{
    /// <summary>One temperature sensor reading.</summary>
    public sealed class TemperatureReading
    {
        /// <summary>Creates a reading.</summary>
        /// <param name="hardwareName">Device the sensor belongs to, for example the CPU name.</param>
        /// <param name="sensorName">Sensor name, for example "CPU Package".</param>
        /// <param name="celsius">Temperature in degrees Celsius.</param>
        public TemperatureReading(string hardwareName, string sensorName, double celsius)
        {
            HardwareName = hardwareName;
            SensorName = sensorName;
            Celsius = celsius;
        }

        /// <summary>Device the sensor belongs to.</summary>
        public string HardwareName { get; }

        /// <summary>Sensor name.</summary>
        public string SensorName { get; }

        /// <summary>Temperature in degrees Celsius.</summary>
        public double Celsius { get; }
    }
}
