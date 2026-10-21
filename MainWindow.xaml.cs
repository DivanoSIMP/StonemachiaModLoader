using StonemachiaModLoader.Core;
using System.Windows;
using System.Windows.Controls;

namespace StonemachiaModLoader
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static ProfileManager profileManager = new ProfileManager();
        private static ProfilesPage profilePage = new ProfilesPage(profileManager);

        public MainWindow()
        {
            InitializeComponent();
            MainFrame.Content = profilePage;
            profilePage.RefreshProfiles();
        }

        private void FileTooltipButton_Click(object sender, RoutedEventArgs e)
        {
            ContextMenu ctx = (ContextMenu)WindowGrid.FindResource("FileContextMenu");

            ctx.PlacementTarget = (Control)sender;

            ctx.IsOpen = true;
        }

        private void OpenConfigMenu(object sender, RoutedEventArgs e)
        {
            Window configWindow = new ConfigWindow();
            configWindow.Owner = this;
            configWindow.ShowDialog();
        }

        private void OpenProfileMenu(object sender, RoutedEventArgs e)
        {
            Window profileWindow = new ProfileWindow(profileManager, profilePage);
            profileWindow.Owner = this;
            profileWindow.ShowDialog();
        }
    }
}