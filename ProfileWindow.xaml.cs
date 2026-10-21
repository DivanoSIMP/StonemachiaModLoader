using StonemachiaModLoader.Core;
using System.IO;
using System.Windows;

namespace StonemachiaModLoader
{
    /// <summary>
    /// Interaction logic for ProfileWindow.xaml
    /// </summary>
    public partial class ProfileWindow : Window
    {
        private ProfilesPage profilesPage;
        private ProfileManager profileManager;

        public ProfileWindow(ProfileManager profileManager, ProfilesPage profilesPage)
        {
            InitializeComponent();
            this.profileManager = profileManager;
            this.profilesPage = profilesPage;
        }

        private void SaveProfileButton_Click(object sender, RoutedEventArgs e)
        {
            string profileName = ProfileNameTextBox.Text.Trim();

            if (string.IsNullOrEmpty(profileName) || Path.GetInvalidFileNameChars().Any(_ => profileName.Contains(_)))
            {
                MessageBox.Show("Please enter a valid profile name.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                profileManager.AddProfile(profileName);
                profilesPage.RefreshProfiles();
                Close();
            }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentException or IOException or UnauthorizedAccessException)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
