using NLog;
using System;
using System.IO;
using System.Text.Json;

namespace QPAS
{
    public static class SettingsUtils
    {
        public static IAppSettings LoadSettings()
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), "QpasSettings.json");
            if (!File.Exists(path))
            {
                var sw = File.CreateText(path);
                var settings = new AppSettings();
                sw.Write(JsonSerializer.Serialize(settings));
                return settings;
            }

            try
            {
                var serialized = File.ReadAllText(path);
                return JsonSerializer.Deserialize<AppSettings>(serialized);
            }
            catch (Exception ex)
            {
                var logger = LogManager.GetCurrentClassLogger();
                logger.Error(ex, "settings deserialization failure");
                return new AppSettings();
            }
        }

        public static void SaveSettings(IAppSettings settings)
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), "QpasSettings.json");
            File.WriteAllText(path, JsonSerializer.Serialize(settings));
        }
    }
}