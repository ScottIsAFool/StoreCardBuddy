using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Phone.Controls;

namespace ClubcardManager.Views
{
    public partial class MainPage : PhoneApplicationPage
    {
        // Constructor
        public MainPage()
        {
            InitializeComponent();
        }

        private void MultiselectList_OnIsSelectionEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            
        }

        private void MultiselectList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            base.OnBackKeyPress(e);
            if (MultiSelectList.IsSelectionEnabled)
            {
                MultiSelectList.IsSelectionEnabled = false;
                e.Cancel = true;
            }
        }

        private void ApplicationBarIconButton_OnClick(object sender, EventArgs e)
        {
            MultiSelectList.IsSelectionEnabled = true;
        }
    }
}