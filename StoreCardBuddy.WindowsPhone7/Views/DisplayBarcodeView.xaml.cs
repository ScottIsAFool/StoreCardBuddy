using GalaSoft.MvvmLight.Messaging;
using Microsoft.Phone.Controls;

namespace ClubcardManager.Views
{
    /// <summary>
    /// Description for DisplayBarcodeView.
    /// </summary>
    public partial class DisplayBarcodeView : PhoneApplicationPage
    {
        /// <summary>
        /// Initializes a new instance of the DisplayBarcodeView class.
        /// </summary>
        public DisplayBarcodeView()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            string id;
            if (NavigationContext.QueryString.TryGetValue("id", out id))
            {
                Messenger.Default.Send(new NotificationMessage(id, "PinnedBarcodeFound"));
            }
        }
    }
}