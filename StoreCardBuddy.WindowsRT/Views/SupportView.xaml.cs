using Windows.ApplicationModel;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace StoreCardBuddy.Views
{
    public sealed partial class SupportView : UserControl
    {
        public SupportView()
        {
            this.InitializeComponent();
            var version = Package.Current.Id.Version;
            VersionText.Text = string.Format("{0}.{1}.{2}.{3}",
                                             version.Major,
                                             version.Minor,
                                             version.Build,
                                             version.Revision);
        }
    }
}
