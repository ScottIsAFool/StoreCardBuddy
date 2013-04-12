using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Callisto.Controls.SettingsManagement;
using GalaSoft.MvvmLight.Ioc;
using StoreCardBuddy.Model;
using StoreCardBuddy.ViewModel;
using StoreCardBuddy.Views;
using WinRtUtility;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.UI.Notifications;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Application template is documented at http://go.microsoft.com/fwlink/?LinkId=234227

namespace StoreCardBuddy
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        public static void ShowMessage(string message)
        {
            var notification = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText01);
            var textElement = notification.GetElementsByTagName("text");
            
            textElement[0].AppendChild(notification.CreateTextNode(message));
            
            var toast = new ToastNotification(notification);
            
            ToastNotificationManager.CreateToastNotifier().Show(toast);
        }

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used when the application is launched to open a specific file, to display
        /// search results, and so forth.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override async void OnLaunched(LaunchActivatedEventArgs args)
        {
            Frame rootFrame = Window.Current.Content as Frame;

            await GetCards();

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                if (args.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;

                var nav = SimpleIoc.Default.GetInstance<NavigationService>();
                nav.Frame = rootFrame;
            }

            if (rootFrame.Content == null)
            {
                // When the navigation stack isn't restored navigate to the first page,
                // configuring the new page by passing required information as a navigation
                // parameter
                if (!rootFrame.Navigate(typeof(MainView), args.Arguments))
                {
                    throw new Exception("Failed to create initial page");
                }
            }
            // Ensure the current window is active
            Window.Current.Activate();

            AppSettings.Current.AddCommand<AccountSettingsView>("Account");
            AppSettings.Current.AddCommand<SupportView>("Support");
            AppSettings.Current.AddCommand<PrivacyView>("Privacy");
        }

        private async Task GetCards()
        {
            var loader = new ObjectStorageHelper<List<Card>>(StorageType.Roaming);

            //await loader.DeleteAsync("Cards");

            var cards = await loader.LoadAsync("Cards");

            if (cards != null)
                SimpleIoc.Default.GetInstance<MainViewModel>().Cards = new ObservableCollection<Card>(cards);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private async void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Save application state and stop any background activity

            var saver = new ObjectStorageHelper<List<Card>>(StorageType.Roaming);

            await saver.SaveAsync(SimpleIoc.Default.GetInstance<MainViewModel>().Cards.ToList(), "Cards");

            deferral.Complete();
        }
    }
}
