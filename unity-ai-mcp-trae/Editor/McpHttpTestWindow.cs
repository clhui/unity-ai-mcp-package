using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.MCP.Tools.Editor;

namespace Unity.MCP.Editor
{
    /// <summary>
    /// MCP HTTP功能测试面板
    /// 提供所有MCP工具的HTTP接口测试功能
    /// </summary>
    public class McpHttpTestWindow : EditorWindow
    {
        private Vector2 _scrollPosition;
        private Vector2 _toolsScrollPosition;
        private List<string> _testResults = new List<string>();
        private string _serverUrl = "http://localhost:9123/mcp";
        private bool _isTesting = false;
        private int _requestId = 1;
        private Dictionary<string, bool> _groupFoldouts = new Dictionary<string, bool>();
        
        // 工具分组
        private readonly Dictionary<string, List<ToolInfo>> _toolGroups = new Dictionary<string, List<ToolInfo>>
        {
            ["场景管理"] = new List<ToolInfo>
            {
                new ToolInfo("list_scenes", "列出项目场景", new JObject()),
                new ToolInfo("open_scene", "打开指定场景", new JObject { ["path"] = "Assets/Scenes/SampleScene.unity" }),
                new ToolInfo("get_play_mode_status", "获取播放模式状态", new JObject())
            },
            ["播放模式控制"] = new List<ToolInfo>
            {
                new ToolInfo("start_play_mode", "启动播放模式", new JObject()),
                new ToolInfo("stop_play_mode", "停止播放模式", new JObject())
            },
            ["游戏对象基础操作"] = new List<ToolInfo>
            {
                new ToolInfo("create_gameobject", "创建游戏对象", new JObject { ["name"] = "TestObject", ["parent"] = "" }),
                new ToolInfo("find_gameobject", "查找游戏对象", new JObject { ["name"] = "TestObject" }),
                new ToolInfo("get_gameobject_info", "获取游戏对象信息", new JObject { ["name"] = "TestObject" }),
                new ToolInfo("duplicate_gameobject", "复制游戏对象", new JObject { ["name"] = "TestObject", ["newName"] = "TestObject_Copy" }),
                new ToolInfo("delete_gameobject", "删除游戏对象", new JObject { ["name"] = "TestObject" }),
                new ToolInfo("set_parent", "设置父对象", new JObject { ["child"] = "TestObject", ["parent"] = "" })
            },
            ["组件操作"] = new List<ToolInfo>
            {
                new ToolInfo("add_component", "添加组件", new JObject { ["gameObject"] = "TestObject", ["component"] = "Rigidbody" }),
                new ToolInfo("remove_component", "移除组件", new JObject { ["gameObject"] = "TestObject", ["component"] = "Rigidbody" }),
                new ToolInfo("list_components", "列出组件", new JObject { ["gameObject"] = "TestObject" }),
                new ToolInfo("get_component_properties", "获取组件属性", new JObject { ["gameObject"] = "TestObject", ["component"] = "Transform" }),
                new ToolInfo("set_component_properties", "设置组件属性", new JObject { ["gameObject"] = "TestObject", ["component"] = "Transform", ["properties"] = new JObject { ["position"] = new JObject { ["x"] = 1, ["y"] = 2, ["z"] = 3 } } })
            },
            ["变换操作"] = new List<ToolInfo>
            {
                new ToolInfo("set_transform", "设置变换", new JObject { ["gameObject"] = "TestObject", ["position"] = new JObject { ["x"] = 0, ["y"] = 1, ["z"] = 0 } })
            },
            ["材质和渲染"] = new List<ToolInfo>
            {
                new ToolInfo("create_material", "创建材质", new JObject { ["name"] = "TestMaterial", ["shader"] = "Standard" }),
                new ToolInfo("set_material_properties", "设置材质属性", new JObject { ["material"] = "TestMaterial", ["properties"] = new JObject { ["_Color"] = new JObject { ["r"] = 1, ["g"] = 0, ["b"] = 0, ["a"] = 1 } } }),
                new ToolInfo("assign_material", "分配材质", new JObject { ["gameObject"] = "TestObject", ["material"] = "TestMaterial" }),
                new ToolInfo("set_renderer_properties", "设置渲染器属性", new JObject { ["gameObject"] = "TestObject", ["properties"] = new JObject { ["enabled"] = true } })
            },
            ["物理系统"] = new List<ToolInfo>
            {
                new ToolInfo("set_rigidbody_properties", "设置刚体属性", new JObject { ["gameObject"] = "TestObject", ["properties"] = new JObject { ["mass"] = 2.0, ["useGravity"] = true } }),
                new ToolInfo("add_force", "添加力", new JObject { ["gameObject"] = "TestObject", ["force"] = new JObject { ["x"] = 0, ["y"] = 10, ["z"] = 0 } }),
                new ToolInfo("set_collider_properties", "设置碰撞器属性", new JObject { ["gameObject"] = "TestObject", ["collider"] = "BoxCollider", ["properties"] = new JObject { ["isTrigger"] = false } }),
                new ToolInfo("raycast", "射线检测", new JObject { ["origin"] = new JObject { ["x"] = 0, ["y"] = 5, ["z"] = 0 }, ["direction"] = new JObject { ["x"] = 0, ["y"] = -1, ["z"] = 0 }, ["maxDistance"] = 10 })
            },
            ["音频系统"] = new List<ToolInfo>
            {
                new ToolInfo("play_audio", "播放音频", new JObject { ["gameObject"] = "TestObject", ["clip"] = "TestAudioClip" }),
                new ToolInfo("stop_audio", "停止音频", new JObject { ["gameObject"] = "TestObject" }),
                new ToolInfo("set_audio_properties", "设置音频属性", new JObject { ["gameObject"] = "TestObject", ["properties"] = new JObject { ["volume"] = 0.8, ["pitch"] = 1.0 } })
            },
            ["光照系统"] = new List<ToolInfo>
            {
                new ToolInfo("create_light", "创建光源", new JObject { ["name"] = "TestLight", ["type"] = "Directional" }),
                new ToolInfo("set_light_properties", "设置光源属性", new JObject { ["gameObject"] = "TestLight", ["properties"] = new JObject { ["intensity"] = 1.5, ["color"] = new JObject { ["r"] = 1, ["g"] = 1, ["b"] = 1, ["a"] = 1 } } })
            },
            ["预制体管理"] = new List<ToolInfo>
            {
                new ToolInfo("create_prefab", "创建预制体", new JObject { ["gameObjectName"] = "TestCube", ["prefabName"] = "TestCube_Prefab" }),
                new ToolInfo("instantiate_prefab", "实例化预制体", new JObject { ["prefabPath"] = "Assets/Prefabs/TestCube_Prefab.prefab", ["position"] = new JObject { ["x"] = 0, ["y"] = 0, ["z"] = 0 } }),
                new ToolInfo("list_prefabs", "列出预制体", new JObject { ["searchPath"] = "Assets/Prefabs" }),
                new ToolInfo("get_prefab_info", "获取预制体信息", new JObject { ["prefabPath"] = "Assets/Prefabs/TestCube_Prefab.prefab" })
            },
            ["资源管理"] = new List<ToolInfo>
            {
                new ToolInfo("import_asset", "导入资源", new JObject { ["path"] = "Assets/TestAsset.fbx" })
            }
        };
        
        private class ToolInfo
        {
            public string Name { get; }
            public string Description { get; }
            public JObject DefaultArguments { get; }
            
            public ToolInfo(string name, string description, JObject defaultArguments)
            {
                Name = name;
                Description = description;
                DefaultArguments = defaultArguments;
            }
        }
        
        [MenuItem("Unity MCP Trae/3. MCP客户端功能测试面板", false, 5003)]
        public static void ShowWindow()
        {
            var window = GetWindow<McpHttpTestWindow>("MCP HTTP功能测试");
            window.minSize = new Vector2(800, 600);
            window.Show();
        }
        
        private void OnGUI()
        {
            EditorGUILayout.LabelField("MCP HTTP功能测试面板", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            // 服务器设置
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("MCP服务器地址:", GUILayout.Width(100));
            _serverUrl = EditorGUILayout.TextField(_serverUrl);
            if (GUILayout.Button("测试连接", GUILayout.Width(80)))
            {
                TestConnection();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            // 全局操作按钮
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("获取工具列表", GUILayout.Height(30)))
            {
                GetToolsList();
            }
            if (GUILayout.Button("初始化连接", GUILayout.Height(30)))
            {
                InitializeConnection();
            }
            if (GUILayout.Button("清空日志", GUILayout.Height(30)))
            {
                _testResults.Clear();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            // 工具分组测试区域
            EditorGUILayout.LabelField("MCP工具测试", EditorStyles.boldLabel);
            
            _toolsScrollPosition = EditorGUILayout.BeginScrollView(_toolsScrollPosition, "box", GUILayout.Height(300));
            
            foreach (var group in _toolGroups)
            {
                // 初始化折叠状态
                if (!_groupFoldouts.ContainsKey(group.Key))
                    _groupFoldouts[group.Key] = false;
                
                _groupFoldouts[group.Key] = EditorGUILayout.Foldout(_groupFoldouts[group.Key], group.Key, true, EditorStyles.foldoutHeader);
                
                if (_groupFoldouts[group.Key])
                {
                    EditorGUILayout.BeginVertical("box");
                    
                    foreach (var tool in group.Value)
                    {
                        EditorGUILayout.BeginHorizontal();
                        
                        if (GUILayout.Button($"{tool.Name}", GUILayout.Width(200)))
                        {
                            TestTool(tool.Name, tool.DefaultArguments);
                        }
                        
                        EditorGUILayout.LabelField(tool.Description, EditorStyles.miniLabel);
                        
                        EditorGUILayout.EndHorizontal();
                    }
                    
                    EditorGUILayout.EndVertical();
                }
                
                EditorGUILayout.Space();
            }
            
            EditorGUILayout.EndScrollView();
            
            // 测试结果显示区域
            EditorGUILayout.LabelField("测试结果", EditorStyles.boldLabel);
            
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, "box", GUILayout.ExpandHeight(true), GUILayout.MinHeight(150));
            
            foreach (var result in _testResults)
            {
                var color = Color.white;
                if (result.Contains("✅") || result.Contains("成功"))
                    color = Color.green;
                else if (result.Contains("❌") || result.Contains("失败") || result.Contains("错误"))
                    color = Color.red;
                else if (result.Contains("⚠") || result.Contains("警告"))
                    color = Color.yellow;
                
                var originalColor = GUI.color;
                GUI.color = color;
                EditorGUILayout.LabelField(result, EditorStyles.wordWrappedLabel);
                GUI.color = originalColor;
            }
            
            EditorGUILayout.EndScrollView();
            
            // 状态栏
            EditorGUILayout.BeginHorizontal("box");
            EditorGUILayout.LabelField(_isTesting ? "测试进行中..." : "就绪", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"结果数量: {_testResults.Count}", EditorStyles.miniLabel, GUILayout.Width(100));
            EditorGUILayout.EndHorizontal();
        }
        
        private void AddResult(string result)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            _testResults.Add($"[{timestamp}] {result}");
            
            // 自动滚动到底部
            _scrollPosition.y = float.MaxValue;
            
            Repaint();
            

        }
        
        private async void TestConnection()
        {
            if (_isTesting) return;
            
            _isTesting = true;
            AddResult("=== 测试MCP服务器连接 ===");
            
            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(5);
                    
                    var request = new
                    {
                        jsonrpc = "2.0",
                        id = _requestId++,
                        method = "initialize",
                        @params = new
                        {
                            protocolVersion = "2024-11-05",
                            capabilities = new { },
                            clientInfo = new
                            {
                                name = "MCP HTTP Test Panel",
                                version = "1.0.0"
                            }
                        }
                    };
                    
                    var json = JsonConvert.SerializeObject(request);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    
                    var response = await client.PostAsync(_serverUrl, content);
                    var responseText = await response.Content.ReadAsStringAsync();
                    
                    if (response.IsSuccessStatusCode)
                    {
                        var result = JsonConvert.DeserializeObject<JObject>(responseText);
                        if (result["result"] != null)
                        {
                            var serverInfo = result["result"]["serverInfo"];
                            AddResult($"✅ 连接成功！服务器: {serverInfo?["name"]} v{serverInfo?["version"]}");
                        }
                        else
                        {
                            AddResult($"⚠ 连接成功但响应异常: {responseText}");
                        }
                    }
                    else
                    {
                        AddResult($"❌ HTTP错误: {response.StatusCode} - {responseText}");
                    }
                }
            }
            catch (Exception ex)
            {
                AddResult($"❌ 连接失败: {ex.Message}");
            }
            finally
            {
                _isTesting = false;
            }
        }
        
        private async void InitializeConnection()
        {
            if (_isTesting) return;
            
            _isTesting = true;
            AddResult("=== 初始化MCP连接 ===");
            
            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(10);
                    
                    var request = new
                    {
                        jsonrpc = "2.0",
                        id = _requestId++,
                        method = "initialize",
                        @params = new
                        {
                            protocolVersion = "2024-11-05",
                            capabilities = new { tools = new { } },
                            clientInfo = new
                            {
                                name = "MCP HTTP Test Panel",
                                version = "1.0.0"
                            }
                        }
                    };
                    
                    var json = JsonConvert.SerializeObject(request);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    
                    var response = await client.PostAsync(_serverUrl, content);
                    var responseText = await response.Content.ReadAsStringAsync();
                    
                    if (response.IsSuccessStatusCode)
                    {
                        AddResult($"✅ 初始化成功: {responseText}");
                    }
                    else
                    {
                        AddResult($"❌ 初始化失败: {response.StatusCode} - {responseText}");
                    }
                }
            }
            catch (Exception ex)
            {
                AddResult($"❌ 初始化异常: {ex.Message}");
            }
            finally
            {
                _isTesting = false;
            }
        }
        
        private async void GetToolsList()
        {
            if (_isTesting) return;
            
            _isTesting = true;
            AddResult("=== 获取MCP工具列表 ===");
            
            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(10);
                    
                    var request = new
                    {
                        jsonrpc = "2.0",
                        id = _requestId++,
                        method = "tools/list"
                    };
                    
                    var json = JsonConvert.SerializeObject(request);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    
                    var response = await client.PostAsync(_serverUrl, content);
                    var responseText = await response.Content.ReadAsStringAsync();
                    
                    if (response.IsSuccessStatusCode)
                    {
                        var result = JsonConvert.DeserializeObject<JObject>(responseText);
                        if (result["result"]?["tools"] != null)
                        {
                            var tools = result["result"]["tools"] as JArray;
                            AddResult($"✅ 发现 {tools.Count} 个工具:");
                            foreach (var tool in tools)
                            {
                                AddResult($"   • {tool["name"]} - {tool["description"]}");
                            }
                        }
                        else
                        {
                            AddResult($"⚠ 工具列表格式异常: {responseText}");
                        }
                    }
                    else
                    {
                        AddResult($"❌ 获取工具列表失败: {response.StatusCode} - {responseText}");
                    }
                }
            }
            catch (Exception ex)
            {
                AddResult($"❌ 获取工具列表异常: {ex.Message}");
            }
            finally
            {
                _isTesting = false;
            }
        }
        
        private async void TestTool(string toolName, JObject arguments)
        {
            if (_isTesting) return;
            
            _isTesting = true;
            AddResult($"=== 测试工具: {toolName} ===");
            
            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(15);
                    
                    var request = new
                    {
                        jsonrpc = "2.0",
                        id = _requestId++,
                        method = "tools/call",
                        @params = new
                        {
                            name = toolName,
                            arguments = arguments
                        }
                    };
                    
                    var json = JsonConvert.SerializeObject(request);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    
                    AddResult($"📤 发送请求: {toolName}");
                    if (arguments.Count > 0)
                    {
                        AddResult($"📋 参数: {arguments.ToString(Formatting.None)}");
                    }
                    
                    var response = await client.PostAsync(_serverUrl, content);
                    var responseText = await response.Content.ReadAsStringAsync();
                    
                    if (response.IsSuccessStatusCode)
                    {
                        var result = JsonConvert.DeserializeObject<JObject>(responseText);
                        
                        if (result["error"] != null)
                        {
                            AddResult($"❌ 工具执行错误: {result["error"]["message"]}");
                        }
                        else if (result["result"] != null)
                        {
                            var toolResult = result["result"];
                            if (toolResult["content"] != null)
                            {
                                var content_array = toolResult["content"] as JArray;
                                foreach (var contentItem in content_array)
                                {
                                    if (contentItem["type"]?.ToString() == "text")
                                    {
                                        AddResult($"✅ 执行成功: {contentItem["text"]}");
                                    }
                                }
                            }
                            else
                            {
                                AddResult($"✅ 执行成功: {toolResult.ToString(Formatting.None)}");
                            }
                        }
                        else
                        {
                            AddResult($"⚠ 响应格式异常: {responseText}");
                        }
                    }
                    else
                    {
                        AddResult($"❌ HTTP错误: {response.StatusCode} - {responseText}");
                    }
                }
            }
            catch (Exception ex)
            {
                AddResult($"❌ 工具测试异常: {ex.Message}");
            }
            finally
            {
                _isTesting = false;
            }
        }
    }
}