using System.Text.Json;
using ElaraMacro.Models;

namespace ElaraMacro.Services;

public sealed class StorageService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public string AppDir { get; }
    public string SettingsPath => Path.Combine(AppDir, "settings.json");
    public string MacrosPath => Path.Combine(AppDir, "macros.json");

    public StorageService()
    {
        AppDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ElaraMacro");
        Directory.CreateDirectory(AppDir);
    }

    public AppSettings LoadSettings() => LoadJson(SettingsPath, new AppSettings());

    public void SaveSettings(AppSettings settings) => SaveJsonAtomic(SettingsPath, settings ?? new AppSettings());

    public List<Macro> LoadMacros() => LoadJson(MacrosPath, new List<Macro>());

    public void SaveMacros(List<Macro> macros) => SaveJsonAtomic(MacrosPath, macros ?? new List<Macro>());

    private static T LoadJson<T>(string path, T fallback)
    {
        try
        {
            if (!File.Exists(path))
            {
                return fallback;
            }

            using var stream = File.OpenRead(path);
            return JsonSerializer.Deserialize<T>(stream, JsonOptions) ?? fallback;
        }
        catch
        {
            return fallback;
        }
    }

    private static void SaveJsonAtomic<T>(string path, T value)
    {
        try
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var tempPath = path + ".tmp";
            var backupPath = path + ".bak";
            var json = JsonSerializer.Serialize(value, JsonOptions);

            File.WriteAllText(tempPath, json);

            if (File.Exists(path))
            {
                File.Replace(tempPath, path, backupPath, true);
                if (File.Exists(backupPath))
                {
                    File.Delete(backupPath);
                }
            }
            else
            {
                File.Move(tempPath, path);
            }
        }
        catch
        {
            // Persistence is best-effort and must never throw to caller.
        }
    }
}
