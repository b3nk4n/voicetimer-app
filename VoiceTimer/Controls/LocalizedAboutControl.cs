using VoiceTimer.Resources;
using System;
using PhoneKit.Framework.Controls;
using System.Collections.Generic;
using System.Windows.Media;
using System.Windows;

namespace VoiceTimer.Controls
{
    /// <summary>
    /// The localized About control.
    /// </summary>
    public class LocalizedAboutControl : AboutControlBase
    {
        /// <summary>
        /// Localizes the user controls contents and texts.
        /// </summary>
        protected override void LocalizeContent()
        {
            BackgroundTheme.Color = (Color)Application.Current.Resources["ThemeBackgroundMediumColor"];

            ApplicationIconSource = new Uri("/Assets/ApplicationIcon.png", UriKind.Relative);
            ApplicationTitle = AppResources.ApplicationTitle;
            ApplicationVersion = AppResources.ApplicationVersion;
            ApplicationAuthor= AppResources.ApplicationAuthor;
            ApplicationDescription = AppResources.ApplicationDescription;
            SupportAndFeedbackText = AppResources.SupportAndFeedback;
            SupportAndFeedbackEmail = "apps@bsautermeister.de";
            PrivacyInfoText= AppResources.PrivacyInfo;
            PrivacyInfoLink= "http://bsautermeister.de/privacy.php";
            RateAndReviewText = AppResources.RateAndReview;
            MoreAppsText= AppResources.MoreApps;
            MoreAppsSearchTerms = "Benjamin Sautermeister";

            // contributors
            ContributorsListVisibility = System.Windows.Visibility.Visible;
            IList<ContributorModel> contributors = new List<ContributorModel>();
            contributors.Add(new ContributorModel("/Assets/Images/icon.png", "D. Baranovskiy (The Noun Project)"));
            SetContributorsList(contributors);
        }
    }
}
