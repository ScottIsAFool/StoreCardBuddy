using System.Collections.ObjectModel;
using ZXing;

namespace StoreCardBuddy.Model
{
    public class CardProviders : ObservableCollection<CardProvider>
    {
        public CardProviders()
        {
            Add(new CardProvider {ProviderName = "Tesco Clubcard", BarcodeFormat = BarcodeFormat.CODE_39, ImageUrl = "/Images/TescoClubcard.png"});
            Add(new CardProvider {ProviderName = "Nectar Card", BarcodeFormat = BarcodeFormat.CODE_128, ImageUrl = "/Images/NectarCard.png"});
            Add(new CardProvider {ProviderName = "Everyone Active", BarcodeFormat = BarcodeFormat.CODE_128, ImageUrl = "/Images/EveryoneActive.png"});
            Add(new CardProvider {ProviderName = "Pets at Home", BarcodeFormat = BarcodeFormat.CODE_128, ImageUrl = "/Images/PetsAtHome.png"});
            Add(new CardProvider {ProviderName = "Ikea", BarcodeFormat = BarcodeFormat.CODE_128, ImageUrl = "/Images/Ikea.png"});
            Add(new CardProvider {ProviderName = "Jewel-Osco", BarcodeFormat = BarcodeFormat.UPC_A, ImageUrl = "/Images/JewelOsco.png"});
            Add(new CardProvider {ProviderName = "Subway", BarcodeFormat = BarcodeFormat.DATA_MATRIX, ImageUrl = "/Images/Subway.png"});
            Add(new CardProvider{ProviderName = "Woolworths Everyday Rewards (AU)", BarcodeFormat = BarcodeFormat.EAN_13, ImageUrl = "/Images/WoolworthsAU.png"});
            Add(new CardProvider{ProviderName = "Dis-Chem", BarcodeFormat= BarcodeFormat.CODE_128, ImageUrl="/Images/DisChem.png"});
            Add(new CardProvider {ProviderName = "Other", ImageUrl = "/Images/Other.png"});
        }
    }
}
