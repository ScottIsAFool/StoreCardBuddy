using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using ReflectionIT.Windows8.Helpers;
using StoreCardBuddy.Model;
using StoreCardBuddy.Views;
using StoreCardBuddy.WindowsRT;
using StoreCardBuddy.WindowsRT.Model;
using StoreCardBuddy.WindowsRT.Views;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using ZXing;


namespace StoreCardBuddy.ViewModel
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
        private readonly NavigationService navigationService;

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
        public MainViewModel(NavigationService navService)
        {
            navigationService = navService;
            Cards = new ObservableCollection<Card>();
            SelectedCards = new ObservableCollection<Card>();
            if (IsInDesignMode)
            {
                IsInEditMode = true;
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
                        //MessageBox.Show("Sorry, this barcode is currently unsupported. If you wish to request this to be looked into, please email from the about page, state what store card it was.", "Unsupported barcode", MessageBoxButton.OK);
                        navigationService.GoBack();
                        return;
                    }

                    SelectedCard = new Card
                                       {
                                           Id = Guid.NewGuid().ToString(),
                                           OriginalBarcodeFormat = result.BarcodeFormat,
                                           CardProvider = ((CardProviders)Application.Current.Resources["CardProviders"])[1]
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
                    else if (result.BarcodeFormat == BarcodeFormat.CODABAR)
                    {
                        var needsStartingA = false;
                        var needsClosingA = false;
                        if (!result.Text.ToLower().StartsWith("a") &&
                            !result.Text.ToLower().StartsWith("b") &&
                            !result.Text.ToLower().StartsWith("c") &&
                            !result.Text.ToLower().StartsWith("d"))
                        {
                            needsStartingA = true;
                        }

                        if (!result.Text.ToLower().EndsWith("a") &&
                            !result.Text.ToLower().EndsWith("b") &&
                            !result.Text.ToLower().EndsWith("c") &&
                            !result.Text.ToLower().EndsWith("d"))
                        {
                            needsClosingA = true;
                        }

                        SelectedCard.OriginalBarcode = SelectedCard.DisplayBarcode = result.Text;
                        if (needsStartingA) SelectedCard.OriginalBarcode = "A" + SelectedCard.OriginalBarcode;

                        if (needsClosingA) SelectedCard.OriginalBarcode = SelectedCard.OriginalBarcode + "T";

                        SelectedCardIndex = 1;
                    }
                    else
                    {
                        SelectedCard.OriginalBarcode = SelectedCard.DisplayBarcode = result.Text;
                        SelectedCardIndex = 1;
                    }

                    // Navigate to the card editing page
                    IsInEditMode = false;
                    navigationService.Navigate<CardDetailsView>();
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
                if (m.Notification.Equals("RestoreCards"))
                {
                    Cards = (ObservableCollection<Card>) m.Sender;
                    //App.ShowMessage("Cards restored");
                }
            });

            Messenger.Default.Register<NotificationMessageAction<ObservableCollection<Card>>>(this, m =>
                                                                                                        {
                                                                                                            if (m.Notification.Equals("ShowMeTheCards"))
                                                                                                            {
                                                                                                                m.Execute(Cards);
                                                                                                            }
                                                                                                        });
        }

        public string DetailsPageTitle { get { return IsInEditMode ? "edit card details" : "add new card"; } }
        public ObservableCollection<Card> SelectedCards { get; set; }
        public ObservableCollection<Card> Cards { get; set; }
        public Card SelectedCard { get; set; }
        public int SelectedCardIndex { get; set; }
        public bool IsInEditMode { get; set; }

        private void OnSelectedCardIndexChanged()
        {
            SelectedCard.CardProvider = ((CardProviders)Application.Current.Resources["CardProviders"])[SelectedCardIndex];
            CheckBarcode();
            //RaisePropertyChanged("SelectedCard");
        }

        private void CheckBarcode()
        {
            if (SelectedCard.CardProvider.ProviderName.Equals("Other"))
            {
                SelectedCard.CardProvider.BarcodeFormat = SelectedCard.OriginalBarcodeFormat;
            }
        }

        public RelayCommand AddNewBarcodeCommand
        {
            get
            {
                return new RelayCommand(() => navigationService.Navigate<ScanBarcodeView>());
            }
        }

        public RelayCommand<Card> EditCardCommand
        {
            get
            {
                return new RelayCommand<Card>(card =>
                                                  {
                                                      IsInEditMode = true;
                                                      tempCard = card;
                                                      SelectedCard = card;
                                                      if (string.IsNullOrEmpty(card.Id)) SelectedCard.Id = Guid.NewGuid().ToString();
                                                      SetProviderIndex();
                                                      navigationService.Navigate<CardDetailsView>();
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

        public RelayCommand<Card> DeleteCardCommand
        {
            get
            {
                return new RelayCommand<Card>(async card =>
                                                  {
                                                      var result = await MessageBox.ShowAsync("Are you sure you wish to delete this card? This action cannot be undone.", "Are you sure?", MessageBoxButton.YesNo);
                                                      if (result == MessageBoxResult.Yes)
                                                      {
                                                          Cards.Remove(card);
                                                      }
                                                  });
            }
        }

        public RelayCommand<Card> ItemTappedCommand
        {
            get
            {
                return new RelayCommand<Card>(card =>
                                                  {
                                                      SelectedCard = card;
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
                return new RelayCommand(async () =>
                                                  {
                                                      var result = await MessageBox.ShowAsync("Are you sure you wish to delete these items? This cannot be undone.", "Are you sure?", MessageBoxButton.YesNo);
                                                      if (result == MessageBoxResult.Yes)
                                                      {
                                                          var temp = Cards.TakeWhile(x => !SelectedCards.Contains(x)).ToList();

                                                          Cards = new ObservableCollection<Card>(temp);
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
                                                    //Flurry.Api.LogEvent("CardAdded", new List<Parameter>
                                                    //                                     {
                                                    //                                         new Parameter("CardProvider", SelectedCard.CardProvider.ProviderName)
                                                    //                                     });
                                                    if (string.IsNullOrEmpty(SelectedCard.Name)) SelectedCard.Name = SelectedCard.CardProvider.ProviderName;
                                                    Cards.Add(SelectedCard);
                                                }
                                                navigationService.Navigate<MainView>();
                                            });
            }
        }

        public RelayCommand CancelCardCommand
        {
            get
            {
                return new RelayCommand(() =>
                                            {
                                                navigationService.Navigate<MainView>();
                                                SelectedCard = tempCard;
                                            });
            }
        }
    }
}