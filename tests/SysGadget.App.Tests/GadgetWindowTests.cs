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
        /// The window is shown far off-screen so the dispatcher really builds the templates.
        /// </summary>
        [Fact]
        public void Layout_WithBarRowsAndTextRows_RaisesNoDispatcherException_AndFillsTheWindow()
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
                    new MeterSection(MeterCatalog.CpuCores, "CPU per core", new[]
                    {
                        new MeterRow("Core 0", "12 %", 12.0, true),
                        new MeterRow("Core 1", "95 %", 95.0, true),
                    }),
                    new MeterSection(MeterCatalog.Clock, "Clock and uptime", new[]
                    {
                        new MeterRow("Time", "Sun 08:50:19", 0.0, false),
                    }),
                });

                window.Show();
                Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ContextIdle);
                double height = window.ActualHeight;
                window.Close();

                Assert.Empty(dispatcherErrors);
                Assert.Equal(2, viewModel.Sections[0].Rows[1].Level);
                Assert.True(height > 40, "the rows were not laid out, height " + height);
            });
        }
    }
}
