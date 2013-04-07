using System;
using GalaSoft.MvvmLight.Ioc;
using Windows.Networking.Connectivity;
using Windows.UI.Xaml.Controls;

namespace StoreCardBuddy.WindowsRT.Model
{
    public class NavigationService
    {
        private Frame _frame;
        public Frame Frame
        {
            set
            {
                _frame = value;
                _originalState = _frame.GetNavigationState();
            }
        }
        private string _originalState;


        public NavigationService(Frame frame)
        {
            _frame = frame;
            _originalState = _frame.GetNavigationState();
        }

        [PreferredConstructor]
        public NavigationService()
        {

        }

        public void Navigate(Type pageType, object parameter = null)
        {
            if (_frame != null)
                _frame.Navigate(pageType, parameter);
        }

        public bool CanGoBack
        {
            get { return _frame != null && _frame.CanGoBack; }
        }

        public void GoBack()
        {
            // Use the navigation frame to return to the previous page
            if (CanGoBack) _frame.GoBack();
        }

        public bool CanGoForward
        {
            get { return _frame != null && _frame.CanGoForward; }
        }

        public void GoForward()
        {
            // Use the navigation frame to return to the previous page
            if (CanGoForward) _frame.GoForward();
        }

        public void Navigate<T>(object parameter = null)
        {
            var type = typeof(T);
            Navigate(type, parameter);
        }

        public void ClearBackStack()
        {
            _frame.SetNavigationState(_originalState);
        }

        public bool IsNetworkAvailable
        {
            get
            {
                var iConnectionProfile = NetworkInformation.GetInternetConnectionProfile();
                if (iConnectionProfile == null)
                {
                    return false;
                }

                var connectionProfileInfo = iConnectionProfile.GetNetworkConnectivityLevel();
                ConnectivityLevel = connectionProfileInfo;
                return ConnectivityLevel != NetworkConnectivityLevel.None;
            }
        }

        public NetworkConnectivityLevel ConnectivityLevel { get; protected set; }

        public event Windows.UI.Xaml.Navigation.NavigatingCancelEventHandler Navigating;

    }
}
