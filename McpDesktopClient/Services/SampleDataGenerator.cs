using System;
using System.Collections.Generic;
using System.Linq;
using McpDesktopClient.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace McpDesktopClient.Services
{
    public class ToolDependencyInfo
    {
        public List<string> RequiredInterfaces { get; set; } = new List<string>();
        public List<string> RequiredComponents { get; set; } = new List<string>();
        public List<string> RequiredAssets { get; set; } = new List<string>();
        public Dictionary<string, object> ContextRequirements { get; set; } = new Dictionary<string, object>();
        public bool RequiresPlayMode { get; set; }
        public bool RequiresEditMode { get; set; }
        public string Description { get; set; } = string.Empty;
    }
    
    /// <summary>
    /// 样例报文生成器，为不同的MCP工具自动生成样例参数
    /// </summary>
    public class SampleDataGenerator
    {
        private readonly Dictionary<string, Func<McpTool, Dictionary<string, object>>> _toolSampleGenerators;
        private readonly Dictionary<string, ToolDependencyInfo> _toolDependencies;

        public SampleDataGenerator()
        {
            _toolSampleGenerators = new Dictionary<string, Func<McpTool, Dictionary<string, object>>>(StringComparer.OrdinalIgnoreCase);
            _toolDependencies = new Dictionary<string, ToolDependencyInfo>();
            InitializeToolDependencies();
            InitializeSampleGenerators();
        }
        
        private void InitializeSampleGenerators()
        {
            _toolSampleGenerators.Clear();
            
            // Unity场景相关工具
            _toolSampleGenerators["get_scene_info"] = GenerateSceneInfoSample;
            _toolSampleGenerators["list_scene_objects"] = GenerateListSceneObjectsSample;
            _toolSampleGenerators["create_scene_object"] = GenerateCreateSceneObjectSample;
            _toolSampleGenerators["delete_scene_object"] = GenerateDeleteSceneObjectSample;
            _toolSampleGenerators["modify_scene_object"] = GenerateModifySceneObjectSample;
            _toolSampleGenerators["get_object_info"] = GenerateGetObjectInfoSample;
            _toolSampleGenerators["set_object_transform"] = GenerateSetObjectTransformSample;
            _toolSampleGenerators["add_component"] = GenerateAddComponentSample;
            _toolSampleGenerators["remove_component"] = GenerateRemoveComponentSample;
            _toolSampleGenerators["set_component_property"] = GenerateSetComponentPropertySample;
            
            // 文件操作工具
            _toolSampleGenerators["read_file"] = GenerateReadFileSample;
            _toolSampleGenerators["write_file"] = GenerateWriteFileSample;
            _toolSampleGenerators["list_files"] = GenerateListFilesSample;
            _toolSampleGenerators["create_directory"] = GenerateCreateDirectorySample;
            
            // 搜索工具
            _toolSampleGenerators["search_codebase"] = GenerateSearchCodebaseSample;
            _toolSampleGenerators["search_by_regex"] = GenerateSearchByRegexSample;
            
            // 场景信息工具
            _toolSampleGenerators["get_current_scene_info"] = GenerateGetCurrentSceneInfoSample;
            _toolSampleGenerators["get_play_mode_status"] = tool => new Dictionary<string, object>();
            
            // 通用工具
            _toolSampleGenerators["run_command"] = GenerateRunCommandSample;
            _toolSampleGenerators["execute_script"] = GenerateExecuteScriptSample;
        }
        
        private void InitializeToolDependencies()
        {
            // Unity场景相关工具的依赖项
            _toolDependencies["list_scenes"] = new ToolDependencyInfo
            {
                RequiredInterfaces = new List<string> { "UnityEditor.EditorBuildSettings", "UnityEngine.SceneManagement.SceneManager" },
                RequiresEditMode = true,
                Description = "需要Unity编辑器模式和场景管理接口"
            };
            
            _toolDependencies["open_scene"] = new ToolDependencyInfo
            {
                RequiredInterfaces = new List<string> { "UnityEditor.SceneManagement.EditorSceneManager" },
                RequiresEditMode = true,
                Description = "需要Unity编辑器场景管理接口"
            };
            
            _toolDependencies["get_play_mode_status"] = new ToolDependencyInfo
            {
                RequiredInterfaces = new List<string> { "UnityEditor.EditorApplication" },
                RequiresEditMode = true,
                Description = "需要Unity编辑器应用程序接口"
            };
            
            _toolDependencies["get_current_scene_info"] = new ToolDependencyInfo
            {
                RequiredInterfaces = new List<string> { "UnityEngine.SceneManagement.SceneManager", "UnityEditor.SceneManagement.EditorSceneManager" },
                RequiresEditMode = true,
                Description = "获取当前活动场景的详细信息，包括场景中的GameObject和组件"
            };
            
            // 游戏对象相关工具的依赖项
            _toolDependencies["create_cube"] = new ToolDependencyInfo
            {
                RequiredInterfaces = new List<string> { "UnityEngine.GameObject", "UnityEngine.PrimitiveType" },
                RequiredComponents = new List<string> { "Transform", "MeshRenderer", "MeshFilter" },
                Description = "需要Unity游戏对象和基础渲染组件"
            };
            
            _toolDependencies["add_component"] = new ToolDependencyInfo
            {
                RequiredInterfaces = new List<string> { "UnityEngine.GameObject", "System.Type" },
                ContextRequirements = new Dictionary<string, object> { ["gameObject"] = "existing" },
                Description = "需要现有游戏对象和组件类型系统"
            };
            
            _toolDependencies["set_transform"] = new ToolDependencyInfo
            {
                RequiredInterfaces = new List<string> { "UnityEngine.Transform" },
                RequiredComponents = new List<string> { "Transform" },
                ContextRequirements = new Dictionary<string, object> { ["gameObject"] = "existing" },
                Description = "需要现有游戏对象的Transform组件"
            };
            
            // 资源相关工具的依赖项
            _toolDependencies["import_asset"] = new ToolDependencyInfo
            {
                RequiredInterfaces = new List<string> { "UnityEditor.AssetDatabase" },
                RequiresEditMode = true,
                Description = "需要Unity编辑器资源数据库接口"
            };
        }
        
        /// <summary>
        /// 检查工具的依赖项并生成相应的样例参数
        /// </summary>
        public Dictionary<string, object> GenerateArgumentsWithDependencyCheck(McpTool tool)
        {
            if (tool == null || string.IsNullOrEmpty(tool.Name))
            {
                return GenerateDefaultSample();
            }
            
            var sample = GenerateSampleArguments(tool);
            
            if (_toolDependencies.TryGetValue(tool.Name, out var dependency))
            {
                // 根据依赖项信息增强样例数据
                sample = EnhanceSampleWithDependencyInfo(sample, dependency, tool);
            }
            
            return sample;
        }
        
        private Dictionary<string, object> EnhanceSampleWithDependencyInfo(Dictionary<string, object> sample, ToolDependencyInfo dependency, McpTool tool)
        {
            if (sample == null || dependency == null)
            {
                return sample ?? new Dictionary<string, object>();
            }
            
            var enhancedSample = new Dictionary<string, object>(sample);
            
            // 根据上下文要求调整样例数据
            if (dependency.ContextRequirements != null)
            {
                foreach (var requirement in dependency.ContextRequirements)
                {
                    if (requirement.Key == "gameObject" && requirement.Value?.ToString() == "existing")
                    {
                        // 如果需要现有游戏对象，提供更真实的名称
                        if (enhancedSample.ContainsKey("gameObject"))
                        {
                            enhancedSample["gameObject"] = "Main Camera"; // 场景中通常存在的对象
                        }
                        if (enhancedSample.ContainsKey("name"))
                        {
                            enhancedSample["name"] = "Main Camera";
                        }
                    }
                }
            }
            
            // 根据所需组件调整参数
            if (dependency.RequiredComponents != null && dependency.RequiredComponents.Any() && enhancedSample.ContainsKey("component"))
            {
                enhancedSample["component"] = dependency.RequiredComponents.First();
            }
            
            // 添加依赖项信息到样例中（作为注释）
            enhancedSample["_dependency_info"] = new
            {
                description = dependency.Description ?? string.Empty,
                requiredInterfaces = dependency.RequiredInterfaces ?? new List<string>(),
                requiredComponents = dependency.RequiredComponents ?? new List<string>(),
                requiresEditMode = dependency.RequiresEditMode,
                requiresPlayMode = dependency.RequiresPlayMode
            };
            
            return enhancedSample;
        }
        
        /// <summary>
        /// 为指定工具生成样例参数
        /// </summary>
        /// <param name="tool">MCP工具</param>
        /// <returns>样例参数字典</returns>
        public Dictionary<string, object> GenerateSampleArguments(McpTool tool)
        {
            if (tool == null || string.IsNullOrEmpty(tool.Name))
            {
                return GenerateDefaultSample();
            }

            // 首先尝试使用专门的生成器
            if (_toolSampleGenerators.TryGetValue(tool.Name, out var generator))
            {
                try
                {
                    return generator(tool);
                }
                catch
                {
                    // 如果专门生成器失败，回退到基于Schema的生成
                }
            }

            // 基于工具的InputSchema生成样例
            return GenerateFromSchema(tool);
        }

        private Dictionary<string, object> GenerateFromSchema(McpTool tool)
        {
            if (tool.InputSchema == null)
            {
                return GenerateDefaultSample();
            }

            var sample = new Dictionary<string, object>();
            
            try
            {
                var schemaJson = JsonConvert.SerializeObject(tool.InputSchema);
                var schemaObj = JsonConvert.DeserializeObject<dynamic>(schemaJson);
                
                if (schemaObj?.properties != null)
                {
                    foreach (var prop in schemaObj.properties)
                    {
                        var propName = prop.Name;
                        var propValue = prop.Value;
                        
                        sample[propName] = GenerateValueFromProperty(propValue, propName);
                    }
                }
            }
            catch
            {
                return GenerateDefaultSample();
            }

            return sample.Count > 0 ? sample : GenerateDefaultSample();
        }

        private object GenerateValueFromProperty(dynamic property, string propertyName)
        {
            var type = property?.type?.ToString();
            var description = property?.description?.ToString();
            
            switch (type)
            {
                case "string":
                    return GenerateStringValue(propertyName, description);
                case "number":
                case "integer":
                    return GenerateNumberValue(propertyName, description);
                case "boolean":
                    return GenerateBooleanValue(propertyName, description);
                case "array":
                    return GenerateArrayValue(propertyName, property);
                case "object":
                    return GenerateObjectValue(propertyName, property);
                default:
                    return GenerateStringValue(propertyName, description);
            }
        }

        private string GenerateStringValue(string propertyName, string? description)
        {
            var lowerName = propertyName.ToLowerInvariant();
            
            if (lowerName.Contains("name"))
                return "ExampleObject";
            if (lowerName.Contains("path") || lowerName.Contains("file"))
                return "Assets/Scenes/SampleScene.unity";
            if (lowerName.Contains("tag"))
                return "Player";
            if (lowerName.Contains("layer"))
                return "Default";
            if (lowerName.Contains("command"))
                return "echo Hello World";
            if (lowerName.Contains("query") || lowerName.Contains("search"))
                return "example search term";
            if (lowerName.Contains("type"))
                return "GameObject";
            if (lowerName.Contains("component"))
                return "Transform";
            
            return description ?? "example_value";
        }

        private object GenerateNumberValue(string propertyName, string? description)
        {
            var lowerName = propertyName.ToLowerInvariant();
            
            if (lowerName.Contains("x") || lowerName.Contains("y") || lowerName.Contains("z"))
                return 0.0;
            if (lowerName.Contains("count") || lowerName.Contains("limit"))
                return 10;
            if (lowerName.Contains("size") || lowerName.Contains("scale"))
                return 1.0;
            if (lowerName.Contains("angle") || lowerName.Contains("rotation"))
                return 0.0;
            
            return 0;
        }

        private bool GenerateBooleanValue(string propertyName, string? description)
        {
            var lowerName = propertyName.ToLowerInvariant();
            
            if (lowerName.Contains("include") || lowerName.Contains("enable"))
                return true;
            if (lowerName.Contains("exclude") || lowerName.Contains("disable"))
                return false;
            
            return true;
        }

        private object[] GenerateArrayValue(string propertyName, dynamic property)
        {
            var lowerName = propertyName.ToLowerInvariant();
            
            if (lowerName.Contains("tag"))
                return new object[] { "Player", "Enemy" };
            if (lowerName.Contains("name"))
                return new object[] { "Object1", "Object2" };
            if (lowerName.Contains("component"))
                return new object[] { "Transform", "Renderer" };
            
            return new object[] { "example1", "example2" };
        }

        private object GenerateObjectValue(string propertyName, dynamic property)
        {
            var lowerName = propertyName.ToLowerInvariant();
            
            if (lowerName.Contains("position"))
                return new { x = 0.0, y = 0.0, z = 0.0 };
            if (lowerName.Contains("rotation"))
                return new { x = 0.0, y = 0.0, z = 0.0 };
            if (lowerName.Contains("scale"))
                return new { x = 1.0, y = 1.0, z = 1.0 };
            if (lowerName.Contains("color"))
                return new { r = 1.0, g = 1.0, b = 1.0, a = 1.0 };
            
            return new { example_key = "example_value" };
        }

        private Dictionary<string, object> GenerateDefaultSample()
        {
            return new Dictionary<string, object>
            {
                { "example_param", "example_value" }
            };
        }

        #region 专门的样例生成器

        private Dictionary<string, object> GenerateSceneInfoSample(McpTool tool)
        {
            return new Dictionary<string, object>();
        }

        private Dictionary<string, object> GenerateListSceneObjectsSample(McpTool tool)
        {
            return new Dictionary<string, object>
            {
                { "include_inactive", true },
                { "filter_by_tag", "" },
                { "filter_by_layer", "" }
            };
        }

        private Dictionary<string, object> GenerateCreateSceneObjectSample(McpTool tool)
        {
            return new Dictionary<string, object>
            {
                { "name", "NewGameObject" },
                { "position", new { x = 0.0, y = 0.0, z = 0.0 } },
                { "rotation", new { x = 0.0, y = 0.0, z = 0.0 } },
                { "scale", new { x = 1.0, y = 1.0, z = 1.0 } },
                { "parent_name", "" },
                { "tag", "Untagged" },
                { "layer", "Default" }
            };
        }

        private Dictionary<string, object> GenerateDeleteSceneObjectSample(McpTool tool)
        {
            return new Dictionary<string, object>
            {
                { "object_name", "GameObject" }
            };
        }

        private Dictionary<string, object> GenerateModifySceneObjectSample(McpTool tool)
        {
            return new Dictionary<string, object>
            {
                { "object_name", "GameObject" },
                { "new_name", "ModifiedGameObject" },
                { "position", new { x = 1.0, y = 0.0, z = 0.0 } },
                { "rotation", new { x = 0.0, y = 45.0, z = 0.0 } },
                { "scale", new { x = 1.5, y = 1.5, z = 1.5 } }
            };
        }

        private Dictionary<string, object> GenerateGetObjectInfoSample(McpTool tool)
        {
            return new Dictionary<string, object>
            {
                { "object_name", "GameObject" },
                { "include_components", true },
                { "include_children", false }
            };
        }

        private Dictionary<string, object> GenerateSetObjectTransformSample(McpTool tool)
        {
            return new Dictionary<string, object>
            {
                { "object_name", "GameObject" },
                { "position", new { x = 0.0, y = 0.0, z = 0.0 } },
                { "rotation", new { x = 0.0, y = 0.0, z = 0.0 } },
                { "scale", new { x = 1.0, y = 1.0, z = 1.0 } }
            };
        }

        private Dictionary<string, object> GenerateAddComponentSample(McpTool tool)
        {
            return new Dictionary<string, object>
            {
                { "object_name", "GameObject" },
                { "component_type", "Rigidbody" },
                { "properties", new { mass = 1.0, useGravity = true } }
            };
        }

        private Dictionary<string, object> GenerateRemoveComponentSample(McpTool tool)
        {
            return new Dictionary<string, object>
            {
                { "object_name", "GameObject" },
                { "component_type", "Rigidbody" }
            };
        }

        private Dictionary<string, object> GenerateSetComponentPropertySample(McpTool tool)
        {
            return new Dictionary<string, object>
            {
                { "object_name", "GameObject" },
                { "component_type", "Transform" },
                { "property_name", "position" },
                { "property_value", new { x = 1.0, y = 2.0, z = 3.0 } }
            };
        }

        private Dictionary<string, object> GenerateReadFileSample(McpTool tool)
        {
            return new Dictionary<string, object>
            {
                { "file_path", "Assets/Scripts/ExampleScript.cs" },
                { "encoding", "utf-8" }
            };
        }

        private Dictionary<string, object> GenerateWriteFileSample(McpTool tool)
        {
            return new Dictionary<string, object>
            {
                { "file_path", "Assets/Scripts/NewScript.cs" },
                { "content", "using UnityEngine;\n\npublic class NewScript : MonoBehaviour\n{\n    void Start()\n    {\n        Debug.Log(\"Hello World!\");\n    }\n}" },
                { "encoding", "utf-8" },
                { "create_directories", true }
            };
        }

        private Dictionary<string, object> GenerateListFilesSample(McpTool tool)
        {
            return new Dictionary<string, object>
            {
                { "directory_path", "Assets/Scripts" },
                { "pattern", "*.cs" },
                { "recursive", true }
            };
        }

        private Dictionary<string, object> GenerateCreateDirectorySample(McpTool tool)
        {
            return new Dictionary<string, object>
            {
                { "directory_path", "Assets/NewFolder" },
                { "create_parent_directories", true }
            };
        }

        private Dictionary<string, object> GenerateSearchCodebaseSample(McpTool tool)
        {
            return new Dictionary<string, object>
            {
                { "query", "MonoBehaviour Start method" },
                { "file_types", new[] { ".cs" } },
                { "max_results", 10 }
            };
        }

        private Dictionary<string, object> GenerateSearchByRegexSample(McpTool tool)
        {
            return new Dictionary<string, object>
            {
                { "pattern", "public class \\w+" },
                { "directory", "Assets/Scripts" },
                { "file_extensions", new[] { ".cs" } },
                { "case_sensitive", false }
            };
        }

        private Dictionary<string, object> GenerateRunCommandSample(McpTool tool)
        {
            return new Dictionary<string, object>
            {
                { "command", "echo Hello Unity MCP" },
                { "working_directory", "" },
                { "timeout_seconds", 30 }
            };
        }

        private Dictionary<string, object> GenerateExecuteScriptSample(McpTool tool)
        {
            return new Dictionary<string, object>
            {
                { "script_path", "Assets/Scripts/TestScript.cs" },
                { "method_name", "TestMethod" },
                { "parameters", new object[] { "param1", 123, true } }
            };
        }

        private Dictionary<string, object> GenerateGetCurrentSceneInfoSample(McpTool tool)
        {
            return new Dictionary<string, object>();
        }

        #endregion
    }
}