using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using ClubcardManager;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Live;
using Microsoft.Live.Controls;
using Microsoft.Phone.Controls;
using Newtonsoft.Json;
using LiveSDKHelper;
using LiveSDKHelper.SkyDrive;
using StoreCardBuddy.Model;

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
        private readonly INavigationService _navigationService;

        private LiveConnectClient _client;

        private const string StoreCardBuddyFile = "TheCards.txt";

        /// <summary>
        /// Initializes a new instance of the BackupRestoreViewModel class.
        /// </summary>
        public BackupRestoreViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService;
            if (IsInDesignMode)
            {
                LoggedInAs = "Logged in as Scott Lovegrove";
                IsLoggedIn = true;
            }
            else
            {
                LoggedInAs = "Not logged in";
                IsLoggedIn = false;
                WireMessages();
            }
        }

        private void WireMessages()
        {
            Messenger.Default.Register<NotificationMessage>(this, m =>
                                                                      {
                                                                          if (m.Notification.Equals("SkydriveDetails"))
                                                                          {
                                                                              ClientId = (string)m.Sender;
                                                                          }
                                                                      });
        }

        public bool ProgressIsVisible { get; set; }
        public string ProgressText { get; set; }

        public string ClientId { get; set; }

        public bool IsLoggedIn { get; set; }
        public string LoggedInAs { get; set; }

        public RelayCommand BackupPageLoaded
        {
            get
            {
                return new RelayCommand(() =>
                                            {
                                                if (string.IsNullOrEmpty(ClientId))
                                                {
                                                    MessageBox.Show("Sorry, couldn't find the app's skydrive bits and bobs, please contact the developer, it's their fault.");
                                                    _navigationService.GoBack();
                                                }
                                            });
            }
        }

        public RelayCommand<LiveConnectSessionChangedEventArgs> SessionChangedCommand
        {
            get
            {
                return new RelayCommand<LiveConnectSessionChangedEventArgs>(async args =>
                                                                                {
                                                                                    if (args.Session != null && args.Status == LiveConnectSessionStatus.Connected)
                                                                                    {
                                                                                        IsLoggedIn = true;
                                                                                        _client = new LiveConnectClient(args.Session);
#if WP8
                                                                                        var result = await _client.GetAsync(LiveSdkConstants.MyDetails);
                                                                                        ProcessResult(result.RawResult);
#else
                                                                                        _client.GetCompleted += ClientOnGetCompleted;
                                                                                        _client.UploadCompleted += ClientOnUploadCompleted;
                                                                                        _client.GetAsync(LiveSdkConstants.MyDetails, "loggedin");
#endif
                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        IsLoggedIn = false;
                                                                                        LoggedInAs = "Not logged in";
                                                                                    }
                                                                                });
            }
        }

        private void ProcessResult(string result)
        {
            var me = JsonConvert.DeserializeObject<MeDetails>(result);
            LoggedInAs = string.Format("Logged in as {0}", me.Name);
        }

        public RelayCommand BackupSettingsCommand
        {
            get
            {
                return new RelayCommand(BackupSettings);
            }
        }

        public RelayCommand RestoreSettingsCommand
        {
            get
            {
                return new RelayCommand(RestoreSettings);
            }
        }

        private void RestoreSettings()
        {
            if (!IsLoggedIn) return;

            if (!_navigationService.IsNetworkAvailable) return;

            var messageBox = new CustomMessageBox
            {
                Caption = "Restore your cards from SkyDrive?",
                Message = "Are you sure you want to restore your cards from SkyDrive? If you do so, any that are on your phone right now would be lost. Continue?",
                LeftButtonContent = "yes",
                RightButtonContent = "no",
                IsFullScreen = false
            };

            messageBox.Dismissed += (sender, args) =>
                                        {
                                            if (args.Result == CustomMessageBoxResult.LeftButton)
                                            {
                                                DoTheRestore();
                                            }
                                        };
            messageBox.Show();
        }

        private async Task DoTheRestore()
        {
            ProgressIsVisible = true;
            ProgressText = "Restoring...";
#if !WP8
            _client.GetAsync(MeDetails.TopLevelSkyDriveFolder, "restorefiles");
#else
            var result = await _client.GetAsync(MeDetails.TopLevelSkyDriveFolder);
            ProcessFiles(result.RawResult);
#endif
        }

        private async void ProcessFiles(string result)
        {
            var folder = JsonConvert.DeserializeObject<FolderDetails>(result);

            if (folder.Items == null || !folder.Items.Any())
            {
                App.ShowMessage("No backup could be found");
                ProgressText = string.Empty;
                ProgressIsVisible = false;
                return;
            }

            var fileId = folder.Items
                               .Where(item => item.Name == StoreCardBuddyFile)
                               .Select(x => x.Id)
                               .SingleOrDefault();
            
            if (!string.IsNullOrEmpty(fileId))
            {
                if (!_navigationService.IsNetworkAvailable) return;
#if !WP8
                _client.DownloadCompleted += ClientOnDownloadCompleted;
                _client.DownloadAsync(SkyDriveHelper.GetFile(fileId));
#else
                try
                {
                    var file = await _client.DownloadAsync(SkyDriveHelper.GetFile(fileId));
                    ParseFileContent(file.Stream);
                }
                catch
                {
                    App.ShowMessage("There was an error getting the file");
                }
#endif
            }
            else
            {
                App.ShowMessage("No backup could be found");
                ProgressText = string.Empty;
                ProgressIsVisible = false;
            }
        }

        private void ParseFileContent(Stream stream)
        {
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            {
                var encodedBytes = Convert.FromBase64String(reader.ReadToEnd());
                var cardString = Encoding.UTF8.GetString(encodedBytes, 0, encodedBytes.Length);

                var cards = JsonConvert.DeserializeObject<ObservableCollection<Card>>(cardString);

                Messenger.Default.Send(new NotificationMessage(cards, "RestoreCards"));
                ProgressText = string.Empty;
                ProgressIsVisible = false;
            }
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
            ProgressIsVisible = true;

            var cardString = JsonConvert.SerializeObject(cards);
            var bytes = Encoding.UTF8.GetBytes(cardString);
            var encodedString = Convert.ToBase64String(bytes);
            
            try
            {
                using (var stream = encodedString.ToStream())
                {
#if !WP8
                    _client.UploadAsync(MeDetails.TopLevelSkyDriveFolder, StoreCardBuddyFile, stream, OverwriteOption.Overwrite);
#else
                    var result = await _client.UploadAsync(MeDetails.TopLevelSkyDriveFolder, StoreCardBuddyFile, stream, OverwriteOption.Overwrite);

                    App.ShowMessage("Backup completed successfully.");
                    ProgressText = string.Empty;
                    ProgressIsVisible = false;
#endif
                }
            }
            catch (Exception ex)
            {
                var v = "";
            }
        }

#if !WP8
        private void ClientOnDownloadCompleted(object sender, LiveDownloadCompletedEventArgs e)
        {
            if (e.Error == null)
            {
                ParseFileContent(e.Result);
            }
            else
            {
                App.ShowMessage("There was an error getting the file");
            }
        }

        private void ClientOnUploadCompleted(object sender, LiveOperationCompletedEventArgs e)
        {
            App.ShowMessage(e.Error == null ? "Backup completed successfully." : "Backup failed.");

            ProgressText = string.Empty;
            ProgressIsVisible = false;
        }

        private void ClientOnGetCompleted(object sender, LiveOperationCompletedEventArgs e)
        {
            if (e.Error != null) return;

            var state = e.UserState.ToString();

            switch (state)
            {
                case "loggedin":
                    ProcessResult(e.RawResult);
                    break;
                case "restorefiles":
                    ProcessFiles(e.RawResult);
                    break;
            }
        }
#endif
    }
}