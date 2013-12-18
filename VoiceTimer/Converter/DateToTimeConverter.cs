using System;
using System.Globalization;
using System.Windows.Data;

namespace VoiceTimer.Converter
{
    /// <summary>
    /// Converter class for extracting the time from a DateTime instance.
    /// </summary>
    public class DateToTimeConverter : IValueConverter
    {
        /// <summary>
        /// Converts a date time do a time only string.
        /// </summary>
        /// <param name="value">The date time to convert.</param>
        /// <param name="targetType">The target type.</param>
        /// <param name="parameter">The parameter.</param>
        /// <param name="culture">The culture.</param>
        /// <returns></returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {

            if (value is DateTime)
            {
                var dateValue = (DateTime)value;
                string param = string.Empty;

                if (parameter is string)
                    param = (string)parameter;

                if (dateValue == DateTime.MinValue || dateValue == DateTime.MaxValue)
                    return string.Empty;
                return dateValue.ToString(param);
            }

            return string.Empty;
        }

        /// <summary>
        /// Back conversion ot supported.
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
