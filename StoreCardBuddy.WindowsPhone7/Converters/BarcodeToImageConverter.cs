using System;
using StoreCardBuddy.Model;
using ZXing;
using ZXing.Common;
#if !WIN8
using System.Windows.Data;
#else
using Windows.UI.Xaml.Data;
#endif

namespace StoreCardBuddy.Converters
{
    public class BarcodeToImageConverter : IValueConverter
    {
#if WIN8
        public object Convert(object value, Type targetType, object parameter, string language)
#else
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
#endif
        {
            if (value != null)
            {
                try
                {
                    var isPreview = true;
                    if (parameter != null) isPreview = bool.Parse(parameter.ToString());
                    var barcode = (Card) value;
                    if (string.IsNullOrEmpty(barcode.OriginalBarcode)) return null;

                    var writer = new BarcodeWriter
                                     {
                                         Format = barcode.CardProvider.BarcodeFormat,
                                         Options = isPreview
                                                       ? new EncodingOptions
                                                             {
                                                                 Height = 100,
                                                                 Width = 450,
                                                             }
                                                       : new EncodingOptions
                                                             {
                                                                 Height = 280,
                                                                 Width = 280
                                                             }
                                     };
                    var bmp = writer.Write(barcode.OriginalBarcode);
                    return bmp;
                }
                catch
                {
                    return new Uri("/Images/InvalidData.png", UriKind.Relative);
                }
            }
            return null;
        }

#if WIN8
        public object ConvertBack(object value, Type targetType, object parameter, string language)
#else
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
#endif
        {
            throw new NotImplementedException();
        }
    }
}
