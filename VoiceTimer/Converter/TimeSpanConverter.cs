using System;
using System.Globalization;
using System.Windows.Data;

namespace VoiceTimer.Converter
{
    /// <summary>
    /// Convert class for timespan values depending of the application configuration.
    /// </summary>
    public class TimeSpanConverter : IValueConverter
    {
        /// <summary>
        /// Converts a timespan to a non negative displayable value depending on the applications settings.
        /// </summary>
        /// <param name="value">The timespan value.</param>
        /// <param name="targetType">The target type.</param>
        /// <param name="parameter">The parameter</param>
        /// <param name="culture">The culture.</param>
        /// <returns>Returns the formatted timespan.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is TimeSpan))
                throw new ArgumentException("The argument is not a TimeSpan value.");

            var timeValue = (TimeSpan)value;

            if (timeValue.TotalMilliseconds < 0)
                return new TimeSpan();
            else
                return new DateTime(timeValue.Ticks).ToString("HH:mm:ss");
        }

        /// <summary>
        /// Backwards conversion not supported.
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
