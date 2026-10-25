using System;
using System.Collections.Generic;

namespace SysGadget.Core
{
    /// <summary>A source of temperature readings that needs more than performance counters.</summary>
    public interface ITemperatureSource : IDisposable
    {
        /// <summary>Reads every temperature sensor.</summary>
        /// <returns>The current readings.</returns>
        IReadOnlyList<TemperatureReading> Read();
    }
}
