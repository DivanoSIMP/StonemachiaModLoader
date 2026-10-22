using StonemachiaModLoader.Core;
using StonemachiaModLoader.CustomControls;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace StonemachiaModLoader;

public partial class ProfilesPage : Page
{
    private readonly ProfileManager _profileManager;
    private bool _isLaunching;

    public ProfilesPage(ProfileManager profileManager)
    {
        InitializeComponent();
        _profileManager = profileManager;
    }

    public void RefreshProfiles()
    {
        ProfilesStackPanel.Children.Clear();
        ProfilesStackPanel.Children.Add(WARNING);
        var profiles = _profileManager.LoadProfiles();

        if (!_profileManager.HasProfileSelected() && profiles.Count > 0)
            _profileManager.SelectProfile(profiles[0]);

        foreach (Profile profile in profiles)
        {
            var control = new ProfileControl(
                _profileManager, this, profile,
                _profileManager.IsProfileSelected(profile));
            control.ProfileName.Text = profile.Name;
            ProfilesStackPanel.Children.Add(control);
        }
    }

    private void ProfileCreationButton_Click(object sender, RoutedEventArgs e)
    {
        var window = new ProfileWindow(_profileManager, this) { Owner = Window.GetWindow(this) };
        window.ShowDialog();
    }

    private void OpenFolderButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _profileManager.OpenLauncherFolder();
        }
        catch (Exception ex)
        {
            ShowError("Could not open launcher folder.", ex);
        }
    }

    private void OpenConfigsButton_Click(object sender, RoutedEventArgs e)
    {
        var window = new ConfigWindow { Owner = Window.GetWindow(this) };
        window.ShowDialog();
    }

    private async void LaunchGameButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isLaunching)
            return;

        _isLaunching = true;
        try
        {
            ConfigManager config = ConfigManager.Load();
            if (string.IsNullOrWhiteSpace(config.GameExe) ||
                string.IsNullOrWhiteSpace(config.InjectorExe) ||
                string.IsNullOrWhiteSpace(config.Ue4ssDll))
            {
                MessageBox.Show(
                    "Configure the game executable, injector and UE4SS DLL before launching.",
                    "Setup required", MessageBoxButton.OK, MessageBoxImage.Information);
                var configWindow = new ConfigWindow { Owner = Window.GetWindow(this) };
                configWindow.ShowDialog();
                return;
            }

            await Launcher.LaunchModdedAsync(config.GameExe, config.InjectorExe, config.Ue4ssDll);
        }
        catch (OperationCanceledException ex)
        {
            // The user or application cancelled the operation.
            ShowError("The operation was cancelled.", ex);
        }
        catch (Exception ex)
        {
            ShowError("Could not launch Stonemachia with UE4SS.", ex);
        }
        finally
        {
            _isLaunching = false;
        }
    }

    private static void ShowError(string message, Exception exception)
    {
        Logger.Error($"Error: {message}", exception);
        MessageBox.Show($"{message}\n\n{exception.Message}", "Stonemachia Mod Loader",
            MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
