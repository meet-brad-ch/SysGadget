using System;

namespace SysGadget.Core
{
    /// <summary>An open source of samples: a read function and the provider it owns, if any.</summary>
    public sealed class SampleSource : IDisposable
    {
        private readonly Func<SampleContext, object> read;
        private readonly IDisposable? owned;

        /// <summary>Creates a source.</summary>
        /// <param name="read">Reads one snapshot.</param>
        /// <param name="owned">The provider to release with the source, or null.</param>
        public SampleSource(Func<SampleContext, object> read, IDisposable? owned)
        {
            this.read = read;
            this.owned = owned;
        }

        /// <summary>Reads one snapshot.</summary>
        /// <param name="context">The clock values.</param>
        /// <returns>The snapshot; its type depends on the source.</returns>
        public object Read(SampleContext context)
        {
            return read(context);
        }

        /// <summary>Releases the owned provider.</summary>
        public void Dispose()
        {
            owned?.Dispose();
        }
    }
}
