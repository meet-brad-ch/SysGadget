using System.Collections.Generic;
using System.Threading;
using SysGadget.Core;
using Xunit;

namespace SysGadget.Core.Tests
{
    /// <summary>Reads the real Windows counters of the machine running the tests.</summary>
    public class CpuCounterProviderTests
    {
        [Fact]
        public void Read_ReturnsOneLoadPerLogicalProcessor_InRange()
        {
            using (CpuCounterProvider provider = new CpuCounterProvider())
            {
                Thread.Sleep(1000);
                IReadOnlyList<CoreLoad> loads = provider.Read();

                Assert.Equal(System.Environment.ProcessorCount, loads.Count);
                Assert.Equal(provider.CoreCount, loads.Count);
                foreach (CoreLoad load in loads)
                {
                    Assert.InRange(load.Percent, 0.0, 100.0);
                }
            }
        }
    }
}
