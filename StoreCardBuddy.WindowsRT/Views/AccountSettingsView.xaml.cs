using GalaSoft.MvvmLight.Messaging;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace StoreCardBuddy.Views
{
    public sealed partial class AccountSettingsView : UserControl
    {
        public AccountSettingsView()
        {
            this.InitializeComponent();
            Loaded += (sender, args) => Messenger.Default.Send(new NotificationMessage("AccountViewLoaded"));
        }
    }
}
