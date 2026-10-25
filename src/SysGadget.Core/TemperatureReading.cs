namespace SysGadget.Core
{
    /// <summary>One temperature sensor reading, with the device's limits when known.</summary>
    public sealed class TemperatureReading
    {
        /// <summary>Creates a reading.</summary>
        /// <param name="hardwareName">Device the sensor belongs to, for example the GPU name.</param>
        /// <param name="sensorName">Sensor name, for example "Hot Spot".</param>
        /// <param name="celsius">Temperature now, in degrees Celsius.</param>
        /// <param name="warningCelsius">The device's warning (slow-down) limit, or null when unknown.</param>
        /// <param name="criticalCelsius">The device's critical (shut-down) limit, or null when unknown.</param>
        public TemperatureReading(
            string hardwareName,
            string sensorName,
            double celsius,
            double? warningCelsius = null,
            double? criticalCelsius = null)
        {
            HardwareName = hardwareName;
            SensorName = sensorName;
            Celsius = celsius;
            WarningCelsius = warningCelsius;
            CriticalCelsius = criticalCelsius;
        }

        /// <summary>Device the sensor belongs to.</summary>
        public string HardwareName { get; }

        /// <summary>Sensor name.</summary>
        public string SensorName { get; }

        /// <summary>Temperature now, in degrees Celsius.</summary>
        public double Celsius { get; }

        /// <summary>The device's warning (slow-down) limit, or null when unknown.</summary>
        public double? WarningCelsius { get; }

        /// <summary>The device's critical (shut-down) limit, or null when unknown.</summary>
        public double? CriticalCelsius { get; }
    }
}
