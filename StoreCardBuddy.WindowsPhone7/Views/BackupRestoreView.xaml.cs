using GalaSoft.MvvmLight.Messaging;
using Microsoft.Live.Controls;
using Microsoft.Phone.Controls;
using StoreCardBuddy.ViewModel;

namespace ClubcardManager.Views
{
    /// <summary>
    /// Description for BackupRestoreView.
    /// </summary>
    public partial class BackupRestoreView : PhoneApplicationPage
    {
        /// <summary>
        /// Initializes a new instance of the BackupRestoreView class.
        /// </summary>
        public BackupRestoreView()
        {
            InitializeComponent();
            Messenger.Default.Register<NotificationMessage>(this, m =>
                                                                      {
                                                                          if (m.Notification.Equals("ReloadClient"))
                                                                          {
                                                                              WLSignIn.InitializeComponent();
                                                                          }
                                                                      });
        }

        private void WLSignIn_OnSessionChanged(object sender, LiveConnectSessionChangedEventArgs e)
        {
            ((BackupRestoreViewModel)DataContext).SessionChangedCommand.Execute(e);
        }
    }
}