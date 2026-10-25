namespace SysGadget.Core
{
    /// <summary>One temperature sensor reading, with its peak and the device's limits when known.</summary>
    public sealed class TemperatureReading
    {
        private readonly double? maxCelsius;

        /// <summary>Creates a reading.</summary>
        /// <param name="hardwareName">Device the sensor belongs to, for example the GPU name.</param>
        /// <param name="sensorName">Sensor name, for example "Hot Spot".</param>
        /// <param name="celsius">Temperature now, in degrees Celsius.</param>
        /// <param name="maxCelsius">Highest temperature since reading started; null when not tracked (then the current one).</param>
        /// <param name="warningCelsius">The device's warning (slow-down) limit, or null when unknown.</param>
        /// <param name="criticalCelsius">The device's critical (shut-down) limit, or null when unknown.</param>
        public TemperatureReading(
            string hardwareName,
            string sensorName,
            double celsius,
            double? maxCelsius = null,
            double? warningCelsius = null,
            double? criticalCelsius = null)
        {
            HardwareName = hardwareName;
            SensorName = sensorName;
            Celsius = celsius;
            this.maxCelsius = maxCelsius;
            WarningCelsius = warningCelsius;
            CriticalCelsius = criticalCelsius;
        }

        /// <summary>Device the sensor belongs to.</summary>
        public string HardwareName { get; }

        /// <summary>Sensor name.</summary>
        public string SensorName { get; }

        /// <summary>Temperature now, in degrees Celsius.</summary>
        public double Celsius { get; }

        /// <summary>Highest temperature since reading started; the current one when not tracked.</summary>
        public double MaxCelsius => maxCelsius ?? Celsius;

        /// <summary>The device's warning (slow-down) limit, or null when unknown.</summary>
        public double? WarningCelsius { get; }

        /// <summary>The device's critical (shut-down) limit, or null when unknown.</summary>
        public double? CriticalCelsius { get; }
    }
}
