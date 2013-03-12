using System;
using System.Windows.Data;

namespace ClubcardManager.Converters
{
    public class PinToStartConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var canPinToStart = bool.Parse(value.ToString());
            var isText = parameter.ToString().Equals("Text");

            if (isText)
            {
                return canPinToStart ? "pin to start" : "unpin";
            }
            return canPinToStart ? new Uri("/Icons/appbar.pin.png", UriKind.Relative) : new Uri("/Icons/appbar.pin.remove.png", UriKind.Relative);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
