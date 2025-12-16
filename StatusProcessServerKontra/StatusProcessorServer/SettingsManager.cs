using System;
using System.IO;
using System.Text.Json;

public class AppSettings
{
    public int ProcessingIntervalSeconds { get; set; } = 3000;
    public int MaxErrorRetries { get; set; } = 5;
    public string ConnectionString { get; set; } = "";
}

public class SettingsManager
{
    private readonly string _path;
    private AppSettings _settings = new();
    private readonly FileSystemWatcher _watcher;

    public SettingsManager(string path)
    {
        _path = path;
        _watcher = new(Path.GetDirectoryName(Path.GetFullPath(_path))!, Path.GetFileName(_path))
        {
            NotifyFilter = NotifyFilters.LastWrite
        };
        _watcher.Changed += (_, _) => { System.Threading.Thread.Sleep(100); Load(); };
        _watcher.EnableRaisingEvents = true;
        Load();
        
    }

    public AppSettings Get() => _settings;

    public void Reload() => Load();

    private void Load()
    {
        try
        {
            if (!File.Exists(_path)) return;
            var json = File.ReadAllText(_path);
            _settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new();
            Console.WriteLine("Конфиг загружен");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ошибка конфига{ex.Message}");
        }
    }
}