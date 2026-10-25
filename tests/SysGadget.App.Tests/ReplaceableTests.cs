using System;
using Xunit;

namespace SysGadget.App.Tests
{
    /// <summary>
    /// Regression: switching the thin bar replaced the sampler and disposed the old one while a sample
    /// was still running on the background thread (NVML shut down under a temperature read).
    /// </summary>
    public class ReplaceableTests
    {
        private sealed class Resource : IDisposable
        {
            public int Disposed { get; private set; }

            public void Dispose()
            {
                Disposed++;
            }
        }

        [Fact]
        public void Replace_WhileInUse_DisposesTheOldOneOnlyAtRelease()
        {
            Resource first = new Resource();
            Resource second = new Resource();
            Replaceable<Resource> holder = new Replaceable<Resource>(first);

            Resource used = holder.Acquire();
            holder.Replace(second);

            Assert.Same(second, holder.Current);
            Assert.Equal(0, first.Disposed); // the running sample may still read it

            holder.Release(used);

            Assert.Equal(1, first.Disposed);
            Assert.Equal(0, second.Disposed);
        }

        [Fact]
        public void Replace_WhenIdle_DisposesTheOldOneAtOnce_AndRelease_KeepsTheCurrentOne()
        {
            Resource first = new Resource();
            Resource second = new Resource();
            Replaceable<Resource> holder = new Replaceable<Resource>(first);

            holder.Release(holder.Acquire());
            Assert.Equal(0, first.Disposed);

            holder.Replace(second);

            Assert.Equal(1, first.Disposed);
        }

        [Fact]
        public void Replace_TwiceWhileInUse_DisposesTheUnusedMiddleOneAtOnce()
        {
            Resource first = new Resource();
            Resource middle = new Resource();
            Resource last = new Resource();
            Replaceable<Resource> holder = new Replaceable<Resource>(first);

            Resource used = holder.Acquire();
            holder.Replace(middle);
            holder.Replace(last);

            Assert.Equal(1, middle.Disposed);
            Assert.Equal(0, first.Disposed);

            holder.Release(used);

            Assert.Equal(1, first.Disposed);
            Assert.Equal(0, last.Disposed);
        }

        [Fact]
        public void Acquire_Twice_Throws_AndDispose_ReleasesBoth()
        {
            Resource first = new Resource();
            Resource second = new Resource();
            Replaceable<Resource> holder = new Replaceable<Resource>(first);

            holder.Acquire();
            Assert.Throws<InvalidOperationException>(() => holder.Acquire());
            holder.Replace(second);
            holder.Dispose();

            Assert.Equal(1, first.Disposed);
            Assert.Equal(1, second.Disposed);
        }
    }
}
