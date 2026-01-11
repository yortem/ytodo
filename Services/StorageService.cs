using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using yTodo.Models;

namespace yTodo.Services
{
    public class StorageService
    {
        private readonly string _filePath;
        private static readonly JsonSerializerOptions _options = new JsonSerializerOptions { WriteIndented = true };

        public StorageService()
        {
            var localFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "yTodo");
            Directory.CreateDirectory(localFolder);
            _filePath = Path.Combine(localFolder, "tasks.json");
        }

        public async Task<AppData> LoadAsync()
        {
            if (!File.Exists(_filePath))
            {
                return new AppData();
            }

            try
            {
                string json = await File.ReadAllTextAsync(_filePath);
                return JsonSerializer.Deserialize<AppData>(json, _options) ?? new AppData();
            }
            catch
            {
                return new AppData();
            }
        }

        public async Task SaveAsync(AppData data)
        {
            try
            {
                string json = JsonSerializer.Serialize(data, _options);
                await File.WriteAllTextAsync(_filePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save tasks: {ex.Message}");
            }
        }

        public void Save(AppData data)
        {
            try
            {
                string json = JsonSerializer.Serialize(data, _options);
                File.WriteAllText(_filePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save tasks: {ex.Message}");
            }
        }

        public string GetStoragePath() => _filePath;
    }
}
