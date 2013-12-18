using Microsoft.Xna.Framework.Audio;
using PhoneKit.Framework.Audio;
using PhoneKit.Framework.Core.MVVM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Resources;

namespace VoiceTimer.ViewModels
{
    /// <summary>
    /// Represents an selectable audio file in the list picker.
    /// </summary>
    class AudioViewModel : ViewModelBase
    {
        #region Members

        /// <summary>
        /// The audio title name.
        /// </summary>
        private string _title;

        /// <summary>
        /// The alarm audio URI as a sting.
        /// </summary>
        private string _uriString;

        /// <summary>
        /// The image URI.
        /// </summary>
        private Uri _imageUri;

        /// <summary>
        /// Command for playing the audio.
        /// </summary>
        private ICommand _playCommand;

        #endregion

        /// <summary>
        /// Creates an AudioViewModel instance.
        /// </summary>
        /// <param name="title">The audio title.</param>
        /// <param name="uriString">The audio uri path as a string.</param>
        /// <param name="imageUri">The image uri.</param>
        public AudioViewModel(string title, string uriString, Uri imageUri)
        {
            _title = title;
            _uriString = uriString;
            _imageUri = imageUri;

            // load audio
            StreamResourceInfo alarmResource = App.GetResourceStream(new Uri(_uriString, UriKind.Relative));
            SoundEffects.Instance.Load(_uriString, alarmResource);

            // commands
            _playCommand = new DelegateCommand(
                () =>
                {
                    SoundEffects.Instance[_uriString].Play();
                });
        }

        /// <summary>
        /// Gets the title.
        /// </summary>
        public string Title
        {
            get
            {
                return _title;
            } 
        }

        /// <summary>
        /// Gets the audio URI as a string.
        /// </summary>
        public string UriString
        {
            get
            {
                return _uriString;
            }
        }

        /// <summary>
        /// Gets the image URI.
        /// </summary>
        public Uri ImageUri
        {
            get
            {
                return _imageUri;
            }
        }

        /// <summary>
        /// Gets the play audio command.
        /// </summary>
        public ICommand PlayCommand
        {
            get
            {
                return _playCommand;
            }
        }
    }
}
