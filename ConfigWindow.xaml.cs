using Microsoft.Win32;
using StonemachiaModLoader.Core;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using static System.Net.WebRequestMethods;

namespace StonemachiaModLoader;

public partial class ConfigWindow : Window
{
    private readonly ConfigManager _config;
    private bool _isChecking;
    private bool _isInitializing = true;

    private static readonly Brush NeutralBrush = new SolidColorBrush(Color.FromRgb(0xB5, 0xA3, 0x99));
    private static readonly Brush SuccessBrush = new SolidColorBrush(Color.FromRgb(0x86, 0xC9, 0x9A));
    private static readonly Brush WarningBrush = new SolidColorBrush(Color.FromRgb(0xE9, 0xB4, 0x4C));
    private static readonly Brush ErrorBrush = new SolidColorBrush(Color.FromRgb(0xC9, 0x4C, 0x4C));

    private const string DefaultGamePath = "C:\\Program Files (x86)\\Steam\\steamapps\\common";
    private static readonly string DefaultInjectorPath = Path.Combine(AppContext.BaseDirectory, "Dependencies\\Injector\\Injector.exe");
    private static readonly string DefaultDllPath = Path.Combine(AppContext.BaseDirectory, "Dependencies\\UE4SS\\UE4SS.dll");

    public ConfigWindow()
    {
        InitializeComponent();
        _config = ConfigManager.Load();
        GameExeText.Text = _config.GameExe;
        InjectorExeText.Text = _config.InjectorExe;
        Ue4ssDllText.Text = _config.Ue4ssDll;
        _isInitializing = false;
    }

    private static void BrowseFile(TextBox text, string filter, string currentPath)
    {
        string? initialDirectory = null;
        if (!string.IsNullOrWhiteSpace(currentPath))
        {
            string? candidate = Path.GetDirectoryName(currentPath);
            if (!string.IsNullOrWhiteSpace(candidate) && Directory.Exists(candidate))
                initialDirectory = candidate;
        }

        var dialog = new OpenFileDialog
        {
            Filter = filter,
            CheckFileExists = true,
            Multiselect = false,
            Title = "Select File",
            InitialDirectory = initialDirectory ?? Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)
        };

        if (dialog.ShowDialog() == true)
            text.Text = dialog.FileName;
    }

    private void BrowseGame_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_config.GameExe)) GameExeText.Text = DefaultGamePath;
        BrowseFile(GameExeText, "Game executable (*.exe)|*.exe", GameExeText.Text);
    }

    private void BrowseInjector_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_config.InjectorExe)) InjectorExeText.Text = DefaultInjectorPath;
        BrowseFile(InjectorExeText, "Injector executable (*.exe)|*.exe", InjectorExeText.Text);
    }

    private void BrowseUe4ss_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_config.Ue4ssDll)) Ue4ssDllText.Text = DefaultDllPath;
        BrowseFile(Ue4ssDllText, "Dynamic-link library (*.dll)|*.dll", Ue4ssDllText.Text);
    }

    private void ConfigurationPath_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isInitializing || GameStatus is null)
            return;

        SetStatus(GameStatus, GameDetails, "Not checked", "Path changed. Run Check Installation again.", StatusKind.Neutral);
        SetStatus(InjectorStatus, InjectorDetails, "Not checked", "Path changed. Run Check Installation again.", StatusKind.Neutral);
        SetStatus(DllStatus, DllDetails, "Not checked", "Path changed. Run Check Installation again.", StatusKind.Neutral);
        CheckSummary.Text = "Configuration changed — run Check Installation before saving.";
        CheckSummary.Foreground = NeutralBrush;
    }

    private async void CheckInstallation_Click(object sender, RoutedEventArgs e)
    {
        if (_isChecking) return;
        _isChecking = true;
        CheckInstallationButton.IsEnabled = false;
        CheckInstallationButton.Content = "Checking…";
        CheckSummary.Text = "Checking selected files and injector command-line options…";
        CheckSummary.Foreground = NeutralBrush;

        try
        {
            await Task.Yield();
            ValidationReport report = FileValidation.Validate(GameExeText.Text.Trim(), InjectorExeText.Text.Trim(), Ue4ssDllText.Text.Trim());

            ApplyComponentResult("Game executable", report, GameStatus, GameDetails,
                "Game executable", "No common Unreal Engine markers were found");
            ApplyComponentResult("UE4SS library", report, DllStatus, DllDetails,
                "UE4SS DLL", "The DLL filename is not UE4SS.dll");

            bool injectorHasFileError = report.Errors.Any(x => x.StartsWith("Injector", StringComparison.OrdinalIgnoreCase));
            string injectorWarning = report.Warnings.FirstOrDefault(x => x.Contains("injector filename", StringComparison.OrdinalIgnoreCase)) ?? string.Empty;
            if (injectorHasFileError)
            {
                string details = string.Join(" ", report.Errors.Where(x => x.StartsWith("Injector", StringComparison.OrdinalIgnoreCase)));
                Logger.Error(details);
                SetStatus(InjectorStatus, InjectorDetails, "Invalid", details, StatusKind.Error);
            }
            else
            {
                try
                {
                    await FileValidation.ValidateNefariusInjectorAsync(Path.GetFullPath(InjectorExeText.Text.Trim()));
                    if (!string.IsNullOrEmpty(injectorWarning))
                    {
                        Logger.Warning("Suspicious validation from Injector.");
                        Logger.Warning(injectorWarning);
                        SetStatus(InjectorStatus, InjectorDetails, "Warning", injectorWarning + " The command-line options were detected.", StatusKind.Warning);
                    }
                    else
                        SetStatus(InjectorStatus, InjectorDetails, "Valid", "Executable is readable and expected --process-id / --inject options were detected.", StatusKind.Success);
                }
                catch (Exception ex)
                {
                    Logger.Error("Injector failed validation.", ex);
                    SetStatus(InjectorStatus, InjectorDetails, "Invalid", ex.Message, StatusKind.Error);
                }
            }

            int errorCount = report.Errors.Count;
            int warningCount = report.Warnings.Count;
            bool injectorFailed = string.Equals(InjectorStatus.Text, "Invalid", StringComparison.OrdinalIgnoreCase);
            if (errorCount == 0 && warningCount == 0 && !injectorFailed)
            {
                CheckSummary.Text = "All checks passed. You can save this configuration.";
                CheckSummary.Foreground = SuccessBrush;
            }
            else if (errorCount > 0 || injectorFailed)
            {
                CheckSummary.Text = $"Check finished: {errorCount + (injectorFailed && !report.Errors.Any(x => x.StartsWith("Injector", StringComparison.OrdinalIgnoreCase)) ? 1 : 0)} blocking issue(s), {warningCount} warning(s). Fix invalid items before saving.";
                CheckSummary.Foreground = ErrorBrush;
            }
            else
            {
                CheckSummary.Text = $"Check finished with {warningCount} warning(s). Review the details before saving.";
                CheckSummary.Foreground = WarningBrush;
            }
        }
        catch (Exception ex)
        {
            Logger.Error("The installation check could not complete", ex);
            CheckSummary.Text = "The installation check could not complete: " + ex.Message;
            CheckSummary.Foreground = ErrorBrush;
        }
        finally
        {
            _isChecking = false;
            CheckInstallationButton.IsEnabled = true;
            CheckInstallationButton.Content = "Check Installation";
        }
    }

    private static void ApplyComponentResult(string component, ValidationReport report, TextBlock status,
        TextBlock details, string errorPrefix, string warningMatch)
    {
        string[] errors = report.Errors.Where(x => x.StartsWith(errorPrefix, StringComparison.OrdinalIgnoreCase)).ToArray();
        string[] warnings = report.Warnings.Where(x => x.Contains(warningMatch, StringComparison.OrdinalIgnoreCase)).ToArray();

        if (errors.Length > 0)
            SetStatus(status, details, "Invalid", string.Join(" ", errors), StatusKind.Error);
        else if (warnings.Length > 0)
            SetStatus(status, details, "Warning", string.Join(" ", warnings), StatusKind.Warning);
        else if (report.Errors.Any(x => x.Contains("architecture", StringComparison.OrdinalIgnoreCase) || x.Contains("architectures", StringComparison.OrdinalIgnoreCase)))
            SetStatus(status, details, "Invalid", "A component architecture check failed. Review the validation errors.", StatusKind.Error);
        else
            SetStatus(status, details, "Valid", component + " passed basic file validation.", StatusKind.Success);
    }

    private static void SetStatus(TextBlock status, TextBlock details, string value, string message, StatusKind kind)
    {
        status.Text = value;
        status.Foreground = kind switch
        {
            StatusKind.Success => SuccessBrush,
            StatusKind.Warning => WarningBrush,
            StatusKind.Error => ErrorBrush,
            _ => NeutralBrush
        };
        details.Text = message;
        details.Foreground = kind == StatusKind.Neutral ? NeutralBrush : status.Foreground;
    }

    private enum StatusKind { Neutral, Success, Warning, Error }

    private async void SaveConfig_Click(object sender, RoutedEventArgs e)
    {
        string game = GameExeText.Text.Trim();
        string injector = InjectorExeText.Text.Trim();
        string dll = Ue4ssDllText.Text.Trim();

        ValidationReport report = FileValidation.Validate(game, injector, dll);
        if (!report.IsValid)
        {
            Logger.Error("Configuration validation failed.");
            MessageBox.Show(string.Join(Environment.NewLine + Environment.NewLine, report.Errors),
                "Configuration validation failed", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        if (report.Warnings.Count > 0)
        {
            MessageBoxResult answer = MessageBox.Show(
                "The following checks need your attention:" + Environment.NewLine + Environment.NewLine +
                string.Join(Environment.NewLine + Environment.NewLine, report.Warnings) +
                Environment.NewLine + Environment.NewLine + "Do you want to continue anyway?",
                "Configuration warnings", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (answer != MessageBoxResult.Yes)
                return;
        }

        try
        {
            await FileValidation.ValidateNefariusInjectorAsync(Path.GetFullPath(injector));
            _config.GameExe = Path.GetFullPath(game);
            _config.InjectorExe = Path.GetFullPath(injector);
            _config.Ue4ssDll = Path.GetFullPath(dll);
            _config.Save();
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            Logger.Error("Injector validation failed.", ex);
            MessageBox.Show($"The configuration was not saved.\n\n{ex.Message}",
                "Injector validation failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CancelConfig_Click(object sender, RoutedEventArgs e) => Close();
}
