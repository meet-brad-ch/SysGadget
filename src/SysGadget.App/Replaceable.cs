using System;

namespace SysGadget.App
{
    /// <summary>
    /// Holds a disposable object that may be replaced while a background task still uses it.
    /// A replaced object is disposed at once when nobody uses it, otherwise when its user releases it,
    /// so a settings change never disposes the sampler under a running sample.
    /// </summary>
    /// <typeparam name="T">The held type.</typeparam>
    internal sealed class Replaceable<T> : IDisposable
        where T : class, IDisposable
    {
        private T? inUse;

        /// <summary>Holds the first object.</summary>
        /// <param name="initial">The object to hold.</param>
        public Replaceable(T initial)
        {
            Current = initial;
        }

        /// <summary>The object new work should use.</summary>
        public T Current { get; private set; }

        /// <summary>Marks the current object as in use and returns it.</summary>
        /// <returns>The current object.</returns>
        /// <exception cref="InvalidOperationException">An object is already in use; one user at a time.</exception>
        public T Acquire()
        {
            if (inUse != null)
            {
                throw new InvalidOperationException("The held object is already in use.");
            }

            inUse = Current;
            return Current;
        }

        /// <summary>Ends the use; an object that was replaced meanwhile is disposed now.</summary>
        /// <param name="used">The object returned by <see cref="Acquire"/>.</param>
        public void Release(T used)
        {
            inUse = null;
            if (!ReferenceEquals(used, Current))
            {
                used.Dispose();
            }
        }

        /// <summary>Holds a new object; the old one is disposed now unless it is in use.</summary>
        /// <param name="next">The new object.</param>
        public void Replace(T next)
        {
            T old = Current;
            Current = next;
            if (!ReferenceEquals(old, inUse))
            {
                old.Dispose();
            }
        }

        /// <summary>Disposes the current object and one still in use.</summary>
        public void Dispose()
        {
            if (inUse != null && !ReferenceEquals(inUse, Current))
            {
                inUse.Dispose();
            }

            Current.Dispose();
        }
    }
}
