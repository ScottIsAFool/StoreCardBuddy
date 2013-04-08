using System.ComponentModel;
using System.Runtime.Serialization;
using ZXing;

namespace StoreCardBuddy.Model
{
    public class CardProvider : INotifyPropertyChanged
    {
        public CardProvider()
        {
            
        }

        public string ProviderName { get; set; }
        public BarcodeFormat BarcodeFormat { get; set; }
        public string ImageUrl { get; set; }
        [IgnoreDataMember]
        public string TileUrl
        {
            get
            {
                var tileName = ImageUrl.Replace(".png", "Tile.png");
                return tileName;
            }
        }
        [IgnoreDataMember]
        public string SquareUrl
        {
            get
            {
                var tileName = ImageUrl.Replace(".png", "Square.png");
                return tileName;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}