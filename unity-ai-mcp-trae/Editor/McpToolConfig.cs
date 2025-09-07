using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;
using System.IO;

namespace Unity.MCP.Editor
{
    /// <summary>
    /// MCP工具配置管理器
    /// 用于控制每个MCP工具的启用/禁用状态
    /// </summary>
    [System.Serializable]
    public class McpToolConfig
    {
        [SerializeField]
        private Dictionary<string, bool> _toolStates = new Dictionary<string, bool>();
        
        private static McpToolConfig _instance;
        private static readonly string ConfigPath = "ProjectSettings/McpToolConfig.json";
        
        public static McpToolConfig Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = LoadConfig();
                }
                return _instance;
            }
        }
        
        /// <summary>
        /// 获取工具的启用状态
        /// </summary>
        /// <param name="toolName">工具名称</param>
        /// <returns>是否启用，默认为true</returns>
        public bool IsToolEnabled(string toolName)
        {
            return _toolStates.GetValueOrDefault(toolName, true);
        }
        
        /// <summary>
        /// 设置工具的启用状态
        /// </summary>
        /// <param name="toolName">工具名称</param>
        /// <param name="enabled">是否启用</param>
        public void SetToolEnabled(string toolName, bool enabled)
        {
            _toolStates[toolName] = enabled;
            SaveConfig();
        }
        
        /// <summary>
        /// 获取所有工具的状态
        /// </summary>
        /// <returns>工具状态字典</returns>
        public Dictionary<string, bool> GetAllToolStates()
        {
            return new Dictionary<string, bool>(_toolStates);
        }
        
        /// <summary>
        /// 批量设置工具状态
        /// </summary>
        /// <param name="toolStates">工具状态字典</param>
        public void SetAllToolStates(Dictionary<string, bool> toolStates)
        {
            _toolStates.Clear();
            foreach (var kvp in toolStates)
            {
                _toolStates[kvp.Key] = kvp.Value;
            }
            SaveConfig();
        }
        
        /// <summary>
        /// 重置所有工具为启用状态
        /// </summary>
        public void ResetAllToEnabled()
        {
            var allTools = GetAllRegisteredTools();
            foreach (var tool in allTools)
            {
                _toolStates[tool] = true;
            }
            SaveConfig();
        }
        
        /// <summary>
        /// 获取所有已注册的工具名称
        /// </summary>
        /// <returns>工具名称列表</returns>
        public List<string> GetAllRegisteredTools()
        {
            return new List<string>
            {
                // 场景管理工具
                "list_scenes",
                "open_scene", 
                "load_scene",
                "get_current_scene_info",
                
                // 播放模式工具
                "play_mode_start",
                "play_mode_stop",
                "get_play_mode_status",
                
                // 调试工具
                "get_thread_stack_info",
                "get_unity_logs",
                "clear_unity_logs",
                "get_unity_log_stats",
                
                // 游戏对象工具
                "create_gameobject",
                "find_gameobject",
                "delete_gameobject",
                "duplicate_gameobject",
                "set_parent",
                "get_gameobject_info",
                "set_transform",
                
                // 组件管理工具
                "add_component",
                "remove_component",
                "get_component_properties",
                "set_component_properties",
                "list_components",
                
                // 材质和渲染工具
                "create_material",
                "set_material_properties",
                "assign_material",
                "set_renderer_properties",
                
                // 物理系统工具
                "set_rigidbody_properties",
                "add_force",
                "set_collider_properties",
                "raycast",
                
                // 资源管理工具
                "import_asset",
                
                // 脚本工具
                "create_script",
                "modify_script",
                "compile_scripts",
                "get_script_errors",
                
                // UI系统工具
                "create_canvas",
                "create_ui_element",
                "set_ui_properties",
                "bind_ui_events",
                
                // 动画系统工具
                "create_animator",
                "set_animation_clip",
                "play_animation",
                "set_animation_parameters",
                "create_animation_clip",
                
                // 输入系统工具
                "setup_input_actions",
                "bind_input_events",
                "simulate_input",
                "create_input_mapping",
                
                // 粒子系统工具
                "create_particle_system",
                "set_particle_properties",
                "play_particle_effect",
                "create_particle_effect",
                
                // 音频系统工具
                "play_audio",
                "stop_audio",
                "set_audio_properties",
                
                // 光照系统工具
                "create_light",
                "set_light_properties"
            };
        }
        
        /// <summary>
        /// 加载配置文件
        /// </summary>
        /// <returns>配置实例</returns>
        private static McpToolConfig LoadConfig()
        {
            var config = new McpToolConfig();
            
            try
            {
                if (File.Exists(ConfigPath))
                {
                    var json = File.ReadAllText(ConfigPath);
                    var data = JsonConvert.DeserializeObject<Dictionary<string, bool>>(json);
                    if (data != null)
                    {
                        config._toolStates = data;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to load MCP tool config: {ex.Message}");
            }
            
            return config;
        }
        
        /// <summary>
        /// 保存配置文件
        /// </summary>
        public void SaveConfig()
        {
            try
            {
                var directory = Path.GetDirectoryName(ConfigPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                var json = JsonConvert.SerializeObject(_toolStates, Formatting.Indented);
                File.WriteAllText(ConfigPath, json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to save MCP tool config: {ex.Message}");
            }
        }
    }
}