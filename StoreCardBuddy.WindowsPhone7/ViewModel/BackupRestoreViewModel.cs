using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using ClubcardManager.Model;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Live;
using Microsoft.Live.Controls;
using Microsoft.Phone.Controls;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ClubcardManager.ViewModel
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
                                                                                        var result = await _client.GetAsync("me");
                                                                                        ProcessResult(result.Result);
#else
                                                                                        _client.GetCompleted += ClientOnGetCompleted;
                                                                                        _client.UploadCompleted += ClientOnUploadCompleted;
                                                                                        _client.GetAsync("me", "loggedin");
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

        private void ProcessResult(IDictionary<string, object> result)
        {
            LoggedInAs = string.Format("Logged in as {0}", result["name"]);
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
            _client.GetAsync("me/skydrive/files", "restorefiles");
#else
            var result = await _client.GetAsync("me/skydrive/files");
            ProcessFiles(result.Result);
#endif
        }

        private async void ProcessFiles(IDictionary<string, object> dictionary)
        {
            var fileId = string.Empty;
            foreach (IDictionary<string, object> item in (List<object>)dictionary["data"])
            {
                if (item.ContainsKey("name") && (string)item["name"] == StoreCardBuddyFile)
                {
                    if (item.ContainsKey("id"))
                    {
                        fileId = (string)item["id"];
                        break;
                    }
                }
            }

            if (!string.IsNullOrEmpty(fileId))
            {
                if (!_navigationService.IsNetworkAvailable) return;
#if !WP8
                _client.DownloadCompleted += ClientOnDownloadCompleted;
                _client.DownloadAsync(fileId + "/content");
#else
                try
                {
                    var result = await _client.DownloadAsync(fileId + "/content");
                    ParseFileContent(result.Stream);
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

            var path = "me/skydrive/";

            try
            {
                using (var stream = encodedString.ToStream())
                {
#if !WP8
                    _client.UploadAsync(path, StoreCardBuddyFile, stream, OverwriteOption.Overwrite);
#else
                    var result = await _client.UploadAsync(path, StoreCardBuddyFile, stream, OverwriteOption.Overwrite);

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
                    ProcessResult(e.Result);
                    break;
                case "restorefiles":
                    ProcessFiles(e.Result);
                    break;
            }
        }
#endif
    }
}