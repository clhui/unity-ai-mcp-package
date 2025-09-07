using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Text.RegularExpressions;
using System;
using System.Linq;
using System.Collections.Generic;

namespace Unity.MCP.Editor
{
    public class McpServerWindow : EditorWindow
    {
        private McpServer _server;
        private bool _autoStart;
        private int _port = 9123;
        private Vector2 _scrollPosition;
        private double _lastRepaintTime;
        private string _version;
        private Vector2 _clientScrollPosition;
        private bool _isPlayModeTransition = false;
        
        // 工具配置相关
        private Vector2 _toolConfigScrollPosition;
        private string _toolSearchFilter = "";
        private int _priorityFilter = 0; // 0=全部, 1=高优, 2=中优, 3=低优
        
        // 工具中文名称映射
        private static readonly Dictionary<string, string> _toolChineseNames = new Dictionary<string, string>
        {
            // 场景管理工具
            ["list_scenes"] = "列出场景",
            ["open_scene"] = "打开场景",
            ["load_scene"] = "加载场景",
            
            // 播放模式工具
            ["play_mode_start"] = "开始播放",
            ["play_mode_stop"] = "停止播放",
            ["get_play_mode_status"] = "获取播放状态",
            
            // 调试工具
            ["get_current_scene_info"] = "获取场景信息",
            ["get_thread_stack_info"] = "获取线程堆栈",
            
            // 日志工具
            ["get_unity_logs"] = "获取日志",
            ["clear_unity_logs"] = "清空日志",
            ["get_unity_log_stats"] = "获取日志统计",
            
            // 游戏对象工具
            ["create_gameobject"] = "创建游戏对象",
            ["find_gameobject"] = "查找游戏对象",
            ["delete_gameobject"] = "删除游戏对象",
            ["duplicate_gameobject"] = "复制游戏对象",
            ["set_parent"] = "设置父对象",
            ["get_gameobject_info"] = "获取对象信息",
            ["set_transform"] = "设置变换",
            
            // 组件管理工具
            ["add_component"] = "添加组件",
            ["remove_component"] = "移除组件",
            ["get_component_properties"] = "获取组件属性",
            ["set_component_properties"] = "设置组件属性",
            ["list_components"] = "列出组件",
            
            // 材质渲染工具
            ["create_material"] = "创建材质",
            ["set_material_properties"] = "设置材质属性",
            ["assign_material"] = "分配材质",
            ["set_renderer_properties"] = "设置渲染器属性",
            
            // 物理系统工具
            ["set_rigidbody_properties"] = "设置刚体属性",
            ["add_force"] = "添加力",
            ["set_collider_properties"] = "设置碰撞器属性",
            ["raycast"] = "射线检测",
            
            // 音频系统工具
            ["play_audio"] = "播放音频",
            ["stop_audio"] = "停止音频",
            ["set_audio_properties"] = "设置音频属性",
            
            // 光照系统工具
            ["create_light"] = "创建光源",
            ["set_light_properties"] = "设置光源属性",
            
            // 脚本管理工具
            ["create_script"] = "创建脚本",
            ["modify_script"] = "修改脚本",
            ["compile_scripts"] = "编译脚本",
            ["get_script_errors"] = "获取脚本错误",
            
            // UI系统工具
            ["create_canvas"] = "创建画布",
            ["create_ui_element"] = "创建UI元素",
            ["set_ui_properties"] = "设置UI属性",
            ["bind_ui_events"] = "绑定UI事件",
            
            // 动画系统工具
            ["create_animator"] = "创建动画控制器",
            ["set_animation_clip"] = "设置动画片段",
            ["play_animation"] = "播放动画",
            ["set_animation_parameters"] = "设置动画参数",
            ["create_animation_clip"] = "创建动画片段",
            
            // 输入系统工具
            ["setup_input_actions"] = "设置输入动作",
            ["bind_input_events"] = "绑定输入事件",
            ["simulate_input"] = "模拟输入",
            ["create_input_mapping"] = "创建输入映射",
            
            // 粒子系统工具
            ["create_particle_system"] = "创建粒子系统",
            ["set_particle_properties"] = "设置粒子属性",
            ["play_particle_effect"] = "播放粒子效果",
            ["create_particle_effect"] = "创建粒子效果",
            
            // 资源管理工具
            ["import_asset"] = "导入资源"
        };
        private bool _showToolConfig = false;
        private McpToolConfig _toolConfig;
        
        public McpServerWindow()
        {
        }
        
        // 工具优先级定义
        private enum ToolPriority
        {
            High,    // 高优先级
            Medium,  // 中优先级
            Low      // 低优先级
        }
        
        // 获取工具优先级
        private ToolPriority GetToolPriority(string toolName)
        {
            // 高优先级工具 - 常用的基础功能
            var highPriorityTools = new HashSet<string>
            {
                "create_gameobject", "find_gameobject", "delete_gameobject", "set_transform",
                "list_scenes", "open_scene", "load_scene",
                "play_mode_start", "play_mode_stop", "get_play_mode_status",
                "get_current_scene_info", "add_component", "remove_component",
                "get_unity_logs", "clear_unity_logs"
            };
            
            // 低优先级工具 - 高级或特殊功能
            var lowPriorityTools = new HashSet<string>
            {
                "get_thread_stack_info", "get_unity_log_stats", "duplicate_gameobject",
                "compile_scripts", "get_script_errors", "bind_ui_events",
                "simulate_input", "create_input_mapping", "create_particle_effect",
                "import_asset", "create_animation_clip", "set_animation_parameters"
            };
            
            if (highPriorityTools.Contains(toolName))
                return ToolPriority.High;
            if (lowPriorityTools.Contains(toolName))
                return ToolPriority.Low;
            return ToolPriority.Medium;
        }
        
        // 获取优先级颜色
        private Color GetPriorityColor(ToolPriority priority)
        {
            switch (priority)
            {
                case ToolPriority.High: return new Color(0f, 1f, 0f, 0.8f); // 超鲜绿色背景
                case ToolPriority.Medium: return new Color(0f, 0f, 1f, 0.8f); // 超鲜蓝色背景
                case ToolPriority.Low: return new Color(0.4f, 0.9f, 1f, 0.7f); // 超鲜浅蓝色背景
                default: return Color.white;
            }
        }
        
        // 获取优先级标签
        private string GetPriorityLabel(ToolPriority priority)
        {
            switch (priority)
            {
                case ToolPriority.High: return "[高优]";
                case ToolPriority.Medium: return "[中优]";
                case ToolPriority.Low: return "[低优]";
                default: return "";
            }
        }
        
        private string GetToolDisplayName(string toolName)
        {
            if (_toolChineseNames.TryGetValue(toolName, out var chineseName))
            {
                return $"{chineseName} ({toolName})";
            }
            return toolName;
        }
        
        private void DrawToolConfigSection(string sectionName, string[] toolNames)
        {
            // 绘制分类标题
            EditorGUILayout.LabelField(sectionName, EditorStyles.boldLabel);
            
            // 按优先级分组工具
            var toolsByPriority = toolNames
                .Where(toolName => {
                    // 文本搜索过滤
                    var matchesSearch = string.IsNullOrEmpty(_toolSearchFilter) || 
                                      toolName.ToLower().Contains(_toolSearchFilter.ToLower());
                    
                    // 优先级过滤
                    var toolPriority = GetToolPriority(toolName);
                    var matchesPriority = _priorityFilter == 0 || 
                                         (_priorityFilter == 1 && toolPriority == ToolPriority.High) ||
                                         (_priorityFilter == 2 && toolPriority == ToolPriority.Medium) ||
                                         (_priorityFilter == 3 && toolPriority == ToolPriority.Low);
                    
                    return matchesSearch && matchesPriority;
                })
                .GroupBy(GetToolPriority)
                .OrderBy(g => g.Key)
                .ToArray();
            
            if (toolsByPriority.Length == 0)
            {
                EditorGUILayout.Space(5);
                return;
            }
            
            foreach (var priorityGroup in toolsByPriority)
            {
                var priority = priorityGroup.Key;
                var tools = priorityGroup.ToArray();
                
                // 绘制优先级标题
                var priorityColor = GetPriorityColor(priority);
                var originalColor = GUI.backgroundColor;
                GUI.backgroundColor = priorityColor;
                
                EditorGUILayout.BeginVertical("box");
                GUI.backgroundColor = originalColor;
                
                var priorityLabel = GetPriorityLabel(priority);
                EditorGUILayout.LabelField($"{priorityLabel} ({tools.Length}个工具)", EditorStyles.miniLabel);
                
                // 简单的垂直布局显示工具
                foreach (var toolName in tools)
                {
                    EditorGUILayout.BeginHorizontal();
                    
                    var isEnabled = _toolConfig.IsToolEnabled(toolName);
                    var newEnabled = EditorGUILayout.Toggle(isEnabled, GUILayout.Width(20));
                    
                    if (newEnabled != isEnabled)
                    {
                        _toolConfig.SetToolEnabled(toolName, newEnabled);
                        _toolConfig.SaveConfig();
                        
                        // 实时刷新工具注册状态
                        if (_server != null && _server.IsRunning)
                        {
                            _server.RefreshTools();
                        }
                    }
                    
                    var displayName = GetToolDisplayName(toolName);
                    EditorGUILayout.LabelField(displayName, GUILayout.ExpandWidth(true));
                    
                    EditorGUILayout.EndHorizontal();
                }
                
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(3);
            }
            
            EditorGUILayout.Space(5);
        }
        
        private void Awake()
        {
        }
        
        private void OnFocus()
        {
        }
        
        private void OnLostFocus()
        {
        }
        
        private void OnDestroy()
        {
        }
        
        [MenuItem("Unity MCP Trae/1. MCP服务管理面板", false, 5001)]
        public static void ShowWindow()
        {
            GetWindow<McpServerWindow>("MCP Server");
        }
        
        private void OnEnable()
        {
            _autoStart = EditorPrefs.GetBool("McpServer.AutoStart", false);
            _port = EditorPrefs.GetInt("McpServer.Port", 9123);
            
            // 加载Debug模式设置
            McpLogger.IsDebugEnabled = EditorPrefs.GetBool("McpServer.DebugMode", false);
            
            // 初始化工具配置
            _toolConfig = McpToolConfig.Instance;
            
            // 读取版本号
            LoadVersion();
            StartServer();
            // 监听播放模式状态变化
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            
            // 监听场景变化
            EditorSceneManager.activeSceneChangedInEditMode += OnActiveSceneChanged;
            EditorSceneManager.sceneOpened += OnSceneOpened;
        }
        
        private void OnDisable()
        {
            // 移除播放模式状态监听
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            
            // 移除场景变化监听
            EditorSceneManager.activeSceneChangedInEditMode -= OnActiveSceneChanged;
            EditorSceneManager.sceneOpened -= OnSceneOpened;
            
            // 只在非播放模式转换时停止服务器
            if (!_isPlayModeTransition)
            {
                // 保存服务器运行状态
                EditorPrefs.SetBool("McpServer.WasRunning", _server?.IsRunning == true);
                StopServer();
            }
        }
        
        private void OnGUI()
        {
            
            // 如果服务器正在运行，每2秒重绘一次以更新客户端状态
            if (_server?.IsRunning == true)
            {
                var currentTime = EditorApplication.timeSinceStartup;
                if (currentTime - _lastRepaintTime > 2.0)
                {
                    _lastRepaintTime = currentTime;
                    Repaint();
                }
            }
            
            // 插件名称和标题
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Unity MCP Trae", EditorStyles.largeLabel);
            GUILayout.FlexibleSpace();
            if (!string.IsNullOrEmpty(_version))
            {
                GUILayout.Label($"v{_version}", EditorStyles.miniLabel);
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("MCP服务控制面板", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            // 左右两列布局
            EditorGUILayout.BeginHorizontal();
            
            // 左列：服务控制、客户端配置、连接的客户端
            EditorGUILayout.BeginVertical(GUILayout.Width(position.width / 2 - 10));
            
            DrawServerControlSection();
            EditorGUILayout.Space(10);
            
            DrawClientConfigSection();
            EditorGUILayout.Space(10);
            
            DrawConnectedClientsSection();
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(10);
            
            // 右列：工具配置
            EditorGUILayout.BeginVertical(GUILayout.Width(position.width / 2 - 10), GUILayout.ExpandHeight(true));
            
            DrawToolConfigurationSection();
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
        }
        
        private void DrawServerControlSection()
        {
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("🔧 服务控制", EditorStyles.boldLabel);
            
            // 服务器状态
            EditorGUILayout.LabelField("状态:", _server?.IsRunning == true ? "运行中" : "已停止");
            
            if (_server?.IsRunning == true)
            {
                EditorGUILayout.LabelField("端口:", _server.Port.ToString());
                EditorGUILayout.LabelField("接口地址:", $"http://localhost:{_server.Port}/mcp");
            }
            
            EditorGUILayout.Space();
            
            // 端口设置
            EditorGUI.BeginDisabledGroup(_server?.IsRunning == true);
            var newPort = EditorGUILayout.IntField("端口:", _port);
            if (newPort != _port)
            {
                _port = newPort;
                EditorPrefs.SetInt("McpServer.Port", _port);
            }
            EditorGUI.EndDisabledGroup();
            
            // 自动启动设置
            var newAutoStart = EditorGUILayout.Toggle("自动启动:", _autoStart);
            if (newAutoStart != _autoStart)
            {
                _autoStart = newAutoStart;
                EditorPrefs.SetBool("McpServer.AutoStart", _autoStart);
            }
            
            // Debug模式设置
            var newDebugMode = EditorGUILayout.Toggle("Debug模式:", McpLogger.IsDebugEnabled);
            if (newDebugMode != McpLogger.IsDebugEnabled)
            {
                McpLogger.IsDebugEnabled = newDebugMode;
                EditorPrefs.SetBool("McpServer.DebugMode", newDebugMode);
            }

            EditorGUILayout.Space();
            
            // 控制按钮
            EditorGUILayout.BeginHorizontal();
            
            EditorGUI.BeginDisabledGroup(_server?.IsRunning == true);
            if (GUILayout.Button("启动服务器"))
            {
                StartServer();
            }
            EditorGUI.EndDisabledGroup();
            
            EditorGUI.BeginDisabledGroup(_server?.IsRunning != true);
            if (GUILayout.Button("停止服务器"))
            {
                StopServer();
            }
            EditorGUI.EndDisabledGroup();
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawClientConfigSection()
        {
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("⚙️ 客户端配置", EditorStyles.boldLabel);
            
            var configJson = $@"{{
  ""mcpServers"": {{
    ""clh-unity-mcp"": {{
      ""url"": ""http://localhost:{_server.Port}/mcp""
    }}
  }}
}}";
            
            EditorGUILayout.LabelField("复制此配置到您的AI工具中:", EditorStyles.miniLabel);
            
            var textAreaStyle = new GUIStyle(EditorStyles.textArea)
            {
                wordWrap = true,
                fontSize = 10
            };
            
            EditorGUILayout.TextArea(configJson, textAreaStyle, GUILayout.Height(120));
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("复制到剪贴板"))
            {
                EditorGUIUtility.systemCopyBuffer = configJson;
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            // 快速推送按钮
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("推送到 Claude Desktop"))
            {
                PushToClaudeDesktop(configJson);
            }
            
            if (GUILayout.Button("推送到 Cursor"))
            {
                PushToCursor(configJson);
            }
            
            if (GUILayout.Button("推送到 Trae AI"))
            {
                PushToTraeAI(configJson);
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawConnectedClientsSection()
        {
            if (_server?.IsRunning != true) return;
            
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("👥 连接的客户端", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            
            var clients = _server.ConnectedClients;
            if (clients.Count > 0)
            {
                GUILayout.Label($"({Math.Min(clients.Count, 100)}/{clients.Count})", EditorStyles.miniLabel);
            }
            EditorGUILayout.EndHorizontal();
            
            if (clients.Count > 0)
            {
                // 限制显示最近100条记录，并添加滚动条
                var clientsToShow = clients.Values.OrderByDescending(c => c.LastSeen).Take(100).ToList();
                
                _clientScrollPosition = EditorGUILayout.BeginScrollView(_clientScrollPosition, GUILayout.Height(120));
                
                foreach (var client in clientsToShow)
                {
                    EditorGUILayout.BeginHorizontal(GUILayout.Height(16)); // 减小行高
                    
                    var timeSinceLastSeen = DateTime.Now - client.LastSeen;
                    var statusColor = timeSinceLastSeen.TotalSeconds < 30 ? "green" : "orange";
                    
                    var labelStyle = new GUIStyle(EditorStyles.miniLabel) { richText = true };
                    EditorGUILayout.LabelField($"<color={statusColor}>●</color> {client.RemoteEndPoint}", labelStyle, GUILayout.Width(140));
                    EditorGUILayout.LabelField($"请求: {client.RequestCount}", EditorStyles.miniLabel, GUILayout.Width(60));
                    EditorGUILayout.LabelField($"最后活动: {timeSinceLastSeen.TotalSeconds:F0}秒前", EditorStyles.miniLabel, GUILayout.Width(80));
                    
                    if (!string.IsNullOrEmpty(client.UserAgent) && client.UserAgent != "unknown")
                    {
                        var shortUserAgent = client.UserAgent.Length > 25 ? client.UserAgent.Substring(0, 25) + "..." : client.UserAgent;
                        EditorGUILayout.LabelField($"客户端: {shortUserAgent}", EditorStyles.miniLabel, GUILayout.ExpandWidth(true));
                    }
                    
                    EditorGUILayout.EndHorizontal();
                }
                
                EditorGUILayout.EndScrollView();
                
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("清理断开的客户端", GUILayout.Width(120)))
                {
                    _server.ClearDisconnectedClients();
                }
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.HelpBox("暂无客户端连接", MessageType.Info);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawToolConfigurationSection()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();
            
            // 计算已选择的工具数量
            var allTools = _toolConfig.GetAllRegisteredTools();
            var enabledCount = allTools.Count(tool => _toolConfig.IsToolEnabled(tool));
            var totalCount = allTools.Count();
            
            GUILayout.Label($"🔨 工具开关配置 (已选择: {enabledCount}/{totalCount})", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button(_showToolConfig ? "隐藏配置" : "显示配置", GUILayout.Width(80)))
            {
                _showToolConfig = !_showToolConfig;
            }
            EditorGUILayout.EndHorizontal();
            
            if (_showToolConfig)
            {
                EditorGUILayout.Space(5);
                
                // 搜索框和优先级过滤
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("搜索:", GUILayout.Width(40));
                _toolSearchFilter = EditorGUILayout.TextField(_toolSearchFilter);
                if (GUILayout.Button("清空", GUILayout.Width(40)))
                {
                    _toolSearchFilter = "";
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("优先级:", GUILayout.Width(50));
                var priorityOptions = new string[] { "全部", "高优", "中优", "低优" };
                _priorityFilter = EditorGUILayout.Popup(_priorityFilter, priorityOptions);
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Space(3);
                
                // 批量操作按钮
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("全选", GUILayout.Height(20)))
                {
                    foreach (var toolName in _toolConfig.GetAllRegisteredTools())
                    {
                        _toolConfig.SetToolEnabled(toolName, true);
                    }
                    _toolConfig.SaveConfig();
                    
                    // 实时刷新工具注册状态
                    if (_server != null && _server.IsRunning)
                    {
                        _server.RefreshTools();
                    }
                }
                if (GUILayout.Button("全不选", GUILayout.Height(20)))
                {
                    foreach (var toolName in _toolConfig.GetAllRegisteredTools())
                    {
                        _toolConfig.SetToolEnabled(toolName, false);
                    }
                    _toolConfig.SaveConfig();
                    
                    // 实时刷新工具注册状态
                    if (_server != null && _server.IsRunning)
                    {
                        _server.RefreshTools();
                    }
                }
                if (GUILayout.Button("重置默认", GUILayout.Height(20)))
                {
                    _toolConfig.ResetAllToEnabled();
                    
                    // 实时刷新工具注册状态
                    if (_server != null && _server.IsRunning)
                    {
                        _server.RefreshTools();
                    }
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Space(3);
                
                // 工具配置滚动区域
                _toolConfigScrollPosition = EditorGUILayout.BeginScrollView(_toolConfigScrollPosition, GUILayout.Height(500));
                
                DrawToolConfigSection("场景管理工具", new[] { "list_scenes", "open_scene", "load_scene" });
                DrawToolConfigSection("播放模式工具", new[] { "play_mode_start", "play_mode_stop", "get_play_mode_status" });
                DrawToolConfigSection("调试工具", new[] { "get_current_scene_info", "get_thread_stack_info" });
                DrawToolConfigSection("日志工具", new[] { "get_unity_logs", "clear_unity_logs", "get_unity_log_stats" });
                DrawToolConfigSection("游戏对象工具", new[] { "create_gameobject", "find_gameobject", "delete_gameobject", "duplicate_gameobject", "set_parent", "get_gameobject_info", "set_transform" });
                DrawToolConfigSection("组件管理工具", new[] { "add_component", "remove_component", "get_component_properties", "set_component_properties", "list_components" });
                DrawToolConfigSection("材质渲染工具", new[] { "create_material", "set_material_properties", "assign_material", "set_renderer_properties" });
                DrawToolConfigSection("物理系统工具", new[] { "set_rigidbody_properties", "add_force", "set_collider_properties", "raycast" });
                DrawToolConfigSection("音频系统工具", new[] { "play_audio", "stop_audio", "set_audio_properties" });
                DrawToolConfigSection("光照系统工具", new[] { "create_light", "set_light_properties" });
                DrawToolConfigSection("脚本管理工具", new[] { "create_script", "modify_script", "compile_scripts", "get_script_errors" });
                DrawToolConfigSection("UI系统工具", new[] { "create_canvas", "create_ui_element", "set_ui_properties", "bind_ui_events" });
                DrawToolConfigSection("动画系统工具", new[] { "create_animator", "set_animation_clip", "play_animation", "set_animation_parameters", "create_animation_clip" });
                DrawToolConfigSection("输入系统工具", new[] { "setup_input_actions", "bind_input_events", "simulate_input", "create_input_mapping" });
                DrawToolConfigSection("粒子系统工具", new[] { "create_particle_system", "set_particle_properties", "play_particle_effect", "create_particle_effect" });
                DrawToolConfigSection("资源管理工具", new[] { "import_asset" });
                
                EditorGUILayout.EndScrollView();
                
                EditorGUILayout.Space(3);
                
                // 配置提示
                if (_server != null && _server.IsRunning)
                {
                    EditorGUILayout.HelpBox("工具配置修改后立即生效，无需重启服务器。", MessageType.Info);
                }
                else
                {
                    EditorGUILayout.HelpBox("请先启动MCP服务器，然后修改工具配置将实时生效。", MessageType.Warning);
                }
            }
            else
            {
                // 简化的工具状态显示
                var simpleEnabledCount = _toolConfig.GetAllRegisteredTools().Count(name => _toolConfig.IsToolEnabled(name));
                var simpleTotalCount = _toolConfig.GetAllRegisteredTools().Count;
                EditorGUILayout.LabelField($"已启用工具: {simpleEnabledCount}/{simpleTotalCount}", EditorStyles.miniLabel);
            }
            
            EditorGUILayout.EndVertical();
        
        
            // 使用说明
            if (_server?.IsRunning == true)
            {
                EditorGUILayout.HelpBox(
                    "MCP服务器正在运行！请将上方的配置复制到您的AI工具的MCP设置中。" +
                    "服务器通过列出的工具提供Unity编辑器控制功能。" +
                    "为了安全起见，请仅在可信网络中运行。",
                    MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "启动MCP服务器以启用AI工具与Unity编辑器的集成。" +
                    "启动后，您将看到可复制到AI工具的配置JSON。",
                    MessageType.Info);
            }
        }
        
        private void StartServer()
        {
            if (_server?.IsRunning == true) 
            {
                return;
            }
            
            try
            {
                // 创建新的服务器实例
                _server = new McpServer(_port);
                
                // 启动服务器
                _server.Start();
                //打印服务器状态
                McpLogger.LogTool($"启动MCP服务器，端口: {_port}");
            }
            catch (System.Exception ex)
            {
                EditorUtility.DisplayDialog("错误", $"启动MCP服务器失败: {ex.Message}", "确定");
            }
        }
        
        /// <summary>
        /// 静态方法用于外部启动服务器
        /// </summary>
        public static void StartServerStatic()
        {
            var window = GetWindow<McpServerWindow>();
            if (window != null)
            {
                window.StartServer();
            }
        }
        

        
        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            switch (state)
            {
                case PlayModeStateChange.ExitingEditMode:
                    _isPlayModeTransition = true;
                    if (_server?.IsRunning == true)
                    {
                        //打印服务器状态
                        McpLogger.LogDebug($"退出编辑模式，服务器状态: {_server?.IsRunning}");
                        StopServer();
                    }
                    break;
                    
                case PlayModeStateChange.ExitingPlayMode:
                    _isPlayModeTransition = true;
                    if (_server?.IsRunning == true)
                    {
                        //打印服务器状态
                        McpLogger.LogDebug($"退出播放模式，服务器状态: {_server?.IsRunning}");
                        StopServer();
                    }
                    break;
                    
                case PlayModeStateChange.EnteredEditMode:
                    _isPlayModeTransition = false;
                    if (_autoStart ) {
                        Debug.Log($"[McpServerWindow] 进入编辑模式: ，自动启动MCP服务器");
                        StopServer();
                        StartServer();
                    }
                    //打印服务器状态
                    McpLogger.LogDebug($"进入编辑模式，服务器状态: {_server?.IsRunning}");
                    break;
                    
                case PlayModeStateChange.EnteredPlayMode:
                    _isPlayModeTransition = false;
                    if (_autoStart) {
                        McpLogger.LogTool("进入播放模式，自动启动MCP服务器");
                        StopServer();
                        StartServer();
                    }
                    //打印服务器状态
                    McpLogger.LogDebug($"进入播放模式，服务器状态: {_server?.IsRunning}");
                    break;
            }
        }
        
        private void OnActiveSceneChanged(UnityEngine.SceneManagement.Scene previousScene, UnityEngine.SceneManagement.Scene newScene)
        {
            if (newScene.IsValid())
            {
                OnSceneChanged(newScene.name, newScene.path);
            }else{
                //打印服务器状态
                McpLogger.LogDebug($"场景变化检测到: {newScene.name}，场景无效");
            }
        }
        
        private void OnSceneOpened(UnityEngine.SceneManagement.Scene scene, UnityEditor.SceneManagement.OpenSceneMode mode)
        {
            OnSceneChanged(scene.name, scene.path);
        }
        
        private void OnSceneChanged(string sceneName, string scenePath)
        {
            // 场景变化时自动启动服务（如果启用了自动启动且服务未运行）
            if (_autoStart && (_server?.IsRunning != true))
            {
                Debug.Log($"[McpServerWindow] 场景变化检测到: {sceneName}，自动启动MCP服务器");
                StartServer();
            }else{
                //打印服务器状态
                McpLogger.LogDebug($"OnSceneChanged场景变化检测到: {sceneName}，场景无效");
            }
        }
        

        
        private void StopServer()
        {
            if (_server?.IsRunning != true) 
            {
                return;
            }
            
            try
            {
                _server.Stop();
                _server = null;
                EditorPrefs.SetBool("McpServer.WasRunning", false);
                //打印服务器状态
                McpLogger.LogTool("停止MCP服务器");
            }
            catch (System.Exception ex)
            {
                // 静默处理停止错误
            }
        }
        

        
        private void PushToClaudeDesktop(string configJson)
        {
            try
            {
                var appDataPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData);
                var claudeConfigPath = System.IO.Path.Combine(appDataPath, "Claude", "claude_desktop_config.json");
                
                UpdateMcpConfig(claudeConfigPath, configJson, "Claude Desktop");
            }
            catch (System.Exception ex)
            {
                EditorUtility.DisplayDialog("错误", $"推送到Claude Desktop失败: {ex.Message}", "确定");
            }
        }
        
        private void PushToCursor(string configJson)
        {
            try
            {
                var appDataPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData);
                var cursorConfigPath = System.IO.Path.Combine(appDataPath, "Cursor", "User", "settings.json");
                
                UpdateMcpConfig(cursorConfigPath, configJson, "Cursor");
            }
            catch (System.Exception ex)
            {
                EditorUtility.DisplayDialog("错误", $"推送到Cursor失败: {ex.Message}", "确定");
            }
        }
        
        private void PushToTraeAI(string configJson)
        {
            try
            {
                var appDataPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData);
                var traeConfigPath = System.IO.Path.Combine(appDataPath, "Trae", "mcp_config.json");
                
                UpdateMcpConfig(traeConfigPath, configJson, "Trae AI");
            }
            catch (System.Exception ex)
            {
                EditorUtility.DisplayDialog("错误", $"推送到Trae AI失败: {ex.Message}", "确定");
            }
        }
        
        private void LoadVersion()
        {
            try
            {
                // 获取程序集定义文件路径
                var asmdefPath = System.IO.Path.Combine(Application.dataPath.Replace("/Assets", ""), "Assets", "Editor", "Unity.MCP.Editor.asmdef");
                
                // 如果在Packages目录中，尝试其他路径
                if (!System.IO.File.Exists(asmdefPath))
                {
                    var packagePath = "Packages/com.clh.unity-mcp/Editor/Unity.MCP.Editor.asmdef";
                    asmdefPath = System.IO.Path.Combine(Application.dataPath.Replace("/Assets", ""), packagePath);
                }
                
                // 如果还是找不到，尝试相对于当前脚本的路径
                if (!System.IO.File.Exists(asmdefPath))
                {
                    var scriptPath = new System.Diagnostics.StackTrace(true).GetFrame(0).GetFileName();
                    var scriptDir = System.IO.Path.GetDirectoryName(scriptPath);
                    asmdefPath = System.IO.Path.Combine(scriptDir, "Unity.MCP.Editor.asmdef");
                }
                
                if (System.IO.File.Exists(asmdefPath))
                {
                    var json = System.IO.File.ReadAllText(asmdefPath);
                    
                    // 简单的JSON解析获取版本号
                    var versionMatch = System.Text.RegularExpressions.Regex.Match(json, @"""version""\s*:\s*""([^""]+)""");
                    if (versionMatch.Success)
                    {
                        _version = versionMatch.Groups[1].Value;
                    }
                    else
                    {
                        _version = "未知";
                    }
                }
                else
                {
                    _version = "未找到";
                }
            }
            catch (System.Exception ex)
            {
                _version = "错误";
            }
        }
        

        
        private string GetGameObjectPath(GameObject obj)
        {
            string path = obj.name;
            Transform parent = obj.transform.parent;
            
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            
            return path;
        }
        
        private void UpdateMcpConfig(string configPath, string newConfigJson, string editorName)
        {
            try
            {
                // 确保目录存在
                var directory = System.IO.Path.GetDirectoryName(configPath);
                if (!System.IO.Directory.Exists(directory))
                {
                    System.IO.Directory.CreateDirectory(directory);
                }
                
                string finalConfigJson;
                
                // 如果配置文件已存在，尝试合并配置
                if (System.IO.File.Exists(configPath))
                {
                    try
                    {
                        var existingJson = System.IO.File.ReadAllText(configPath);
                        
                        // 简单的JSON合并：查找mcpServers部分并替换或添加clh-unity-mcp
                        if (existingJson.Contains("\"mcpServers\""))
                        {
                            // 如果已存在clh-unity-mcp，替换它
                            if (existingJson.Contains("\"clh-unity-mcp\""))
                            {
                                // 使用正则表达式替换现有的clh-unity-mcp配置
                                var pattern = @"""clh-unity-mcp""\s*:\s*\{[^}]*\}";
                                var replacement = "\"clh-unity-mcp\": {\"url\": \"http://localhost:" + _server.Port + "/mcp\"}";
                                finalConfigJson = System.Text.RegularExpressions.Regex.Replace(existingJson, pattern, replacement);
                            }
                            else
                            {
                                // 在mcpServers中添加新的clh-unity-mcp配置
                                var mcpServersEnd = existingJson.LastIndexOf("}", existingJson.IndexOf("}", existingJson.IndexOf("\"mcpServers\"")));
                                var insertion = ",\n    \"clh-unity-mcp\": {\n      \"url\": \"http://localhost:" + _server.Port + "/mcp\"\n    }";
                                finalConfigJson = existingJson.Insert(mcpServersEnd, insertion);
                            }
                        }
                        else
                        {
                            // 添加整个mcpServers部分
                            var lastBrace = existingJson.LastIndexOf("}");
                            var insertion = ",\n  \"mcpServers\": {\n    \"clh-unity-mcp\": {\n      \"url\": \"http://localhost:" + _server.Port + "/mcp\"\n    }\n  }";
                            finalConfigJson = existingJson.Insert(lastBrace, insertion);
                        }
                    }
                    catch
                    {
                        // 如果解析失败，直接使用新配置
                        finalConfigJson = newConfigJson;
                    }
                }
                else
                {
                    // 文件不存在，直接使用新配置
                    finalConfigJson = newConfigJson;
                }
                
                // 写入配置文件
                System.IO.File.WriteAllText(configPath, finalConfigJson);
                
                EditorUtility.DisplayDialog("成功", $"MCP配置已成功推送到{editorName}!\n\n配置文件路径:\n{configPath}\n\n请重启{editorName}以使配置生效。", "确定");
            }
            catch (System.Exception ex)
            {
                throw new System.Exception($"更新配置文件失败: {ex.Message}");
            }
        }
    }
}