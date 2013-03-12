using System.Collections.ObjectModel;
using System.ComponentModel;
using ZXing;

namespace ClubcardManager.Model
{
    public class CardProvider : INotifyPropertyChanged
    {
        public string ProviderName { get; set; }
        public BarcodeFormat BarcodeFormat { get; set; }
        public string ImageUrl { get; set; }
        public string TileUrl
        {
            get
            {
                var tileName = ImageUrl.Replace(".png", "Tile.png");
                return tileName;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    public class CardProviders : ObservableCollection<CardProvider>
    {
    }
}