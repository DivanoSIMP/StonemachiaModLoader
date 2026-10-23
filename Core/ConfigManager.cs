using System.IO;
using System.Text.Json;

namespace StonemachiaModLoader.Core;

internal sealed class ConfigManager
{
    public string GameExe { get; set; } = string.Empty;
    public string InjectorExe { get; set; } = string.Empty;
    public string Ue4ssDll { get; set; } = string.Empty;

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private static readonly string ConfigDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "StonemachiaModLoader");

    private static readonly string ConfigPath = Path.Combine(ConfigDirectory, "config.json");

    public static ConfigManager Load()
    {
        try
        {
            if (!File.Exists(ConfigPath))
                return new ConfigManager();

            return JsonSerializer.Deserialize<ConfigManager>(File.ReadAllText(ConfigPath))
                   ?? new ConfigManager();
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            Logger.Error($"Failed to read file at {ConfigPath}", ex);
            // Keep the app usable if configuration is malformed or temporarily inaccessible.
            return new ConfigManager();
        }
    }

    public void Save()
    {
        Directory.CreateDirectory(ConfigDirectory);
        string temporaryPath = ConfigPath + ".tmp";
        string json = JsonSerializer.Serialize(this, JsonOptions);

        File.WriteAllText(temporaryPath, json);
        File.Move(temporaryPath, ConfigPath, overwrite: true);
    }
}
