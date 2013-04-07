using System;
using Windows.UI.Xaml.Data;

namespace StoreCardBuddy.Converters
{
    public class CountToEnabled : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            int count = int.Parse(value.ToString());
            string action = "";
            if (parameter == null)
                action = "add";
            else
                action = parameter.ToString();

            if (action.Equals("edit"))
                return count == 1;
            else if (action.Equals("delete"))
                return count > 0;
            else
                return count == 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
