using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace McpDesktopClient.Services
{
    public class TestPreset
    {
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("tool")]
        public string Tool { get; set; } = string.Empty;

        [JsonProperty("description")]
        public string Description { get; set; } = string.Empty;

        [JsonProperty("arguments")]
        public Dictionary<string, object> Arguments { get; set; } = new Dictionary<string, object>();

        public override string ToString()
        {
            return $"{Name} ({Tool})";
        }
    }

    public class TestPresetCollection
    {
        [JsonProperty("presets")]
        public List<TestPreset> Presets { get; set; } = new List<TestPreset>();
    }

    public class TestPresetManager
    {
        private List<TestPreset> _presets = new List<TestPreset>();
        private readonly string _presetsFilePath;

        public TestPresetManager()
        {
            _presetsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestPresets", "UnityMcpTestPresets.json");
            LoadPresets();
        }

        public List<TestPreset> GetAllPresets()
        {
            return new List<TestPreset>(_presets);
        }

        public TestPreset? GetPresetByName(string name)
        {
            return _presets.Find(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public List<TestPreset> GetPresetsByTool(string toolName)
        {
            return _presets.FindAll(p => p.Tool.Equals(toolName, StringComparison.OrdinalIgnoreCase));
        }

        private void LoadPresets()
        {
            try
            {
                if (File.Exists(_presetsFilePath))
                {
                    var json = File.ReadAllText(_presetsFilePath);
                    var collection = JsonConvert.DeserializeObject<TestPresetCollection>(json);
                    if (collection?.Presets != null)
                    {
                        _presets = collection.Presets;
                    }
                }
                else
                {
                    // 如果文件不存在，创建默认预设
                    CreateDefaultPresets();
                }
            }
            catch (Exception ex)
            {
                // 如果加载失败，创建默认预设
                CreateDefaultPresets();
                System.Diagnostics.Debug.WriteLine($"Failed to load presets: {ex.Message}");
            }
        }

        private void CreateDefaultPresets()
        {
            _presets = new List<TestPreset>
            {
                new TestPreset
                {
                    Name = "获取场景信息",
                    Tool = "get_scene_info",
                    Description = "获取当前Unity场景的基本信息",
                    Arguments = new Dictionary<string, object>()
                },
                new TestPreset
                {
                    Name = "列出场景对象",
                    Tool = "list_scene_objects",
                    Description = "列出当前场景中的所有游戏对象",
                    Arguments = new Dictionary<string, object>
                    {
                        { "include_inactive", true }
                    }
                },
                new TestPreset
                {
                    Name = "创建立方体",
                    Tool = "create_scene_object",
                    Description = "在场景中创建一个立方体",
                    Arguments = new Dictionary<string, object>
                    {
                        { "name", "TestCube" },
                        { "position", new { x = 0, y = 0, z = 0 } },
                        { "rotation", new { x = 0, y = 0, z = 0 } },
                        { "scale", new { x = 1, y = 1, z = 1 } }
                    }
                }
            };
        }

        public void SavePresets()
        {
            try
            {
                var collection = new TestPresetCollection { Presets = _presets };
                var json = JsonConvert.SerializeObject(collection, Formatting.Indented);
                
                var directory = Path.GetDirectoryName(_presetsFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                File.WriteAllText(_presetsFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save presets: {ex.Message}");
            }
        }

        public void AddPreset(TestPreset preset)
        {
            if (preset != null && !string.IsNullOrEmpty(preset.Name))
            {
                // 检查是否已存在同名预设
                var existing = GetPresetByName(preset.Name);
                if (existing != null)
                {
                    // 替换现有预设
                    var index = _presets.IndexOf(existing);
                    _presets[index] = preset;
                }
                else
                {
                    // 添加新预设
                    _presets.Add(preset);
                }
                SavePresets();
            }
        }

        public bool RemovePreset(string name)
        {
            var preset = GetPresetByName(name);
            if (preset != null)
            {
                _presets.Remove(preset);
                SavePresets();
                return true;
            }
            return false;
        }
    }
}