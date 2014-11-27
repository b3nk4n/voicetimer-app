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
using PhoneKit.Framework.OS;
using Microsoft.Phone.Net.NetworkInformation;
using System.Windows.Threading;
using System.Windows.Media;
using PhoneKit.Framework.Core.Net;
using PhoneKit.Framework.Support;
using System.Windows.Controls.Primitives;

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
                StartupActionManager.Instance.Fire(e);
            }

            // determine view state
            UpdateGeneralViewState();

            if (Settings.EnableSuppressLockScreen.Value)
            {
                // disable lock screen
                PhoneApplicationService.Current.UserIdleDetectionMode = IdleDetectionMode.Disabled;
            }

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
            string seconds;
            switch (commandName)
            {
                case "startTimer1":
                    minutes = NavigationContext.QueryString["minute"];
                    HandleStartTimer1Command(minutes);
                    break;
                case "startTimer1b":
                    hours = NavigationContext.QueryString["hour"];
                    HandleStartTimer2Command(hours, "0");
                    break;
                case "startTimer1c":
                    seconds = NavigationContext.QueryString["second"];
                    HandleStartTimer1cCommand(seconds);
                    break;
                case "startTimer2":
                    hours = NavigationContext.QueryString["hour"];
                    minutes = NavigationContext.QueryString["minute"];
                    HandleStartTimer2Command(hours, minutes);
                    break;
                case "startTimer2b":
                    minutes = NavigationContext.QueryString["minute"];
                    seconds = NavigationContext.QueryString["second"];
                    HandleStartTimer2bCommand(minutes, seconds);
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
            int min = 30;
            int.TryParse(minutes, out min);

            StartTimer(min * 60, true);
        }

        /// <summary>
        /// Handles the start timer command.
        /// </summary>
        /// <param name="seconds">The length of the timer in seconds.</param>
        private void HandleStartTimer1cCommand(string seconds)
        {
            int sec = 30;
            int.TryParse(seconds, out sec);

            StartTimer(sec, false);
        }

        /// <summary>
        /// Handles the start timer command.
        /// </summary>
        /// <param name="hours">The hours to set.</param>
        /// <param name="minutes">The length to set in minutes.</param>
        private void HandleStartTimer2Command(string hours, string minutes)
        {
            int h = 0;
            int min = 30;
            int.TryParse(minutes, out min);
            int.TryParse(hours, out h);
            int totalSec = 3600 * h + 60 * min;

            StartTimer(totalSec, true);
        }

        /// <summary>
        /// Handles the start timer command.
        /// </summary>
        /// <param name="minutes">The minutes to set.</param>
        /// <param name="seconds">The length to set in seconds.</param>
        private void HandleStartTimer2bCommand(string minutes, string seconds)
        {
            int min = 0;
            int sec = 30;
            int.TryParse(minutes, out min);
            int.TryParse(seconds, out sec);
            int totalSec = 60 * min + sec;

            StartTimer(totalSec, false);
        }

        /// <summary>
        /// Starts the timer and gives audio feedback.
        /// </summary>
        /// <param name="seconds">The length to set in seconds.</param>
        private void StartTimer(int seconds, bool sayInMinutes)
        {
            if (AlarmClockViewModel.Instance.Set(seconds))
            {
                if (sayInMinutes)
                {
                    GiveVoiceFeedback(string.Format(AppResources.SpeakStartTimer, seconds / 60));
                }
                else
                {
                    GiveVoiceFeedback(string.Format(AppResources.SpeakStartTimerSeconds, seconds));
                }
            }
            else
                GiveVoiceFeedback(AppResources.SpeakAlarmAlreadySet);
        }

        /// <summary>
        /// Handles the stop timer command.
        /// </summary>
        private void HandleStopTimerCommand()
        {
            if (AlarmClockViewModel.Instance.Stop())
            {
                GiveVoiceFeedback(AppResources.SpeakStopTimer);
            }
            else
                GiveVoiceFeedback(AppResources.SpeakNoAlarmSet);
        }

        /// <summary>
        /// Handles the check alarm time command.
        /// </summary>
        private void HandleCheckAlarmTimeCommand()
        {
            if (AlarmClockViewModel.Instance.IsAlarmSet)
                GiveVoiceFeedback(string.Format(AppResources.SpeakAlarmSetFor, AlarmClockViewModel.Instance.AlarmTime.ToString("t"))); // 12:12 PM
            else
                GiveVoiceFeedback(AppResources.SpeakNoAlarmSet);
        }

        /// <summary>
        /// Handles the check remaining time command.
        /// </summary>
        private void HandleCheckRemainingTimeCommand()
        {
            if (AlarmClockViewModel.Instance.IsAlarmSet)
                GiveVoiceFeedback(string.Format(AppResources.SpeakTimeLeft, (int)AlarmClockViewModel.Instance.TimeToAlarm.TotalMinutes));
            else
                GiveVoiceFeedback(AppResources.SpeakNoAlarmSet);
        }

        /// <summary>
        /// Handles the extend alarm time command.
        /// </summary>
        /// <param name="minutes">The minutes to extend the alarm.</param>
        private void HandleExtendAlarmTime1Command(string minutes)
        {
            int min = 5;
            int.TryParse(minutes, out min);

            ExtendAlarmTime(min);
        }

        /// <summary>
        /// Handles the extend alarm time command.
        /// </summary>
        /// <param name="hours">The hours to extend the timer.</param>
        /// <param name="minutes">The minutes to extend the alarm.</param>
        private void HandleExtendAlarmTime2Command(string hours, string minutes)
        {
            int h = 0;
            int min = 30;
            int.TryParse(minutes, out min);
            int.TryParse(hours, out h);
            int totalMins = 60 * h + min;

            ExtendAlarmTime(h * 60 + min);
        }

        /// <summary>
        /// Extends the alarm time and gives audio feedback.
        /// </summary>
        /// <param name="minutes">The time to extend in minutes.</param>
        private void ExtendAlarmTime(int minutes)
        {
            if (AlarmClockViewModel.Instance.Snooze(minutes))
                GiveVoiceFeedback(string.Format(AppResources.SpeakTimeShifted, (int)AlarmClockViewModel.Instance.TimeToAlarm.TotalMinutes));
            else
                GiveVoiceFeedback(AppResources.SpeakNoAlarmSet);
        }

        /// <summary>
        /// Handles the reduce alarm time command.
        /// </summary>
        /// <param name="minutes">The minutes to reduce the alarm.</param>
        private void HandleReduceAlarmTime1Command(string minutes)
        {
            int min = 5;
            int.TryParse(minutes, out min);

            ReduceAlarmTime( min);
        }

        /// <summary>
        /// Handles the reduce alarm time command.
        /// </summary>
        /// <param name="hours">The hours to reduce the timer.</param>
        /// <param name="minutes">The minutes to reduce the alarm.</param>
        private void HandleReduceAlarmTime2Command(string hours, string minutes)
        {
            int h = 0;
            int min = 30;
            int.TryParse(minutes, out min);
            int.TryParse(hours, out h);
            int totalMins = 60 * h + min;

            ReduceAlarmTime(h * 60 + min);
        }

        /// <summary>
        /// Reduces the alarm time and gives audio feedback.
        /// </summary>
        /// <param name="minutes">The time to reduce in minutes.</param>
        private void ReduceAlarmTime(int minutes)
        {
            if (AlarmClockViewModel.Instance.Snooze(-minutes))
            {
                if (AlarmClockViewModel.Instance.TimeToAlarm.TotalSeconds > 0)
                    GiveVoiceFeedback(string.Format(AppResources.SpeakTimeShifted, (int)AlarmClockViewModel.Instance.TimeToAlarm.TotalMinutes));
                else
                    GiveVoiceFeedback(AppResources.SpeakTimeShiftedNoTimeLeft);
            }
            else
                GiveVoiceFeedback(AppResources.SpeakNoAlarmSet);
        }

        /// <summary>
        /// Speaks a text if the setting for voice feedback is active.
        /// </summary>
        /// <param name="text">The text to speak.</param>
        private async void GiveVoiceFeedback(string text)
        {
            if (Settings.EnableVoiceFeedback.Value)
                await Speech.Instance.TrySpeakTextAsync(text);
        }

        /// <summary>
        /// Builds the localized application bar with all list items.
        /// </summary>
        private void BuildLocalizedApplicationBar()
        {
            // assigns a new application bar to the page.
            ApplicationBar = new ApplicationBar();
            ApplicationBar.Opacity = 0.99;
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
        /// Handles the stop button click event.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event args.</param>
        private void StopButtonClick(object sender, RoutedEventArgs e)
        {
            DeactivateAnimation.Begin();

            UpdatePlusMinusButtons();
            UpdatePreviewTime();
        }

        /// <summary>
        /// Handles the start button click event.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event args.</param>
        private void StartAlarmClick(object sender, RoutedEventArgs e)
        {
            var seconds = (int)CustomNapTimePicker.Value.Value.TotalSeconds;
            AlarmClockViewModel.Instance.Set(seconds);
            ActivateAnimation.Begin();
        }

        /// <summary>
        /// Handles the change of the timespan using one of the buttons.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event args.</param>
        private void ChangeAlarmTimeClick(object sender, RoutedEventArgs e)
        {
            var button = sender as ButtonBase;

            if (button == null)
                return;

            int minDelta = 1;
            int.TryParse(button.Tag.ToString(), out minDelta);
            var value = CustomNapTimePicker.Value.Value;

            value = value.Add(TimeSpan.FromSeconds(minDelta));
            
            // verify at least 1 sec
            if (value.TotalSeconds < 1)
                value = TimeSpan.FromSeconds(1);

            CustomNapTimePicker.Value = value;
        }

        /// <summary>
        /// Handles the stop button click event.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event args.</param>
        private void CustomNapTimeChanged(object sender, RoutedPropertyChangedEventArgs<TimeSpan> e)
        {
            UpdatePlusMinusButtons();

            UpdatePreviewTime();
        }

        private void UpdatePlusMinusButtons()
        {
            // update button view state in inactive mode.
            var seconds = (int)CustomNapTimePicker.Value.Value.TotalSeconds;
            bool noAlarmOn = !AlarmClockViewModel.Instance.IsAlarmSet;

            ButtonMinus1.IsEnabled = seconds > 1;
            ButtonPlus1.IsEnabled = true;
        }

        /// <summary>
        /// Updates the preview sleep time.
        /// </summary>
        private void UpdatePreviewTime()
        {
            var seconds = (int)CustomNapTimePicker.Value.Value.TotalSeconds;
            AlarmClockViewModel.Instance.AlarmPreviewTime = DateTime.Now.AddSeconds(seconds);
        }
    }
}