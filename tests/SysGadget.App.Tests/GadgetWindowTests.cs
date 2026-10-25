using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Threading;
using SysGadget.Core;
using Xunit;

namespace SysGadget.App.Tests
{
    public class GadgetWindowTests
    {
        /// <summary>
        /// Regression: ProgressBar.Value bound two-way by default to the read-only RowViewModel.Percent,
        /// which threw a XamlParseException as soon as the first row was laid out and crashed the gadget.
        /// The window is shown far off-screen so the dispatcher really builds every template: card header,
        /// line rows with and without bars, the compact strip and a collapsed card.
        /// </summary>
        [Fact]
        public void Layout_OfEveryTemplate_RaisesNoDispatcherException_AndFillsTheWindow()
        {
            StaRunner.Run(() =>
            {
                List<Exception> dispatcherErrors = new List<Exception>();
                Dispatcher.CurrentDispatcher.UnhandledException += (sender, args) =>
                {
                    dispatcherErrors.Add(args.Exception);
                    args.Handled = true;
                };

                GadgetViewModel viewModel = new GadgetViewModel();
                GadgetWindow window = new GadgetWindow(viewModel)
                {
                    WindowStartupLocation = WindowStartupLocation.Manual,
                    Left = -20000,
                    Top = 0,
                    ShowActivated = false,
                };
                viewModel.Update(new[]
                {
                    new GroupSection(MeterCatalog.CpuGroup, "CPU", false, new[]
                    {
                        new MeterRow("Total", "20 %", 20.0, true),
                        new MeterRow("Core 0", "12 %", 12.0, true, true),
                        new MeterRow("Core 1", "95 %", 95.0, true, true),
                    }),
                    new GroupSection(MeterCatalog.GpuGroup, "GPU", false, new[]
                    {
                        new MeterRow("Load", "80 %", 80.0, true),
                        new MeterRow("Down", "1.0 KB/s", 0.0, false),
                    }),
                    new GroupSection(MeterCatalog.SystemGroup, "System", true, new[]
                    {
                        new MeterRow("Time", "Sun 08:50:19", 0.0, false),
                    }),
                });

                window.Show();
                Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ContextIdle);
                double height = window.ActualHeight;
                window.Close();

                Assert.Empty(dispatcherErrors);
                Assert.True(height > 80, "the cards were not laid out, height " + height);
            });
        }
    }
}
