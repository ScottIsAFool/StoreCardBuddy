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
using Windows.Storage.Streams;

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

        private List<Scope> _scopes = new List<Scope>
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
            }
        }

        private void WireMessages()
        {
            Messenger.Default.Register<NotificationMessage>(this, async m =>
                                                                      {
                                                                          if (m.Notification.Equals("AccountViewLoaded"))
                                                                          {
                                                                              _liveLoginResult = await _authClient.InitializeAsync(LiveSDKClientHelper.GetScopesString(_scopes).Split(new[] { ' ' }));

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
                                                    _liveLoginResult = await _authClient.LoginAsync(LiveSDKClientHelper.GetScopesString(_scopes).Split(new[] { ' ' }));
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
                return new RelayCommand(async () =>
                                                  {
                                                      
                                                  });
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
                                                          MessageBox.ShowAsync("Are you sure you want to restore your cards from SkyDrive? If you do so, any that are on your phone right now would be lost. Continue?",
                                                                               "Restore your cards from SkyDrive?", MessageBoxButton.YesNo);

                                                      if (result == MessageBoxResult.Yes)
                                                      {
                                                          await DoTheRestore()
                                                      }
                                                  });
            }
        }

        private async Task DoTheRestore()
        {
            ProgressIsVisible = true;
            ProgressText = "Restoring...";

            var result = await _client.GetAsync(MeDetails.TopLevelSkyDriveFolder);
            ProcessFiles(result.RawResult);
        }

        private async void ProcessFiles(string result)
        {
            var folder = JsonConvert.DeserializeObject<FolderDetails>(result);

            if (folder.Items == null || !folder.Items.Any())
            {
                //App.ShowMessage("No backup could be found");
                //ProgressText = string.Empty;
                //ProgressIsVisible = false;
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
                    //App.ShowMessage("There was an error getting the file");
                }
            }
            else
            {
                //App.ShowMessage("No backup could be found");
                //ProgressText = string.Empty;
                //ProgressIsVisible = false;
            }
        }

        private static async Task ParseFileContent(IRandomAccessStream inputStream)
        {
            using (var reader = new StreamReader(inputStream.AsStreamForRead()))
            {
                var encodedBytes = Convert.FromBase64String(reader.ReadToEnd());
                var cardString = Encoding.UTF8.GetString(encodedBytes, 0, encodedBytes.Length);

                var cards = JsonConvert.DeserializeObject<ObservableCollection<Card>>(cardString);

                Messenger.Default.Send(new NotificationMessage(cards, "RestoreCards"));
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
    }
}