using System.Text.Json;
using ElaraMacro.Models;

namespace ElaraMacro.Services;

public sealed class StorageService
{
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };
    public string AppDir { get; }
    public string SettingsPath => Path.Combine(AppDir, "settings.json");
    public string MacrosPath => Path.Combine(AppDir, "macros.json");

    public StorageService()
    {
        AppDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ElaraMacro");
        Directory.CreateDirectory(AppDir);
    }

    public AppSettings LoadSettings() => LoadJson(SettingsPath, new AppSettings());
    public void SaveSettings(AppSettings settings) => SaveJsonAtomic(SettingsPath, settings);
    public List<Macro> LoadMacros() => LoadJson(MacrosPath, new List<Macro>());
    public void SaveMacros(List<Macro> macros) => SaveJsonAtomic(MacrosPath, macros);

    private T LoadJson<T>(string path, T fallback)
    {
        if (!File.Exists(path)) { SaveJsonAtomic(path, fallback); return fallback; }
        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<T>(json, _jsonOptions) ?? fallback;
        }
        catch
        {
            var bak = path + ".bak";
            try { if (File.Exists(bak)) File.Delete(bak); File.Move(path, bak, true); } catch { }
            SaveJsonAtomic(path, fallback);
            return fallback;
        }
    }

    private void SaveJsonAtomic<T>(string path, T value)
    {
        var temp = path + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(value, _jsonOptions));
        if (File.Exists(path)) File.Replace(temp, path, path + ".prev", true);
        else File.Move(temp, path);
    }
}
