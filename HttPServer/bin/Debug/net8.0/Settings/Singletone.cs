using System.Text.Json;
using HttPServer.Settings;

namespace HttPServer.Settings
{
    public class Singletone
    {
        private static Singletone? instance;
        private static readonly object _lock = new();

        public SettingsModel Model { get; private set; }

        public Singletone()
        {
            LoadSettings();
        }

        public static Singletone Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (_lock)
                    {
                        if (instance == null)
                        {
                            instance = new Singletone();
                        }
                    }
                }
                return instance;
            }
        }

        private void LoadSettings()
        {
            try
            {
                string? settings = File.ReadAllText("settings.json");
                Model = JsonSerializer.Deserialize<SettingsModel>(settings) ?? throw new InvalidOperationException("не получилось Десериализовать");
                var properties = Model.GetType().GetProperties();
                foreach ( var property in properties )
                {
                    var value = property.GetValue(Model)?.ToString();
                    if ( value is null || String.IsNullOrEmpty(value))
                        throw new InvalidOperationException($"Поле '{property.Name}' не было заполнено из settings.json");

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error of configuration: {ex.Message}");
            }
        }
    }
}