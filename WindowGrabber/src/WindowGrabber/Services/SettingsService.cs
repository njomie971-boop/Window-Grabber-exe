using System.IO;
using System.Text.Json;
using WindowGrabber.Helpers;
using WindowGrabber.Models;

namespace WindowGrabber.Services;

/// <summary>
/// Persistance JSON des préférences utilisateur dans <c>%APPDATA%\WindowGrabber\settings.json</c>.
/// Tolérant à tout souci de fichier : à défaut, on retourne des valeurs par défaut.
/// </summary>
public sealed class SettingsService
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "WindowGrabber",
        "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath))
                return new AppSettings();

            var json = File.ReadAllText(SettingsPath);
            if (string.IsNullOrWhiteSpace(json))
                return new AppSettings();

            return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
        }
        catch (Exception ex)
        {
            Logger.Warn("Impossible de charger settings.json", ex);
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        try
        {
            var dir = Path.GetDirectoryName(SettingsPath)!;
            Directory.CreateDirectory(dir);
            var json = JsonSerializer.Serialize(settings, JsonOptions);
            File.WriteAllText(SettingsPath, json);
        }
        catch (Exception ex)
        {
            Logger.Warn("Impossible d'écrire settings.json", ex);
        }
    }
}
