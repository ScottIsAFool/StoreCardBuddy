using System;
using System.ComponentModel;
using System.Windows;
using ZXing;

namespace StoreCardBuddy.Model
{
    public class Card : INotifyPropertyChanged
    {
        public Card()
        {
            CardProvider = ((CardProviders) Application.Current.Resources["CardProviders"])[1];
        }
        public string Id { get; set; }
        public string Name { get; set; }
        public string DisplayBarcode { get; set; }
        public string OriginalBarcode { get; set; }
        public Uri CardBarcodeUri { get; set; }
        public CardProvider CardProvider { get; set; }
        public BarcodeFormat OriginalBarcodeFormat { get; set; }

        private void OnDisplayBarcodeChanged()
        {
            if (CardProvider.ProviderName.Equals("Tesco Clubcard"))
            {
                if (DisplayBarcode.StartsWith("634004"))
                {
                    OriginalBarcode = DisplayBarcode.Replace("634004", "9794");
                }
            }
            else
            {
                OriginalBarcode = DisplayBarcode;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
