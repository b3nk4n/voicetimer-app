using Microsoft.Devices;
using Microsoft.Phone.Scheduler;
using Microsoft.Phone.Shell;
using Microsoft.Xna.Framework.Audio;
using PhoneKit.Framework.Audio;
using PhoneKit.Framework.Core.MVVM;
using PhoneKit.Framework.Core.Storage;
using VoiceTimer.Resources;
using System;
using System.Windows.Input;
using System.Windows.Resources;
using System.Windows.Threading;

namespace VoiceTimer.ViewModels
{
    /// <summary>
    /// Represents a down counting alarm clock.
    /// </summary>
    public class AlarmClockViewModel : ViewModelBase
    {
        # region Members

        /// <summary>
        /// The singleton instance.
        /// </summary>
        private static AlarmClockViewModel instance;

        /// <summary>
        /// The alarms unique name.
        /// </summary>
        private const string ALARM_NAME = "powerNapAlarm";

        /// <summary>
        /// Timer to adjust the alarm time each second.
        /// </summary>
        private DispatcherTimer _dispatcherTimer;

        /// <summary>
        /// The alarm sound.
        /// </summary>
        private SoundEffectInstance _alarmSound;

        /// <summary>
        /// The progress of the total power nap [0,...,100].
        /// </summary>
        private int _progress;

        /// <summary>
        /// Indicates whether the alarm is on.
        /// </summary>
        private bool _alarmIsRinging;

        /// <summary>
        /// The alarm time.
        /// </summary>
        private readonly StoredObject<DateTime> _alarmTime = new StoredObject<DateTime>("alarmTime", DateTime.MinValue);

        /// <summary>
        /// The alarm preview time.
        /// </summary>
        private DateTime _alarmPreviewTime = DateTime.Now;

        /// <summary>
        /// The time when the user has set the alarm.
        /// </summary>
        private readonly StoredObject<DateTime> _alarmSetTime = new StoredObject<DateTime>("alarmSetTime", DateTime.MinValue);

        /// <summary>
        /// The time when the user has set the alarm.
        /// </summary>
        private readonly StoredObject<TimeSpan> _lastAlarmDuration = new StoredObject<TimeSpan>("lastAlarmDuration", TimeSpan.FromMinutes(20));

        /// <summary>
        /// the phones alarm scheduler.
        /// </summary>
        private static Alarm alarm;

        /// <summary>
        /// The start alarm command.
        /// </summary>
        private DelegateCommand<string> _startCommand;

        /// <summary>
        /// The snooze alarm command.
        /// </summary>
        private DelegateCommand<string> _snoozeCommand;

        /// <summary>
        /// The anti snooze alarm command.
        /// </summary>
        private DelegateCommand<string> _antiSnoozeCommand;

        /// <summary>
        /// The stop alarm command.
        /// </summary>
        private DelegateCommand _stopCommand;

        /// <summary>
        /// Alarm seconds counter to have an alarm sound in a regular interval.
        /// </summary>
        private int _alarmStartCounter = 0;

        /// <summary>
        /// The alarm interval in seconds when the app is active.
        /// </summary>
        private const int ALARM_INTERVAL = 5;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates an AlarmClock instance.
        /// </summary>
        private AlarmClockViewModel()
        {
            // timer
            _dispatcherTimer = new DispatcherTimer();
            _dispatcherTimer.Tick += dispatcherTimer_Tick;
            _dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
            _dispatcherTimer.Start();

            // alarm
            AlarmClockViewModel.alarm = new Alarm(ALARM_NAME);
            AlarmClockViewModel.alarm.ExpirationTime = DateTime.MaxValue;
            AlarmClockViewModel.alarm.RecurrenceType = RecurrenceInterval.None;
            AlarmClockViewModel.alarm.Content = AppResources.AlertDialogMessage;

            // commands
            _startCommand = new DelegateCommand<string>(
                (minutes) =>
                {
                    int min = 30;
                    int.TryParse(minutes, out min);
                    Set(min);
                },
                (minutes) =>
                {
                    return !IsAlarmSet;
                });

            _snoozeCommand = new DelegateCommand<string>(
                (minutes) =>
                {
                    int min = 5;
                    int.TryParse(minutes, out min);
                    Snooze(min);
                },
                (minutes) =>
                {
                    return IsAlarmSet;
                });

            _antiSnoozeCommand = new DelegateCommand<string>(
                (minutes) =>
                {
                    int min = 1;
                    int.TryParse(minutes, out min);
                    Snooze(-min);
                },
                (minutes) =>
                {
                    int min = 1;
                    int.TryParse(minutes, out min);
                    return IsAlarmSet && TimeToAlarm.TotalMinutes > min;
                });

            _stopCommand = new DelegateCommand(
                () =>
                {
                    Stop();
                },
                () =>
                {
                    return IsAlarmSet;
                });
        }

        #endregion

        #region Methods

        /// <summary>
        /// Tries to set the alarm.
        /// </summary>
        /// <param name="minutes">The alarm time in minutes until now.</param>
        /// <returns>Returns true if successful, else false.</returns>
        public bool Set(int minutes)
        {
            if (minutes < 1)
                throw new ArgumentException("Alarm time must be at least 1 minute.");

            if (!IsAlarmSet)
            {
                AlarmSetTime = DateTime.Now;
                AlarmTime = DateTime.Now.AddMinutes(minutes);

                // save alarm duration
                _lastAlarmDuration.Value = TimeSpan.FromMinutes(minutes);

                UpdateCommands();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Snoozes the alarm time.
        /// </summary>
        /// <param name="minutes">The snooze time.</param>
        /// <returns>Returns true if successful, else false.</returns>
        public bool Snooze(int minutes)
        {
            if (minutes == 0)
                throw new ArgumentException("Alarm snooze time must be non zero.");

            if (IsAlarmSet)
            {
                DateTime newAlarmBaseTime;

                // verify new alarm time offset is not based on a passed time
                if (AlarmTime < DateTime.Now)
                    newAlarmBaseTime = DateTime.Now;
                else
                    newAlarmBaseTime = AlarmTime;

                if (minutes > 0)
                {
                    AlarmTime = newAlarmBaseTime.AddMinutes(minutes);
                }
                else if (TimeToAlarm.TotalMinutes > minutes)
                {
                    AlarmTime = newAlarmBaseTime.AddMinutes(minutes);
                }

                UpdateCommands();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Stops the alarm.
        /// </summary>
        public bool Stop()
        {
            if (IsAlarmSet)
            {
                AlarmTime = DateTime.MinValue;
                AlarmSetTime = DateTime.MinValue;
                IsAlarmRinging = false;
                UpdateCommands();

                // enable lock screen
                if (!Settings.EnableSuppressLockScreen.Value)
                {
                    PhoneApplicationService.Current.UserIdleDetectionMode = IdleDetectionMode.Enabled;
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Tries to add the alarm scheduler.
        /// </summary>
        public void TryAddToScheduler()
        {
            if (IsAlarmSet)
            {
                try
                {
                    if (AlarmTime > DateTime.Now)
                    {
                        DateTime beginTime;

                        // ensure alarm-timer is not set under 30 seconds
                        if (TimeToAlarm.TotalSeconds > 30)
                            beginTime = AlarmTime;
                        else
                            beginTime = DateTime.Now.AddSeconds(30);

                        AlarmClockViewModel.alarm.BeginTime = beginTime;
                        AlarmClockViewModel.alarm.Sound = new Uri(Settings.AlarmUriString.Value, UriKind.Relative);
                        ScheduledActionService.Add(AlarmClockViewModel.alarm);
                    }
                }
                catch (Exception)
                {
                    // do nothing, only to prevent the ScheduledActionService from adding
                    // the alarm member variable more than once
                }
            }
        }

        /// <summary>
        /// Tries to remove the alarm scheduler.
        /// </summary>
        public void TryRemoveFromScheduler()
        {
            var oldAlarm = ScheduledActionService.Find(ALARM_NAME) as Alarm;

            if (oldAlarm != null)
            {
                // check if alarm was dismissed
                if (oldAlarm.IsScheduled == false)
                {
                    Stop();
                }

                ScheduledActionService.Remove(ALARM_NAME);
            }
        }

        /// <summary>
        /// Forces the app to be silent.
        /// </summary>
        public void ForceStopSoundAndVibration()
        {
            if (_alarmSound != null && !_alarmSound.IsDisposed)
                _alarmSound.Stop();

            VibrateController.Default.Stop();
        }

        /// <summary>
        /// Updates the binded button states depending on the CanExecute function.
        /// </summary>
        public void UpdateCommands()
        {
            _snoozeCommand.RaiseCanExecuteChanged();
            _antiSnoozeCommand.RaiseCanExecuteChanged();
            _stopCommand.RaiseCanExecuteChanged();
            _startCommand.RaiseCanExecuteChanged();
        }

        /// <summary>
        /// Updates the alarm click every second.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event args.</param>
        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            if (IsAlarmSet)
            {
                var totalSeconds = (AlarmTime - AlarmSetTime).TotalSeconds;
                var passedSeconds = (DateTime.Now - AlarmSetTime).TotalSeconds;

                if (totalSeconds == 0)
                    Progress = 0;
                else
                    Progress = (int)(100 * passedSeconds / totalSeconds);

                if (TimeToAlarm.TotalSeconds <= 0 && TimeToAlarm.TotalSeconds >= -60)
                {
                    IsAlarmRinging = true;
                    _alarmStartCounter++;

                    // vibrate only if enabled
                    if (Settings.EnableVibration.Value)
                    {
                        if (_alarmStartCounter % ALARM_INTERVAL == ALARM_INTERVAL - 1 || _alarmStartCounter % ALARM_INTERVAL == ALARM_INTERVAL - 2)
                            VibrateController.Default.Start(TimeSpan.FromSeconds(0.5));
                    }

                    if (_alarmSound == null)
                    {
                        StreamResourceInfo alarmResource = App.GetResourceStream(new Uri(Settings.AlarmUriString.Value, UriKind.Relative));
                        SoundEffects.Instance.Load(Settings.AlarmUriString.Value, alarmResource.Stream);
                        SoundEffect sound = SoundEffects.Instance[Settings.AlarmUriString.Value];
                        if (sound != null) // there is a very little chance that the sound file could not be loaded.
                        {
                            _alarmSound = SoundEffects.Instance[Settings.AlarmUriString.Value].CreateInstance();
                            // start silent (but 0 is a too silent start)
                            _alarmSound.Volume = 0.2f;
                        }
                    }

                    if (_alarmStartCounter % ALARM_INTERVAL == 0)
                    {
                        // get slightly louder
                        _alarmSound.Volume = Math.Min(1.0f, _alarmSound.Volume + 0.1f);

                        TryPlayAlarmSound();
                    }
                }
                else
                {
                    TryStopAlarmSound();
                    IsAlarmRinging = false;
                }

                // disable lockscreen 10 sec before the alarm starts. This ensures that the app will not locked out
                // shortly before the alarm or during the alarm.
                if ((int)TimeToAlarm.TotalSeconds == 10)
                {
                    // disable lock screen
                    PhoneApplicationService.Current.UserIdleDetectionMode = IdleDetectionMode.Disabled;
                }

                // check for specific times to update the commands
                if ((int)TimeToAlarm.TotalSeconds == 300 || // 5 min
                    (int)TimeToAlarm.TotalSeconds == 60) // 1 min
                    UpdateCommands();
            }
            else
            {
                Progress = 0;

                TryStopAlarmSound();
            }

            NotifyPropertyChanged("TimeToAlarm");
        }

        /// <summary>
        /// Plays the alarm sound if sound instance has been loaded.
        /// </summary>
        private void TryPlayAlarmSound()
        {
            if (_alarmSound == null || _alarmSound.IsDisposed)
                return;
            try
            {
                if (_alarmSound.State != SoundState.Playing)
                {
                    _alarmSound.Play();
                }
            }
            catch (Exception) { }
        }

        /// <summary>
        /// Stops the alarm sound if sound instance has been loaded.
        /// </summary>
        private void TryStopAlarmSound()
        {
            if (_alarmSound == null || _alarmSound.IsDisposed)
                return;

            if (_alarmSound.State == SoundState.Playing)
            {
                _alarmSound.Stop();
            }

            // delete sound to there will be a new instance created for the next run
            // to ensure the current alarm sound is the setting is used
            _alarmSound.Dispose();
            _alarmSound = null;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the singleton AlarmClock instance.
        /// </summary>
        public static AlarmClockViewModel Instance
        {
            get
            {
                if (instance == null)
                    instance = new AlarmClockViewModel();
                return instance;
            }
        }

        /// <summary>
        /// Gets wheter the alarm is active.
        /// </summary>
        public bool IsAlarmSet
        {
            get
            {
                return AlarmTime != DateTime.MinValue;
            }
        }

        /// <summary>
        /// Sets or gets the alarm time.
        /// </summary>
        public TimeSpan TimeToAlarm
        {
            get
            {
                var timeToAlarm = AlarmTime - DateTime.Now;
                return (timeToAlarm < TimeSpan.Zero) ? TimeSpan.Zero : timeToAlarm;
            }
        }

        /// <summary>
        /// Gets or sets whether the alarm is ringing.
        /// </summary>
        public bool IsAlarmRinging
        {
            get
            {
                return _alarmIsRinging;
            }
            private set
            {
                if (_alarmIsRinging != value)
                {
                    _alarmIsRinging = value;
                    NotifyPropertyChanged("IsAlarmRinging");
                    NotifyPropertyChanged("IsAlarmNotRinging");
                }
            }
        }

        /// <summary>
        /// Gets or sets whether the alarm is not ringing.
        /// </summary>
        public bool IsAlarmNotRinging
        {
            get
            {
                return !_alarmIsRinging;
            }
        }

        public string TotalNapTime
        {
            get
            {
                return ((int)(AlarmTime - AlarmSetTime).TotalMinutes).ToString();
            }
        }

        /// <summary>
        /// Gets or sets the alarm time.
        /// </summary>
        public DateTime AlarmTime
        {
            get
            {
                return _alarmTime.Value;
            }
            private set
            {
                if (_alarmTime.Value != value)
                {
                    _alarmTime.Value = value;
                    NotifyPropertyChanged("AlarmTime");
                    NotifyPropertyChanged("TimeToAlarm");
                    NotifyPropertyChanged("TotalNapTime");
                }
            }
        }

        /// <summary>
        /// Gets or sets the alarm preview time.
        /// </summary>
        public DateTime AlarmPreviewTime
        {
            get
            {
                return _alarmPreviewTime;
            }
            set
            {
                if (_alarmPreviewTime != value)
                {
                    _alarmPreviewTime = value;
                    NotifyPropertyChanged("AlarmPreviewTime");
                }
            }
        }

        /// <summary>
        /// Gets or sets the alarm set time.
        /// </summary>
        public DateTime AlarmSetTime
        {
            get
            {
                return _alarmSetTime.Value;
            }
            private set
            {
                if (_alarmSetTime.Value != value)
                {
                    _alarmSetTime.Value = value;
                    NotifyPropertyChanged("AlarmSetTime");
                }
            }
        }

        /// <summary>
        /// Gets the last alarm duration.
        /// </summary>
        public TimeSpan LastAlarmDuration
        {
            get
            {
                return _lastAlarmDuration.Value;
            }
        }

        /// <summary>
        /// Gets the progress of the total power nap.
        /// </summary>
        public int Progress
        {
            get
            {
                return _progress;
            }
            private set
            {
                if (_progress != value)
                {
                    _progress = Math.Min(Math.Max(value, 0), 100);
                    NotifyPropertyChanged("Progress");
                }
            }
        }

        /// <summary>
        /// Gets the start alarm command.
        /// </summary>
        public ICommand StartCommand
        {
            get
            {
                return _startCommand;
            }
        }

        /// <summary>
        /// Gets the snooze alarm command.
        /// </summary>
        public ICommand SnoozeCommand
        {
            get
            {
                return _snoozeCommand;
            }
        }

        /// <summary>
        /// Gets the anti snooze alarm command.
        /// </summary>
        public ICommand AntiSnoozeCommand
        {
            get
            {
                return _antiSnoozeCommand;
            }
        }

        /// <summary>
        /// Gets the stop alarm command.
        /// </summary>
        public ICommand StopCommand
        {
            get
            {
                return _stopCommand;
            }
        }

        #endregion
    }
}
