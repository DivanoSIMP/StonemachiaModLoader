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
    }
}