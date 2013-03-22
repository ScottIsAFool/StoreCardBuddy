using System;
using System.Windows.Data;

namespace ClubcardManager.Converters
{
    public class AddToWalletConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var canAddToWallet = bool.Parse(value.ToString());
            var isText = parameter.ToString().Equals("Text");

            if (isText)
            {
                return canAddToWallet ? "add to wallet" : "remove from wallet";
            }
            return canAddToWallet ? new Uri("/Icons/appbar.wallet.rest.png", UriKind.Relative) : new Uri("/Icons/appbar.wallet.remove.rest.png", UriKind.Relative);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
