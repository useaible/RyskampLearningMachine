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
    public class RLVNumericConverter : IValueConverter
    {
        public RLVNumericConverter()
        {

        }

        private RLVFormatters numericFormat;
        public RLVNumericConverter(RLVFormatters convType)
        {
            numericFormat = convType;
        }
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return value;

            string format = string.Empty;
            switch(numericFormat)
            {
                case RLVFormatters.Numeric_Currency: //Currency
                    format = "{0:c}";
                    break;
                case RLVFormatters.Numeric_FixedPoint: //Fixed-point
                    format = "{0:f}";
                    break;
                case RLVFormatters.Numeric_Number: //Number
                    format = "{0:n}";
                    break;
                case RLVFormatters.Numeric_Percent: //Percent
                    format = "{0:p}";
                    break;
                default: //General (default)
                    format = "{0:g}";
                    break;
            }

            var converted = value != null? string.Format(format, Math.Round(System.Convert.ToDouble(value),2)) : null;
            return converted;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
