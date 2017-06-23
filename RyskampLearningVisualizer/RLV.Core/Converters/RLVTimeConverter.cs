using RLV.Core.Enums;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace RLV.Core.Converters
{
    public class RLVTimeConverter : IValueConverter
    {
        private RLVFormatters timeFormat;
        public RLVTimeConverter(RLVFormatters timeFormat)
        {
            this.timeFormat = timeFormat;
        }
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return value;

            double val = 0;
            if(value.GetType() == typeof(double))
            {
                val = (double)value;
            }

            switch(timeFormat)
            {
                case RLVFormatters.Time_Days:
                    return TimeSpan.FromDays(val);
                case RLVFormatters.Time_Hours:
                    return TimeSpan.FromHours(val);
                case RLVFormatters.Time_Minutes:
                    return TimeSpan.FromMinutes(val);
                case RLVFormatters.Time_Seconds:
                    return TimeSpan.FromSeconds(val);
                case RLVFormatters.Time_Milliseconds:
                    return TimeSpan.FromMinutes(val);
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
