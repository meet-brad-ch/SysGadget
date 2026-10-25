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

        [Fact]
        public void WriteAndDescribe_Written_NamesItsOwnFile()
        {
            string folder = Path.Combine(Path.GetTempPath(), "SysGadgetTests-" + Guid.NewGuid().ToString("N"));
            string file = Path.Combine(folder, "crash.log");
            try
            {
                string text = new CrashLog(file).WriteAndDescribe("sampling", new InvalidOperationException("failure"));

                Assert.Equal("Written to " + file + ".", text);
                Assert.Contains("[sampling]", File.ReadAllText(file));
            }
            finally
            {
                if (Directory.Exists(folder))
                {
                    Directory.Delete(folder, true);
                }
            }
        }

        [Fact]
        public void WriteAndDescribe_NotWritable_SaysWhyInsteadOfThrowing()
        {
            string folder = Path.Combine(Path.GetTempPath(), "SysGadgetTests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            try
            {
                string text = new CrashLog(folder).WriteAndDescribe("sampling", new InvalidOperationException("failure")); // a folder, not a file

                Assert.StartsWith("The crash log could not be written: ", text);
            }
            finally
            {
                Directory.Delete(folder, true);
            }
        }
    }
}
