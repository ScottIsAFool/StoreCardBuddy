using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using LiveSDKHelper;
using LiveSDKHelper.SkyDrive;
using Microsoft.Live;
using Newtonsoft.Json;
using ReflectionIT.Windows8.Helpers;
using StoreCardBuddy.Model;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml;

namespace StoreCardBuddy.ViewModel
{
    /// <summary>
    /// This class contains properties that a View can data bind to.
    /// <para>
    /// See http://www.galasoft.ch/mvvm
    /// </para>
    /// </summary>
    public class BackupRestoreViewModel : ViewModelBase
    {
        private readonly NavigationService _navigationService;
        private const string StoreCardBuddyFile = "TheCards.txt";

        private readonly List<Scope> _scopes = new List<Scope>
                                                   {
                                                       Scope.Basic,
                                                       Scope.SignIn,
                                                       Scope.SkyDriveUpdate
                                                   };

        private LiveConnectClient _client;
        private readonly LiveAuthClient _authClient;
        private LiveLoginResult _liveLoginResult;

        /// <summary>
        /// Initializes a new instance of the BackupRestoreViewModel class.
        /// </summary>
        public BackupRestoreViewModel(NavigationService navigationService)
        {
            _navigationService = navigationService;
            _authClient = new LiveAuthClient();

            if (IsInDesignMode)
            {
                LoggedInAs = "Logged in as Scott Lovegrove";
                IsLoggedIn = true;
            }
            else
            {
                LoggedInAs = "Not logged in";
                WireMessages();
                ProgressVisibility = Visibility.Collapsed;
            }
        }

        private void WireMessages()
        {
            Messenger.Default.Register<NotificationMessage>(this, async m =>
                                                                      {
                                                                          if (m.Notification.Equals("AccountViewLoaded") || m.Notification.Equals("AppLoaded"))
                                                                          {
                                                                              _liveLoginResult = await _authClient.InitializeAsync(LiveSDKClientHelper.GetScopesStringList(_scopes));

                                                                              IsLoggedIn = _liveLoginResult.Status == LiveConnectSessionStatus.Connected;

                                                                              await GetLoginDetails();
                                                                          }

                                                                          if (m.Notification.Equals("Logout"))
                                                                          {
                                                                              if (_authClient.CanLogout)
                                                                              {
                                                                                  _authClient.Logout();
                                                                                  IsLoggedIn = false;
                                                                                  LoggedInAs = "Not logged in";
                                                                              }
                                                                          }
                                                                      });
        }

        public string ProgressText { get; set; }
        public Visibility ProgressVisibility { get; set; }

        public bool IsLoggedIn { get; set; }
        public string LoggedInAs { get; set; }

        public string LoginButtonText { get { return IsLoggedIn ? "Sign out" : "Sign in"; } }

        public RelayCommand LoginLogoutCommand
        {
            get
            {
                return new RelayCommand(async () =>
                                            {
                                                if (!IsLoggedIn)
                                                {
                                                    _liveLoginResult = await _authClient.LoginAsync(LiveSDKClientHelper.GetScopesStringList(_scopes));
                                                }
                                                else
                                                {
                                                    Messenger.Default.Send(new NotificationMessage("Logout"));
                                                    return;
                                                }

                                                if (_liveLoginResult.Status != LiveConnectSessionStatus.Connected) return;

                                                IsLoggedIn = true;

                                                await GetLoginDetails();
                                            });
            }
        }

        public RelayCommand BackupCardsCommand
        {
            get
            {
                return new RelayCommand(async () => BackupSettings());
            }
        }

        public RelayCommand RestoreCardsCommand
        {
            get
            {
                return new RelayCommand(async () =>
                                                  {
                                                      if (!IsLoggedIn) return;

                                                      if (!_navigationService.IsNetworkAvailable) return;

                                                      var result =
                                                          await
                                                          MessageBox.ShowAsync("Are you sure you want to restore your cards from SkyDrive? If you do so, any that are on your device right now would be lost. Continue?",
                                                                               "Restore your cards from SkyDrive?", MessageBoxButton.YesNo);

                                                      if (result == MessageBoxResult.Yes)
                                                      {
                                                          await DoTheRestore();
                                                      }
                                                  });
            }
        }

        private async Task DoTheRestore()
        {
            ProgressVisibility = Visibility.Visible;
            ProgressText = "Restoring...";

            var result = await _client.GetAsync(MeDetails.TopLevelSkyDriveFolder);
            ProcessFiles(result.RawResult);
        }

        private async void ProcessFiles(string result)
        {
            try
            {
                var folder = await JsonConvert.DeserializeObjectAsync<FolderDetails>(result);

                if (folder.Items == null || !folder.Items.Any())
                {
                    //App.ShowMessage("No backup could be found");
                    ProgressText = string.Empty;
                    ProgressVisibility = Visibility.Collapsed;
                    return;
                }

                var fileId = folder.Items
                                   .Where(item => item.Name == StoreCardBuddyFile)
                                   .Select(x => x.Id)
                                   .SingleOrDefault();

                if (!string.IsNullOrEmpty(fileId))
                {
                    if (!_navigationService.IsNetworkAvailable) return;

                    try
                    {
                        var file = await _client.BackgroundDownloadAsync(SkyDriveHelper.GetFile(fileId));
                        await ParseFileContent(await file.GetRandomAccessStreamAsync());
                    }
                    catch
                    {
                        MessageBox.ShowAsync("There was an error getting the file");
                    }
                }
                else
                {
                    App.ShowMessage("No backup could be found");
                    ProgressText = string.Empty;
                    ProgressVisibility = Visibility.Collapsed;
                }
            }
            catch
            {
                var s = "";
            }
        }

        private async Task ParseFileContent(IInputStream inputStream)
        {
            using (var reader = new StreamReader(inputStream.AsStreamForRead()))
            {
                var encodedBytes = Convert.FromBase64String(reader.ReadToEnd());
                var cardString = Encoding.UTF8.GetString(encodedBytes, 0, encodedBytes.Length);

                var cards = JsonConvert.DeserializeObject<ObservableCollection<Card>>(cardString);

                Messenger.Default.Send(new NotificationMessage(cards, "RestoreCards"));

                ProgressText = string.Empty;
                ProgressVisibility = Visibility.Collapsed;
            }
        }

        private void ProcessResult(string result)
        {
            var me = JsonConvert.DeserializeObject<MeDetails>(result);
            LoggedInAs = string.Format("Logged in as {0}", me.Name);
        }

        private async Task GetLoginDetails()
        {
            if (!IsLoggedIn) return;

            _client = new LiveConnectClient(_liveLoginResult.Session);

            var result = await _client.GetAsync(LiveSdkConstants.MyDetails);

            ProcessResult(result.RawResult);
        }

        private void BackupSettings()
        {
            Messenger.Default.Send(new NotificationMessageAction<ObservableCollection<Card>>("ShowMeTheCards", EncryptAndUploadCards));
        }

        private async void EncryptAndUploadCards(ObservableCollection<Card> cards)
        {
            if (!_navigationService.IsNetworkAvailable) return;

            if (_client == null || !IsLoggedIn)
            {
                App.ShowMessage("You must be logged in to be able to backup");
                return;
            }

            if (!cards.Any())
            {
                App.ShowMessage("No cards to backup");
                return;
            }

            ProgressText = "Backing up...";
            ProgressVisibility = Visibility.Visible;

            var cardString = JsonConvert.SerializeObject(cards);
            var bytes = Encoding.UTF8.GetBytes(cardString);
            var encodedString = Convert.ToBase64String(bytes);

            var tmpFile = await ApplicationData.Current.LocalFolder.CreateFileAsync("tmp.txt", CreationCollisionOption.ReplaceExisting);

            using (var writer = new StreamWriter(await tmpFile.OpenStreamForWriteAsync()))
            {
                await writer.WriteAsync(encodedString);
            }

            try
            {
                var result = await _client.BackgroundUploadAsync(MeDetails.TopLevelSkyDriveFolder, StoreCardBuddyFile, tmpFile, OverwriteOption.Overwrite);

                App.ShowMessage("Backup completed successfully.");
            }
            catch (Exception ex)
            {
                var v = "";
            }
            ProgressText = string.Empty;
            ProgressVisibility = Visibility.Collapsed;
        }
    }
}