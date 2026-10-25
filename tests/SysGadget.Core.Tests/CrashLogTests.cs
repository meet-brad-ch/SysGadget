using System;
using System.IO;
using SysGadget.Core;
using Xunit;

namespace SysGadget.Core.Tests
{
    public class CrashLogTests
    {
        [Fact]
        public void Write_AppendsEntries_WithSourceTypeAndMessage_AndCreatesTheFolder()
        {
            string folder = Path.Combine(Path.GetTempPath(), "SysGadgetTests-" + Guid.NewGuid().ToString("N"));
            string file = Path.Combine(folder, "sub", "crash.log");
            try
            {
                CrashLog log = new CrashLog(file);

                log.Write("UI thread", new InvalidOperationException("first failure"));
                log.Write("Task", new ArgumentException("second failure", new FormatException("inner cause")));

                string text = File.ReadAllText(file);
                Assert.Contains("[UI thread]", text);
                Assert.Contains("System.InvalidOperationException: first failure", text);
                Assert.Contains("[Task]", text);
                Assert.Contains("second failure", text);
                Assert.Contains("inner cause", text);
            }
            finally
            {
                if (Directory.Exists(folder))
                {
                    Directory.Delete(folder, true);
                }
            }
        }
    }
}
