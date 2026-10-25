using Microsoft.Win32;
using Xunit;

namespace SysGadget.App.Tests
{
    /// <summary>Writes a test value under HKCU\...\Run (not the real "SysGadget" entry) and removes it again.</summary>
    public class AutostartRegistrationTests
    {
        private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string TestValue = "SysGadget.Tests";

        [Fact]
        public void Set_AddsAndRemovesTheRunValue_ForThisExecutable_Quoted()
        {
            AutostartRegistration registration = new AutostartRegistration(@"C:\Program Files\SysGadget\SysGadget.exe", TestValue);
            try
            {
                registration.Set(true);

                Assert.True(registration.IsEnabled);
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKey)!)
                {
                    Assert.Equal("\"C:\\Program Files\\SysGadget\\SysGadget.exe\"", key.GetValue(TestValue));
                }

                Assert.False(new AutostartRegistration(@"C:\Other\SysGadget.exe", TestValue).IsEnabled); // another copy

                registration.Set(false);

                Assert.False(registration.IsEnabled);
            }
            finally
            {
                registration.Set(false);
            }
        }
    }
}
