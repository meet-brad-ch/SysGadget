using System;

namespace SysGadget.Core
{
    /// <summary>What a sample needs from outside: the clock values, injected so tests control them.</summary>
    public sealed class SampleContext
    {
        /// <summary>Creates a context.</summary>
        /// <param name="now">The local time.</param>
        /// <param name="uptime">The time since boot.</param>
        public SampleContext(DateTime now, TimeSpan uptime)
        {
            Now = now;
            Uptime = uptime;
        }

        /// <summary>The local time.</summary>
        public DateTime Now { get; }

        /// <summary>The time since boot.</summary>
        public TimeSpan Uptime { get; }
    }
}
