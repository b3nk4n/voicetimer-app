using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using PhoneKit.Framework.OS;
using VoiceTimer.Resources;
using System.Windows.Media.Imaging;

namespace VoiceTimer
{
    public partial class InfoPage : PhoneApplicationPage
    {
        public InfoPage()
        {
            InitializeComponent();

            UpdateVersionDependentContent();
        }

        /// <summary>
        /// Updates the special content for WP 8.1 OS version.
        /// </summary>
        /// <remarks>
        /// The default content is based on WP8.0 OS. 
        /// </remarks>
        private void UpdateVersionDependentContent()
        {
            if (VersionHelper.IsPhoneWP8_1)
            {
                VoiceCommandsActivationDescription.Text = AppResources.Commands1Message_8_1_OS;
                VoiceCommandsActivationImage.Source = new BitmapImage(new Uri("/Assets/Images/search.png", UriKind.Relative));
            }
        }
    }
}