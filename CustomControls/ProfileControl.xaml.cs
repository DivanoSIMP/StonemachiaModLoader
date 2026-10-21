using StonemachiaModLoader.Core;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace StonemachiaModLoader.CustomControls
{
    /// <summary>
    /// Interaction logic for ProfileControl.xaml
    /// </summary>
    public partial class ProfileControl : UserControl
    {
        private Profile? _profile;
        private ProfileManager? profileManager;
        private ProfilesPage? profilesPage;

        private SolidColorBrush borderColorBase => (SolidColorBrush)FindResource("Border");
        private SolidColorBrush borderColorHover => (SolidColorBrush)FindResource("Border_Light");

        public ProfileControl(ProfileManager profileManager, ProfilesPage profilesPage, Profile _profile, bool _enabled)
        {
            InitializeComponent();
            this.profileManager = profileManager;
            this._profile = _profile;
            this.profilesPage = profilesPage;

            ProfileSelector.Visibility = _enabled ? Visibility.Visible : Visibility.Hidden;
        }

        public ProfileControl()
        {
            InitializeComponent();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (profileManager is null || _profile is null)
                return;

            MessageBoxResult result = MessageBox.Show(
                $"Delete profile '{_profile.Name}' and all files inside it?",
                "Delete profile",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                profileManager.RemoveProfile(_profile);
                profilesPage?.RefreshProfiles();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not delete profile.\n\n{ex.Message}",
                    "Delete failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ProfileRect_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            profileManager?.SelectProfile(_profile!);
            profilesPage?.RefreshProfiles();
        }

        private void ProfileRect_MouseEnter(object sender, MouseEventArgs e)
        {
            ProfileBorder.Background = borderColorHover;
        }

        private void ProfileRect_MouseLeave(object sender, MouseEventArgs e)
        {
            ProfileBorder.Background = borderColorBase;
        }
    }
}
