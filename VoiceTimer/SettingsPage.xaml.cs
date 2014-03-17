using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Windows.Phone.Speech.Synthesis;
using VoiceTimer.Resources;
using VoiceTimer.ViewModels;

namespace VoiceTimer
{
    /// <summary>
    /// The settings page.
    /// </summary>
    public partial class SettingsPage : PhoneApplicationPage
    {
        /// <summary>
        /// Creates a SettingsPage instance.
        /// </summary>
        public SettingsPage()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Load settings when page is navigated to.
        /// </summary>
        /// <param name="e">The event args.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            this.SuppressLockScreenToggleSwitch.IsChecked = Settings.EnableSuppressLockScreen.Value;
            this.VibrationToggleSwitch.IsChecked = Settings.EnableVibration.Value;
            this.VoiceFeedbackToggleSwitch.IsChecked = Settings.EnableVoiceFeedback.Value;
            BindAudioItems();
            RefreshVoiceCommandsStatus();
        }

        /// <summary>
        /// Save setting when page is navigated from.
        /// </summary>
        /// <param name="e">The event args.</param>
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            Settings.EnableSuppressLockScreen.Value = this.SuppressLockScreenToggleSwitch.IsChecked.Value;
            Settings.EnableVibration.Value = this.VibrationToggleSwitch.IsChecked.Value;
            Settings.EnableVoiceFeedback.Value = this.VoiceFeedbackToggleSwitch.IsChecked.Value;
            
            var selectedAudio = this.AudioList.SelectedItem as AudioViewModel;
            if (selectedAudio != null)
                Settings.AlarmUriString.Value = selectedAudio.UriString;
        }

        /// <summary>
        /// Refreshes the voice command status settings.
        /// </summary>
        private void RefreshVoiceCommandsStatus()
        {
            // verify the current language is supported
            if (InstalledVoices.Default.Language == "de-DE" ||
                InstalledVoices.Default.Language == "en-GB" ||
                InstalledVoices.Default.Language == "en-IN" ||
                InstalledVoices.Default.Language == "en-US")
            {
                StatusText.Text = AppResources.VoiceSupported;
                StatusMessageText.Visibility = Visibility.Collapsed;
            }
            else
            {
                StatusText.Text = AppResources.VoiceUnsupported;
                StatusMessageText.Visibility = Visibility.Visible;
            }

            LanguageText.Text = InstalledVoices.Default.Language;
        }

        /// <summary>
        /// Binds the audio items to the list picker.
        /// </summary>
        private void BindAudioItems()
        {
            if (AudioList.ItemsSource != null)
                return;

            var playImageUri = new Uri("/Assets/Images/play.png", UriKind.Relative);

            var audioList = new List<AudioViewModel>();

            audioList.Add(
                new AudioViewModel(
                    "Classic",
                    "Assets/Audio/classic.wav",
                    playImageUri));
            audioList.Add(
                new AudioViewModel(
                    "Alarm1", 
                    "Assets/Audio/alarm1.wav",
                    playImageUri));
            audioList.Add(
                new AudioViewModel(
                    "Alarm2",
                    "Assets/Audio/alarm2.wav",
                    playImageUri));
            audioList.Add(
                new AudioViewModel(
                    "Buzzer",
                    "Assets/Audio/buzzer.wav",
                    playImageUri));
            audioList.Add(
                new AudioViewModel(
                    "Rooster",
                    "Assets/Audio/rooster.wav",
                    playImageUri));
            audioList.Add(
                new AudioViewModel(
                    "Modern",
                    "Assets/Audio/modern.wav",
                    playImageUri));
            audioList.Add(
                new AudioViewModel(
                    "Dong",
                    "Assets/Audio/dong.wav",
                    playImageUri));

            AudioList.ItemsSource = audioList;

            // try to select the item
            var itemToSelect = audioList.Where(a => a.UriString == Settings.AlarmUriString.Value);

            if (itemToSelect != null && itemToSelect.Count() > 0)
                AudioList.SelectedItem = itemToSelect.First();
        }

        /// <summary>
        /// Handles the button tap event so that the item in the list picker is not selected.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event args.</param>
        private void ButtonTapAndHandleRoutedEvent(object sender, System.Windows.Input.GestureEventArgs e)
        {
            e.Handled = true;
        }
    }
}