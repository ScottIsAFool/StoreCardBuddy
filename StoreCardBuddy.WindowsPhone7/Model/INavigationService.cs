﻿using System;
using System.Windows.Navigation;

namespace ClubcardManager.Model
{
    public interface INavigationService
    {
        event NavigatingCancelEventHandler Navigating;
        void NavigateTo(Uri pageUri);
        void NavigateTo(string pageUri);
        void GoBack();
        bool IsNetworkAvailable { get; }
        void NavigateToPage(string link);
    }
}
