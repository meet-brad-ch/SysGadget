using System.Drawing;
using SysGadget.Core;
using Xunit;

namespace SysGadget.Core.Tests
{
    public class WindowPlacementTests
    {
        private static readonly Rectangle Area = new Rectangle(0, 0, 3440, 1392);

        [Fact]
        public void KeepInside_LeavesAWindowInsideUnchanged()
        {
            Rectangle window = new Rectangle(100, 200, 1280, 141);

            Assert.Equal(window, WindowPlacement.KeepInside(window, Area));
        }

        [Theory]
        [InlineData(-50, 10, 0, 10)]          // past the left edge
        [InlineData(3000, 10, 2160, 10)]      // past the right edge: flush right
        [InlineData(500, -30, 500, 0)]        // past the top
        [InlineData(500, 1300, 500, 1251)]    // past the bottom (into the taskbar)
        [InlineData(-900, -900, 0, 0)]        // past a corner
        public void KeepInside_StopsFlushWithTheEdge(int left, int top, int expectedLeft, int expectedTop)
        {
            Rectangle kept = WindowPlacement.KeepInside(new Rectangle(left, top, 1280, 141), Area);

            Assert.Equal(new Rectangle(expectedLeft, expectedTop, 1280, 141), kept);
        }

        [Fact]
        public void KeepInside_AreaOfASecondMonitor_IsRespected()
        {
            Rectangle secondMonitor = new Rectangle(3440, 0, 2560, 1400);

            Rectangle kept = WindowPlacement.KeepInside(new Rectangle(5900, 50, 400, 100), secondMonitor);

            Assert.Equal(new Rectangle(5600, 50, 400, 100), kept);
        }

        [Fact]
        public void KeepInside_WindowLargerThanTheArea_AlignsLeftAndTop()
        {
            Rectangle kept = WindowPlacement.KeepInside(new Rectangle(200, 200, 4000, 1500), Area);

            Assert.Equal(new Rectangle(0, 0, 4000, 1500), kept);
        }
    }
}
