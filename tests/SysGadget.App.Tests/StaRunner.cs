using System;
using System.Runtime.ExceptionServices;
using System.Threading;

namespace SysGadget.App.Tests
{
    /// <summary>Runs an action on a fresh STA thread, as WPF requires, and rethrows what it throws.</summary>
    internal static class StaRunner
    {
        /// <summary>Runs the action and waits for it.</summary>
        /// <param name="action">The work to run.</param>
        public static void Run(Action action)
        {
            ExceptionDispatchInfo? failure = null;
            Thread thread = new Thread(() =>
            {
                try
                {
                    action();
                }
                catch (Exception error)
                {
                    failure = ExceptionDispatchInfo.Capture(error);
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            failure?.Throw();
        }
    }
}
