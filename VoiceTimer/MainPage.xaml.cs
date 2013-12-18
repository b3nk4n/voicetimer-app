using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using VoiceTimer.Resources;
using PhoneKit.Framework.Voice;
using VoiceTimer.ViewModels;
using Coding4Fun.Toolkit.Controls;
using PhoneKit.Framework.OS;
using Microsoft.Phone.Net.NetworkInformation;
using System.Windows.Threading;
using System.Windows.Media;
using PhoneKit.Framework.Core.Net;
using PhoneKit.Framework.Support;

namespace VoiceTimer
{
    /// <summary>
    /// The applications default page.
    /// </summary>
    public partial class MainPage : PhoneApplicationPage
    {
        /// <summary>
        /// Creates a MainPage instance.
        /// </summary>
        public MainPage()
        {
            InitializeComponent();

            // register voice commands
            Speech.Instance.InstallCommandSets(new Uri("ms-appx:///voicecommands.xml", UriKind.Absolute));

            CustomNapTimePicker.Value = AlarmClockViewModel.Instance.LastAlarmDuration;

            // late binding of timespan picker changed event
            CustomNapTimePicker.ValueChanged += CustomNapTimeChanged;

            Loaded += (s, e) =>
                {
                    // make sure all buttons are enabled/disabled properly
                    AlarmClockViewModel.Instance.UpdateCommands();

                   // Always play the blink animation, because it requires no
                   // resources when the element is collapsed
                    var timer = new DispatcherTimer();
                    timer.Tick += (se, ea) =>
                    {
                        // start clock async
                        AlarmBlinkingAnimation.Begin();
                        timer.Stop();
                    };
                    timer.Interval = TimeSpan.FromSeconds(1);
                    timer.Start();
                };

            ActivateAnimation.Completed += (s, e) =>
                {
                    UpdateGeneralViewState();
                };
            DeactivateAnimation.Completed += (s, e) =>
                {
                    UpdateGeneralViewState();
                };

            BuildLocalizedApplicationBar();

            // register startup actions
            StartupActionManager.Instance.Register(5, ActionExecutionRule.Equals, () =>
            {
                FeedbackManager.Instance.StartFirst();
            });
            StartupActionManager.Instance.Register(10, ActionExecutionRule.Equals, () =>
            {
                FeedbackManager.Instance.StartSecond();
            });
        }

        /// <summary>
        /// When main page gets active, disables idle detection (to not interrupt the speech)
        /// and try to parse voce commands from query string.
        /// </summary>
        /// <param name="e">The navigation args.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (NavigationContext.QueryString != null &&
                NavigationContext.QueryString.ContainsKey("voiceCommandName"))
            {
                String commandName = NavigationContext.QueryString["voiceCommandName"];

                if (!string.IsNullOrEmpty(commandName))
                    HandleVoiceCommands(commandName);

                // clear the QueryString or the page will retain the current value
                NavigationContext.QueryString.Clear();
            }
            else if (AlarmClockViewModel.Instance.IsAlarmNotRinging &&
                (!AlarmClockViewModel.Instance.IsAlarmSet || AlarmClockViewModel.Instance.TimeToAlarm.Minutes > 5))
            {
                // fire startup events only when the app started without voice command
                StartupActionManager.Instance.Fire();
            }

            // determine view state
            UpdateGeneralViewState();

            // set data context to view model
            DataContext = AlarmClockViewModel.Instance;
        }

        /// <summary>
        /// When the main page is navigates from.
        /// </summary>
        /// <param name="e">The event args.</param>
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            if (AlarmClockViewModel.Instance.IsAlarmRinging)
            {
                AlarmClockViewModel.Instance.Stop();
            }

            // ensure sound and vibration is off
            AlarmClockViewModel.Instance.ForceStopSoundAndVibration();
        }

        /// <summary>
        /// Updates the view state of the main page and the application bar
        /// depending on whether the alarm is on or off.
        /// <param name="withTimer">Indicates whether there should be a second async check of the message baloon.</param>
        /// </summary>
        private void UpdateGeneralViewState()
        {
            ResetRotationAnimation.Begin();

            if (AlarmClockViewModel.Instance.IsAlarmSet)
            {
                ActivePanel.Visibility = Visibility.Visible;
                InactivePanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                ActivePanel.Visibility = Visibility.Collapsed;
                InactivePanel.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// Handles the voice commands.
        /// </summary>
        /// <param name="commandName">The command name.</param>
        private void HandleVoiceCommands(string commandName)
        {
            string hours;
            string minutes;
            switch (commandName)
            {
                case "startTimer1":
                    minutes = NavigationContext.QueryString["minute"];
                    HandleStartTimer1Command(minutes);
                    break;
                case "startTimer2":
                    hours = NavigationContext.QueryString["hour"];
                    minutes = NavigationContext.QueryString["minute"];
                    HandleStartTimer2Command(hours, minutes);
                    break;
                case "stopTimer":
                    HandleStopTimerCommand();
                    break;
                case "checkAlarmTime":
                    HandleCheckAlarmTimeCommand();
                    break;
                case "checkRemainingTime":
                    HandleCheckRemainingTimeCommand();
                    break;
                case "extendAlarmTime1":
                    minutes = NavigationContext.QueryString["minute"];
                    HandleExtendAlarmTime1Command(minutes);
                    break;
                case "extendAlarmTime2":
                    hours = NavigationContext.QueryString["hour"];
                    minutes = NavigationContext.QueryString["minute"];
                    HandleExtendAlarmTime2Command(hours, minutes);
                    break;
                case "reduceAlarmTime1":
                    minutes = NavigationContext.QueryString["minute"];
                    HandleReduceAlarmTime1Command(minutes);
                    break;
                case "reduceAlarmTime2":
                    hours = NavigationContext.QueryString["hour"];
                    minutes = NavigationContext.QueryString["minute"];
                    HandleReduceAlarmTime2Command(hours, minutes);
                    break;
            }
        }

        /// <summary>
        /// Handles the start timer command.
        /// </summary>
        /// <param name="minutes">The length of the timer in minutes.</param>
        private void HandleStartTimer1Command(string minutes)
        {
            int min = int.Parse(minutes);

            StartTimer(min);
        }

        /// <summary>
        /// Handles the start timer command.
        /// </summary>
        /// <param name="hours">The hours to set.</param>
        /// <param name="minutes">The length to set in minutes.</param>
        private void HandleStartTimer2Command(string hours, string minutes)
        {
            int min = int.Parse(minutes);
            int h = int.Parse(hours);

            StartTimer(h * 60 + min);
        }

        /// <summary>
        /// Starts the timer and gives audio feedback.
        /// </summary>
        /// <param name="minutes">The length to set in minutes.</param>
        private async void StartTimer(int minutes)
        {
            if (AlarmClockViewModel.Instance.Set(minutes))
            {
                await Speech.Instance.Synthesizer.SpeakTextAsync(string.Format(AppResources.SpeakStartTimer, minutes));
            }
            else
                await Speech.Instance.Synthesizer.SpeakTextAsync(AppResources.SpeakAlarmAlreadySet);
        }

        /// <summary>
        /// Handles the stop timer command.
        /// </summary>
        private async void HandleStopTimerCommand()
        {
            if (AlarmClockViewModel.Instance.Stop())
            {
                await Speech.Instance.Synthesizer.SpeakTextAsync(AppResources.SpeakStopTimer);
            }
            else
                await Speech.Instance.Synthesizer.SpeakTextAsync(AppResources.SpeakNoAlarmSet);
        }

        /// <summary>
        /// Handles the check alarm time command.
        /// </summary>
        private async void HandleCheckAlarmTimeCommand()
        {
            if (AlarmClockViewModel.Instance.IsAlarmSet)
                await Speech.Instance.Synthesizer.SpeakTextAsync(string.Format(AppResources.SpeakAlarmSetFor, AlarmClockViewModel.Instance.AlarmTime.ToString("t"))); // 12:12 PM
            else
                await Speech.Instance.Synthesizer.SpeakTextAsync(AppResources.SpeakNoAlarmSet);
        }

        /// <summary>
        /// Handles the check remaining time command.
        /// </summary>
        private async void HandleCheckRemainingTimeCommand()
        {
            if (AlarmClockViewModel.Instance.IsAlarmSet)
                await Speech.Instance.Synthesizer.SpeakTextAsync(string.Format(AppResources.SpeakTimeLeft, (int)AlarmClockViewModel.Instance.TimeToAlarm.TotalMinutes));
            else
                await Speech.Instance.Synthesizer.SpeakTextAsync(AppResources.SpeakNoAlarmSet);
        }

        /// <summary>
        /// Handles the extend alarm time command.
        /// </summary>
        /// <param name="minutes">The minutes to extend the alarm.</param>
        private void HandleExtendAlarmTime1Command(string minutes)
        {
            int min = int.Parse(minutes);

            ExtendAlarmTime(min);
        }

        /// <summary>
        /// Handles the extend alarm time command.
        /// </summary>
        /// <param name="hours">The hours to extend the timer.</param>
        /// <param name="minutes">The minutes to extend the alarm.</param>
        private void HandleExtendAlarmTime2Command(string hours, string minutes)
        {
            int min = int.Parse(minutes);
            int h = int.Parse(hours);

            ExtendAlarmTime(h * 60 + min);
        }

        /// <summary>
        /// Extends the alarm time and gives audio feedback.
        /// </summary>
        /// <param name="minutes">The time to extend in minutes.</param>
        private async void ExtendAlarmTime(int minutes)
        {
            if (AlarmClockViewModel.Instance.Snooze(minutes))
                await Speech.Instance.Synthesizer.SpeakTextAsync(string.Format(AppResources.SpeakTimeShifted, (int)AlarmClockViewModel.Instance.TimeToAlarm.TotalMinutes));
            else
                await Speech.Instance.Synthesizer.SpeakTextAsync(AppResources.SpeakNoAlarmSet);
        }

        /// <summary>
        /// Handles the reduce alarm time command.
        /// </summary>
        /// <param name="minutes">The minutes to reduce the alarm.</param>
        private void HandleReduceAlarmTime1Command(string minutes)
        {
            int min = int.Parse(minutes);

            ReduceAlarmTime( min);
        }

        /// <summary>
        /// Handles the reduce alarm time command.
        /// </summary>
        /// <param name="hours">The hours to reduce the timer.</param>
        /// <param name="minutes">The minutes to reduce the alarm.</param>
        private void HandleReduceAlarmTime2Command(string hours, string minutes)
        {
            int min = int.Parse(minutes);
            int h = int.Parse(hours);

            ReduceAlarmTime(h * 60 + min);
        }

        /// <summary>
        /// Reduces the alarm time and gives audio feedback.
        /// </summary>
        /// <param name="minutes">The time to reduce in minutes.</param>
        private async void ReduceAlarmTime(int minutes)
        {
            if (AlarmClockViewModel.Instance.Snooze(-minutes))
            {
                if (AlarmClockViewModel.Instance.TimeToAlarm.TotalSeconds > 0)
                    await Speech.Instance.Synthesizer.SpeakTextAsync(string.Format(AppResources.SpeakTimeShifted, (int)AlarmClockViewModel.Instance.TimeToAlarm.TotalMinutes));
                else
                    await Speech.Instance.Synthesizer.SpeakTextAsync(AppResources.SpeakTimeShiftedNoTimeLeft);
            }
            else
                await Speech.Instance.Synthesizer.SpeakTextAsync(AppResources.SpeakNoAlarmSet);
        }

        /// <summary>
        /// Builds the localized application bar with all list items.
        /// </summary>
        private void BuildLocalizedApplicationBar()
        {
            // assigns a new application bar to the page.
            ApplicationBar = new ApplicationBar();
            ApplicationBar.BackgroundColor = (Color)Application.Current.Resources["ThemeBackgroundMediumColor"];
            ApplicationBar.ForegroundColor = (Color)Application.Current.Resources["ThemeForegroundLightColor"];

            // info
            ApplicationBarIconButton appBarButton1 = new ApplicationBarIconButton(new Uri("Assets/AppBar/appbar.questionmark.png", UriKind.Relative));
            appBarButton1.Text = AppResources.AppBarInfo;
            ApplicationBar.Buttons.Add(appBarButton1);
            appBarButton1.Click += (s, e) =>
            {
                NavigationService.Navigate(new Uri("/InfoPage.xaml", UriKind.Relative));
            };

            // settings
            ApplicationBarMenuItem appBarMenuItem1 = new ApplicationBarMenuItem(AppResources.SettingsTitle);
            ApplicationBar.MenuItems.Add(appBarMenuItem1);
            appBarMenuItem1.Click += (s, e) =>
            {
                NavigationService.Navigate(new Uri("/SettingsPage.xaml", UriKind.Relative));
            };

            // about
            ApplicationBarMenuItem appBarMenuItem2 = new ApplicationBarMenuItem(AppResources.AboutTitle);
            ApplicationBar.MenuItems.Add(appBarMenuItem2);
            appBarMenuItem2.Click += (s, e) =>
            {
                NavigationService.Navigate(new Uri("/AboutPage.xaml", UriKind.Relative));
            };
        }



        /// <summary>
        /// Open the timespan picker box.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event args.</param>
        private void CustomNapClick(object sender, RoutedEventArgs e)
        {
            CustomNapTimePicker.OpenPicker();
        }

        /// <summary>
        /// Handles the stop button click event.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event args.</param>
        private void StopButtonClick(object sender, RoutedEventArgs e)
        {
            DeactivateAnimation.Begin();
        }

        /// <summary>
        /// Handles the start button click event.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event args.</param>
        private void StartAlarmClick(object sender, RoutedEventArgs e)
        {
            var minutes = (int)CustomNapTimePicker.Value.Value.TotalMinutes;
            AlarmClockViewModel.Instance.Set(minutes);
            ActivateAnimation.Begin();
        }

        /// <summary>
        /// Handles the change of the timespan using one of the buttons.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event args.</param>
        private void ChangeAlarmTimeClick(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;

            if (button == null)
                return;

            int minDelta = int.Parse(button.Tag.ToString());
            var value = CustomNapTimePicker.Value.Value;

            value = value.Add(TimeSpan.FromMinutes(minDelta));
            
            // verify at least 1 min
            if (value.TotalMinutes < 1)
                value = TimeSpan.FromMinutes(1);

            CustomNapTimePicker.Value = value;
        }

        /// <summary>
        /// Handles the stop button click event.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event args.</param>
        private void CustomNapTimeChanged(object sender, RoutedPropertyChangedEventArgs<TimeSpan> e)
        {
            // update button view state in inactive mode.
            var minutes = (int)CustomNapTimePicker.Value.Value.TotalMinutes;
            bool noAlarmOn = !AlarmClockViewModel.Instance.IsAlarmSet;

            ButtonMinus5.IsEnabled = minutes > 5 && noAlarmOn;
            ButtonMinus1.IsEnabled = minutes > 1 && noAlarmOn;
            ButtonPlus5.IsEnabled = noAlarmOn;
            ButtonPlus1.IsEnabled = noAlarmOn;
        }
    }
}