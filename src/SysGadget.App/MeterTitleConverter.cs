using System;
using System.Globalization;
using System.Windows.Data;
using SysGadget.Core;

namespace SysGadget.App
{
    /// <summary>Shows the catalogue title for a meter id.</summary>
    internal sealed class MeterTitleConverter : IValueConverter
    {
        /// <inheritdoc />
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return MeterCatalog.Get((string)value).Title;
        }

        /// <inheritdoc />
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
