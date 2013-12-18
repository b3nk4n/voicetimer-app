using PhoneKit.Framework.Core.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoiceTimer
{
    /// <summary>
    /// The application settings.
    /// </summary>
    public static class Settings
    {
        /// <summary>
        /// Setting for whether the vibration is enabled.
        /// </summary>
        public static readonly StoredObject<bool> EnableVibration = new StoredObject<bool>("enableVibration", true);

        /// <summary>
        /// The a
        /// </summary>
        public static readonly StoredObject<string> AlarmUriString = new StoredObject<string>("alarmUri", "Assets/Audio/classic.wav");
    }
}
