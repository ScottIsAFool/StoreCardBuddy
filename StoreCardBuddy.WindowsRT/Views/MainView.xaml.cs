// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234237

using GalaSoft.MvvmLight.Ioc;
using StoreCardBuddy.WindowsRT.Model;
using Windows.UI.Xaml.Navigation;

namespace StoreCardBuddy.WindowsRT.Views
{
    /// <summary>
    /// A basic page that provides characteristics common to most applications.
    /// </summary>
    public sealed partial class MainView : StoreCardBuddy.WindowsRT.Common.LayoutAwarePage
    {
        public MainView()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(Windows.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.NavigationMode == NavigationMode.New)
            {
                SimpleIoc.Default.GetInstance<NavigationService>().ClearBackStack();
            }
        }
    }
}
