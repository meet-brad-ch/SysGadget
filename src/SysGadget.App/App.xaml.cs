using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using SysGadget.Core;

namespace SysGadget.App
{
    /// <summary>Application entry: single instance, settings, and the controller.</summary>
    public partial class App : Application, IDisposable
    {
        // the installer's AppMutex names the same mutex, so setup asks to close a running gadget
        private const string MutexName = @"Local\SysGadget.SingleInstance";
        private const int MutexWaitMs = 3000;

        private Mutex? instanceLock;
        private bool ownsInstanceLock; // a second copy that did not get the lock must not release it
        private GadgetController? controller;
        private readonly CrashLog crashLog = new CrashLog(CrashLog.DefaultPath());
        private bool reporting;

        private void OnStartup(object sender, StartupEventArgs e)
        {
            DispatcherUnhandledException += OnUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += OnDomainException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
            if (!AcquireInstanceLock())
            {
                Shutdown(0);
                return;
            }

            GadgetSettings settings;
            SettingsStore store = new SettingsStore(SettingsStore.DefaultPath());
            try
            {
                settings = store.Load();
            }
            catch (InvalidDataException error)
            {
                MessageBox.Show(error.Message, "SysGadget", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(1);
                return;
            }

            controller = new GadgetController(store, settings, crashLog);
            controller.ExitRequested += () => Shutdown(0);
            controller.Start();
        }

        /// <summary>Closes the gadget and releases the single-instance lock; called once at exit.</summary>
        public void Dispose()
        {
            controller?.Dispose();
            controller = null;
            if (instanceLock != null && ownsInstanceLock)
            {
                instanceLock.ReleaseMutex();
            }

            instanceLock?.Dispose();
            instanceLock = null;
            GC.SuppressFinalize(this);
        }

        private void OnExit(object sender, ExitEventArgs e)
        {
            Dispose();
        }

        private bool AcquireInstanceLock()
        {
            instanceLock = new Mutex(false, MutexName);
            try
            {
                ownsInstanceLock = instanceLock.WaitOne(MutexWaitMs); // a restarting copy waits for the old one to leave
            }
            catch (AbandonedMutexException)
            {
                ownsInstanceLock = true; // the previous copy ended without releasing; the lock is ours now
            }

            return ownsInstanceLock;
        }

        private void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            if (reporting)
            {
                return; // the message box pumps messages; a repeat of the same fault must not open another one
            }

            reporting = true;
            try
            {
                MessageBox.Show(crashLog.WriteAndDescribe("UI thread", e.Exception) + "\n\n" + e.Exception, "SysGadget stopped", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Shutdown(1);
            }
        }

        private void OnDomainException(object sender, UnhandledExceptionEventArgs e)
        {
            crashLog.Write("any thread", (Exception)e.ExceptionObject); // the runtime ends the process right after this
        }

        private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            crashLog.Write("unobserved task", e.Exception);
        }
    }
}
