using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ClubcardManager.Model;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Phone.Shell;
using ZXing;

#if WP8
using Flurry = FlurryWP8SDK;
using FlurryWP8SDK.Models;
#else
using Flurry = FlurryWP7SDK;
using FlurryWP7SDK.Models;
#endif

namespace ClubcardManager.ViewModel
{
    /// <summary>
    /// This class contains properties that the main View can data bind to.
    /// <para>
    /// Use the <strong>mvvminpc</strong> snippet to add bindable properties to this ViewModel.
    /// </para>
    /// <para>
    /// You can also use Blend to data bind with the tool's support.
    /// </para>
    /// <para>
    /// See http://www.galasoft.ch/mvvm
    /// </para>
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        private readonly INavigationService navigationService;

        private static readonly BarcodeFormat[] SupportedFormats = new[]
                                                                       {
                                                                           BarcodeFormat.CODE_128,
                                                                           BarcodeFormat.CODE_39,
                                                                           BarcodeFormat.UPC_A,
                                                                           BarcodeFormat.EAN_8, 
                                                                           BarcodeFormat.EAN_13, 
                                                                           BarcodeFormat.ITF,
                                                                           BarcodeFormat.CODABAR,
                                                                           BarcodeFormat.QR_CODE,
                                                                           BarcodeFormat.PDF_417, 
                                                                           BarcodeFormat.DATA_MATRIX,
                                                                       };

        private Result currentCard;
        private Card tempCard;

        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel(INavigationService navService)
        {
            navigationService = navService;
            Cards = new ObservableCollection<Card>();
            SelectedCards = new ObservableCollection<Card>();
            if (IsInDesignMode)
            {
                DetailsPageTitle = "add new card";
                Cards = new ObservableCollection<Card>
                            {
                                new Card
                                    {
                                        OriginalBarcode = "9794024051183961",
                                        DisplayBarcode = "634004024051183961",
                                        CardProvider = ((CardProviders) Application.Current.Resources["CardProviders"])[0],
                                        Name = "My Tesco Clubcard"
                                    }
                            };
                SelectedCard = Cards[0];
            }
            else
            {
                WireMessages();
            }
        }

        private void WireMessages()
        {
            Messenger.Default.Register<NotificationMessage>(this, m =>
            {
                if (m.Notification.Equals("ResultFoundMsg"))
                {
                    var result = (Result)m.Sender;
                    if (!SupportedFormats.Contains(result.BarcodeFormat))
                    {
                        // Show error message about unsupported barcode
                        MessageBox.Show("Sorry, this barcode is currently unsupported. If you wish to request this to be looked into, please email from the about page, state what store card it was.", "Unsupported barcode", MessageBoxButton.OK);
                        navigationService.GoBack();
                        return;
                    }

                    SelectedCard = new Card
                                       {
                                           Id = Guid.NewGuid().ToString(),
                                           OriginalBarcodeFormat = result.BarcodeFormat
                                       };

                    if (result.BarcodeFormat == BarcodeFormat.CODE_128
                        && result.Text.StartsWith("9794")
                        && result.Text.Length >= 16)
                    {
                        // This means it is almost certainly a Tesco clubcard
                        // The leading digits need to be stripped for the display name and replaced with 634004
                        // Example original: 9794024051183961
                        SelectedCard.OriginalBarcode = result.Text;
                        SelectedCard.DisplayBarcode = result.Text.Replace("9794", "634004");
                        SelectedCardIndex = 0;
                        OnSelectedCardIndexChanged();
                    }
                    else
                    {
                        SelectedCard.OriginalBarcode = SelectedCard.DisplayBarcode = result.Text;
                        SelectedCardIndex = 1;
                    }

                    // Navigate to the card editing page
                    DetailsPageTitle = "add new card";
                    navigationService.NavigateToPage("/Views/CardDetailsView.xaml");
                }
                if (m.Notification.Equals("PinnedBarcodeFound"))
                {
                    var id = (string)m.Sender;
                    var card = Cards.FirstOrDefault(x => x.Id == id);
                    if (card != default(Card))
                    {
                        SelectedCard = card;
                    }
                }
            });
        }

        public ObservableCollection<Card> SelectedCards { get; set; }
        public ObservableCollection<Card> Cards { get; set; }
        public Card SelectedCard { get; set; }
        public int SelectedCardIndex { get; set; }
        public bool CanPinToStart { get; set; }
        public bool IsInSelectionMode { get; set; }
        public int SelectedAppBarIndex { get { return IsInSelectionMode ? 1 : 0; } }

        private void OnSelectedCardIndexChanged()
        {
            SelectedCard.CardProvider = ((CardProviders)Application.Current.Resources["CardProviders"])[SelectedCardIndex];
            CheckBarcode();
            RaisePropertyChanged("SelectedCard");
        }

        private void CheckBarcode()
        {
            if (SelectedCard.CardProvider.ProviderName.Equals("Other"))
            {
                SelectedCard.CardProvider.BarcodeFormat = SelectedCard.OriginalBarcodeFormat;
            }
        }

        public string DetailsPageTitle { get; set; }

        public RelayCommand<string> NavigateToPageCommand
        {
            get
            {
                return new RelayCommand<string>(url => navigationService.NavigateToPage(url));
            }
        }

        public RelayCommand<Card> EditCardCommand
        {
            get
            {
                return new RelayCommand<Card>(card =>
                                                  {
                                                      DetailsPageTitle = "edit card details";
                                                      tempCard = card;
                                                      SelectedCard = card;
                                                      if (string.IsNullOrEmpty(card.Id)) SelectedCard.Id = Guid.NewGuid().ToString();
                                                      SetProviderIndex();
                                                      navigationService.NavigateToPage("/Views/CardDetailsView.xaml");
                                                  });
            }
        }

        private void SetProviderIndex()
        {
            var providers = ((CardProviders)Application.Current.Resources["CardProviders"]);
            for (var i = 0; i < providers.Count; i++)
            {
                if (SelectedCard.CardProvider.ProviderName == providers[i].ProviderName)
                {
                    SelectedCardIndex = i;
                    break;
                }
            }
        }

        public RelayCommand ManualEntryCommand
        {
            get
            {
                return new RelayCommand(() =>
                                            {
                                                SelectedCard = new Card{Id = Guid.NewGuid().ToString()};
                                                DetailsPageTitle = "add new card";
                                                navigationService.NavigateToPage("/Views/CardDetailsView.xaml");
                                            });
            }
        }

        public RelayCommand<Card> DeleteCardCommand
        {
            get
            {
                return new RelayCommand<Card>(card =>
                                                  {
                                                      var result = MessageBox.Show("Are you sure you wish to delete this card? This action cannot be undone.", "Are you sure?", MessageBoxButton.OKCancel);
                                                      if (result == MessageBoxResult.OK)
                                                      {
                                                          Cards.Remove(card);
                                                      }
                                                  });
            }
        }

        public RelayCommand<Card> PinToStartContextCommand
        {
            get
            {
                return new RelayCommand<Card>(card =>
                                                  {
                                                      if (string.IsNullOrEmpty(card.Id)) card.Id = Guid.NewGuid().ToString();
                                                      var existingTile = ShellTile.ActiveTiles.FirstOrDefault(x => x.NavigationUri.ToString().Contains(card.Id));
                                                      if (existingTile != default(ShellTile))
                                                      {
                                                          App.ShowMessage("That card has already been pinned");
                                                          return;
                                                      }
                                                      PinToStart(card);
                                                  });
            }
        }

        public RelayCommand PinToStartCommand
        {
            get
            {
                return new RelayCommand(() =>
                                            {
                                                if (string.IsNullOrEmpty(SelectedCard.Id)) SelectedCard.Id = Guid.NewGuid().ToString();
                                                var existingTile = ShellTile.ActiveTiles.FirstOrDefault(x => x.NavigationUri.ToString().Contains(SelectedCard.Id));
                                                if (existingTile == default(ShellTile))
                                                {
                                                    PinToStart(SelectedCard);
                                                }
                                                else
                                                {
                                                    existingTile.Delete();
                                                    CanPinToStart = true;
                                                }
                                            });
            }
        }

        private void PinToStart(Card card)
        {
            var tileUrl = string.Format("/Views/DisplayBarcodeView.xaml?id={0}", card.Id);

#if WP8
            var shellData = new FlipTileData
                                {
                                    Title = "",
                                    BackgroundImage = new Uri(card.CardProvider.TileUrl, UriKind.Relative)
                                };
            ShellTile.Create(new Uri(tileUrl, UriKind.Relative), shellData, false);
#else
            var shellData = new StandardTileData
                                {
                                    Title = "",
                                    BackgroundImage = new Uri(card.CardProvider.TileUrl, UriKind.Relative)
                                };
            ShellTile.Create(new Uri(tileUrl, UriKind.Relative), shellData);
#endif
            CanPinToStart = false;
        }

        public RelayCommand<Card> ItemTappedCommand
        {
            get
            {
                return new RelayCommand<Card>(card =>
                                                  {
                                                      SelectedCard = card;
                                                      if (string.IsNullOrEmpty(SelectedCard.Id))
                                                      {
                                                          CanPinToStart = true;
                                                          return;
                                                      }
                                                      var tile = ShellTile.ActiveTiles.FirstOrDefault(x => x.NavigationUri.ToString().Contains(card.Id));
                                                      CanPinToStart = tile == default(ShellTile);
                                                  });
            }
        }

        public RelayCommand<SelectionChangedEventArgs> SelectionChangedCommand
        {
            get
            {
                return new RelayCommand<SelectionChangedEventArgs>(args =>
                                                    {
                                                        if (args.AddedItems != null)
                                                        {
                                                            foreach (var card in args.AddedItems.Cast<Card>())
                                                            {
                                                                SelectedCards.Add(card);
                                                            }
                                                        }

                                                        if (args.RemovedItems != null)
                                                        {
                                                            foreach (var card in args.RemovedItems.Cast<Card>())
                                                            {
                                                                SelectedCards.Remove(card);
                                                            }
                                                        }
                                                    });
            }
        }

        public RelayCommand DeleteItemsCommand
        {
            get
            {
                return new RelayCommand(() =>
                                            {
                                                var result = MessageBox.Show("Are you sure you wish to delete these items? This cannot be undone.", "Are you sure?", MessageBoxButton.OKCancel);
                                                if (result == MessageBoxResult.OK)
                                                {
                                                    foreach (var card in SelectedCards)
                                                    {
                                                        Cards.Remove(card);
                                                    }
                                                }
                                            });
            }
        }

        public RelayCommand SaveCardCommand
        {
            get
            {
                return new RelayCommand(() =>
                                            {
                                                CheckBarcode();
                                                if (DetailsPageTitle != "edit card details")
                                                {
                                                    Flurry.Api.LogEvent("CardAdded", new List<Parameter>
                                                                                         {
                                                                                             new Parameter("CardProvider", SelectedCard.CardProvider.ProviderName)
                                                                                         });
                                                    Cards.Add(SelectedCard);
                                                }
                                                navigationService.GoBack();
                                            });
            }
        }

        public RelayCommand CancelCardCommand
        {
            get
            {
                return new RelayCommand(() =>
                                            {
                                                navigationService.GoBack();
                                                SelectedCard = null;
                                            });
            }
        }
    }
}