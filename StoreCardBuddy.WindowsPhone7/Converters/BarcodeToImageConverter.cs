using System;
using System.Windows.Data;
using StoreCardBuddy.Model;
using ZXing;
using ZXing.Common;

namespace StoreCardBuddy.Converters
{
    public class BarcodeToImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value != null)
            {
                try
                {
                    var isPreview = true;
                    if (parameter != null) isPreview = bool.Parse(parameter.ToString());
                    var barcode = (Card) value;
                    if (string.IsNullOrEmpty(barcode.OriginalBarcode)) return null;

                    //if (barcode.CardProvider.BarcodeFormat == BarcodeFormat.DATA_MATRIX) return DataMatrixImage(barcode);

                    var writer = new BarcodeWriter
                                     {
                                         Format = barcode.CardProvider.BarcodeFormat,
                                         Options = isPreview
                                                       ? new EncodingOptions
                                                             {
                                                                 Height = 100,
                                                                 Width = 450
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

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
