using System.Diagnostics;
using System.IO;
using System.Text;

namespace StonemachiaModLoader.Core;

public sealed class ProfileManager
{
    private readonly string _profilesPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "StonemachiaLoader", "profiles");

    private readonly string _selectedProfilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "StonemachiaLoader", "selected-profile.txt");

    private readonly List<Profile> _profiles = [];
    private Profile? _selectedProfile;

    public ProfileManager() => InitializeProfiles();

    public string ProfilesPath => _profilesPath;
    public IReadOnlyList<Profile> LoadProfiles() => _profiles.AsReadOnly();

    public void InitializeProfiles()
    {
        Directory.CreateDirectory(_profilesPath);
        _profiles.Clear();

        foreach (string directory in Directory.EnumerateDirectories(_profilesPath)
                     .OrderBy(directory => Path.GetFileName(directory), StringComparer.OrdinalIgnoreCase))
        {
            string name = Path.GetFileName(directory);
            if (!string.IsNullOrWhiteSpace(name))
                _profiles.Add(new Profile(name));
        }

        string? savedName = null;
        try
        {
            if (File.Exists(_selectedProfilePath))
                savedName = File.ReadAllText(_selectedProfilePath).Trim();
        }
        catch (IOException) { }

        _selectedProfile = _profiles.FirstOrDefault(p =>
            string.Equals(p.Name, savedName, StringComparison.OrdinalIgnoreCase))
            ?? _profiles.FirstOrDefault();
        PersistSelection();
    }

    public void AddProfile(Profile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        string path = GetProfilePath(profile.Name);

        if (Directory.Exists(path))
            throw new InvalidOperationException($"Profile '{profile.Name}' already exists.");

        Directory.CreateDirectory(path);
        _profiles.Add(profile);
        if (_selectedProfile is null)
            SelectProfile(profile);
    }

    public void AddProfile(string profileName) => AddProfile(new Profile(profileName));

    public void RemoveProfile(Profile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        Profile? existing = _profiles.FirstOrDefault(p =>
            string.Equals(p.Name, profile.Name, StringComparison.OrdinalIgnoreCase));
        if (existing is null)
            throw new InvalidOperationException($"Profile '{profile.Name}' does not exist.");

        string path = GetProfilePath(existing.Name);
        if (Directory.Exists(path))
            Directory.Delete(path, recursive: true);

        _profiles.Remove(existing);
        if (ReferenceEquals(_selectedProfile, existing))
            _selectedProfile = _profiles.FirstOrDefault();
        PersistSelection();
    }

    public void SelectProfile(Profile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        if (!_profiles.Any(p => ReferenceEquals(p, profile)))
            throw new InvalidOperationException("The selected profile is not managed by this launcher.");

        _selectedProfile = profile;
        PersistSelection();
    }

    public bool IsProfileSelected(Profile profile) => ReferenceEquals(_selectedProfile, profile);
    public bool HasProfileSelected() => _selectedProfile is not null;
    public Profile? GetSelectedProfile() => _selectedProfile;

    public void OpenProfilesFolder()
    {
        Directory.CreateDirectory(_profilesPath);
        Process.Start(new ProcessStartInfo
        {
            FileName = _profilesPath,
            UseShellExecute = true
        });
    }

    private string GetProfilePath(string name)
    {
        if (string.IsNullOrWhiteSpace(name) ||
            name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
            name is "." or ".." ||
            name.Contains(Path.DirectorySeparatorChar) ||
            name.Contains(Path.AltDirectorySeparatorChar))
            throw new ArgumentException("Invalid profile name.", nameof(name));

        string path = Path.GetFullPath(Path.Combine(_profilesPath, name));
        string prefix = Path.GetFullPath(_profilesPath) + Path.DirectorySeparatorChar;
        if (!path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Invalid profile path.", nameof(name));
        return path;
    }

    private void PersistSelection()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_selectedProfilePath)!);
            if (_selectedProfile is null)
                File.Delete(_selectedProfilePath);
            else
                File.WriteAllText(_selectedProfilePath, _selectedProfile.Name, Encoding.UTF8);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
