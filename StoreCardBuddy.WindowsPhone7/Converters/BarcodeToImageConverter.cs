using System;
using System.Windows.Data;
using ClubcardManager.Model;
using MessagingToolkit.Barcode;
using ZXing;
using ZXing.Common;
using BarcodeFormat = ZXing.BarcodeFormat;

namespace ClubcardManager.Converters
{
    public class BarcodeToImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value != null)
            {
                var isPreview = true;
                if (parameter != null) isPreview = bool.Parse(parameter.ToString());
                var barcode = (Card) value;
                if (string.IsNullOrEmpty(barcode.OriginalBarcode)) return null;

                if (barcode.CardProvider.BarcodeFormat == BarcodeFormat.DATA_MATRIX) return DataMatrixImage(barcode);

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
                                                             Height = 480,
                                                             Width = 480
                                                         }
                                 };
                var bmp = writer.Write(barcode.OriginalBarcode);
                return bmp;
            }
            return null;
        }

        private object DataMatrixImage(Card barcode)
        {
            var encoder = new BarcodeEncoder();

            var bmp = encoder.Encode(MessagingToolkit.Barcode.BarcodeFormat.DataMatrix, barcode.OriginalBarcode);

            return bmp;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
