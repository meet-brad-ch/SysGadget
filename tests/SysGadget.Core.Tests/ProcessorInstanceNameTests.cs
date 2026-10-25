using System;
using SysGadget.Core;
using Xunit;

namespace SysGadget.Core.Tests
{
    public class ProcessorInstanceNameTests
    {
        [Fact]
        public void ParseCore_GroupAndIndex_AreRead()
        {
            ProcessorInstanceName? name = ProcessorInstanceName.ParseCore("1,23");

            Assert.NotNull(name);
            Assert.Equal(1, name!.Group);
            Assert.Equal(23, name.Index);
        }

        [Theory]
        [InlineData("_Total")]
        [InlineData("0,_Total")]
        public void ParseCore_Aggregate_ReturnsNull(string instance)
        {
            Assert.Null(ProcessorInstanceName.ParseCore(instance));
        }

        [Theory]
        [InlineData("")]
        [InlineData("3")]
        [InlineData("a,b")]
        [InlineData("0,1,2")]
        [InlineData("-1,0")]
        public void ParseCore_Malformed_Throws(string instance)
        {
            Assert.Throws<FormatException>(() => ProcessorInstanceName.ParseCore(instance));
        }
    }
}
