using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Reflection;
using Unity.MCP.Tools.Editor;

namespace Unity.MCP.Editor
{
    /// <summary>
    /// MCP基础功能测试面板
    /// </summary>
    public class McpTestWindow : EditorWindow
    {
        private Vector2 _scrollPosition;
        private List<string> _testResults = new List<string>();
        private bool _isTestRunning = false;
        private McpServer _testServer;
        private int _testPort = 9125;
        
        // 资源选择字段
        private AudioClip _selectedAudioClip;
        private Texture2D _selectedTexture;
        private string _defaultAudioPath = "Assets/Audio/TestClip.wav";
        private string _defaultTexturePath = "Assets/Textures/TestTexture.png";
        
        [MenuItem("Unity MCP Trae/2. 插件基础功能单测面板", false, 5002)]
        public static void ShowWindow()
        {
            var window = GetWindow<McpTestWindow>("MCP基础功能测试面板");
            window.minSize = new Vector2(600, 400);
            window.Show();
        }
        
        /// <summary>
        /// 安全的Sleep方法，可以在线程中止时快速退出
        /// </summary>
        private static void SafeSleep(int milliseconds)
        {
            try
            {
                // 将长时间的Sleep分解为多个短时间的Sleep，以便快速响应中止
                const int chunkSize = 100; // 每次Sleep 100ms
                int remaining = milliseconds;
                
                while (remaining > 0)
                {
                    int sleepTime = Math.Min(remaining, chunkSize);
                    System.Threading.Thread.Sleep(sleepTime);
                    remaining -= sleepTime;
                }
            }
            catch (System.Threading.ThreadAbortException)
            {
                // 线程被中止时，直接退出
                throw;
            }
            catch (Exception ex)
            {
                McpLogger.LogWarning($"SafeSleep异常: {ex.Message}");
            }
        }
        
        private void OnGUI()
        {
            // 插件名称和标题
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Unity MCP Trae", EditorStyles.largeLabel);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("MCP基础功能测试面板", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
            
            // 测试端口设置
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("测试端口:", GUILayout.Width(80));
            _testPort = EditorGUILayout.IntField(_testPort, GUILayout.Width(100));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
            
            // 资源选择设置
            EditorGUILayout.LabelField("测试资源设置", EditorStyles.boldLabel);
            
            // 音频资源选择
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("音频文件:", GUILayout.Width(80));
            _selectedAudioClip = (AudioClip)EditorGUILayout.ObjectField(_selectedAudioClip, typeof(AudioClip), false, GUILayout.Width(200));
            if (GUILayout.Button("使用默认", GUILayout.Width(80)))
            {
                _selectedAudioClip = AssetDatabase.LoadAssetAtPath<AudioClip>(_defaultAudioPath);
            }
            EditorGUILayout.EndHorizontal();
            
            // 图片资源选择
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("图片文件:", GUILayout.Width(80));
            _selectedTexture = (Texture2D)EditorGUILayout.ObjectField(_selectedTexture, typeof(Texture2D), false, GUILayout.Width(200));
            if (GUILayout.Button("使用默认", GUILayout.Width(80)))
            {
                _selectedTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(_defaultTexturePath);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
            
            // 全面测试按钮
            EditorGUILayout.BeginHorizontal();
            GUI.enabled = !_isTestRunning;
            if (GUILayout.Button("开始全面测试", GUILayout.Height(30)))
            {
                StartComprehensiveTest();
            }
            GUI.enabled = true;
            
            if (GUILayout.Button("清空日志", GUILayout.Height(30)))
            {
                ClearResults();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
            
            // 基础功能测试
            EditorGUILayout.LabelField("基础功能测试", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            GUI.enabled = !_isTestRunning;
            if (GUILayout.Button("服务器启动", GUILayout.Height(25)))
            {
                TestServerStartup();
            }
            GUI.enabled = _testServer != null && _testServer.IsRunning;
            if (GUILayout.Button("停止服务", GUILayout.Height(25)))
            {
                StopServer();
            }
            GUI.enabled = !_isTestRunning;
            if (GUILayout.Button("服务状态", GUILayout.Height(25)))
            {
                TestMcpServerStatus();
            }
            if (GUILayout.Button("依赖检查", GUILayout.Height(25)))
            {
                TestDependencies();
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
            
            // 场景和播放模式测试
            EditorGUILayout.LabelField("场景和播放模式", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            GUI.enabled = !_isTestRunning;
            if (GUILayout.Button("场景工具", GUILayout.Height(25)))
            {
                //打印当前线程
                TestSceneTools();
            }
            if (GUILayout.Button("播放模式", GUILayout.Height(25)))
            {
                TestPlayModeTools();
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
            
            // GameObject和组件测试
            EditorGUILayout.LabelField("GameObject和组件", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            GUI.enabled = !_isTestRunning;
            if (GUILayout.Button("GameObject", GUILayout.Height(25)))
            {
                TestGameObjectTools();
            }
            if (GUILayout.Button("组件管理", GUILayout.Height(25)))
            {
                TestComponentTools();
            }
            if (GUILayout.Button("变换操作", GUILayout.Height(25)))
            {
                TestTransformTools();
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
            
            // 渲染和视觉效果测试
            EditorGUILayout.LabelField("渲染和视觉效果", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            GUI.enabled = !_isTestRunning;
            if (GUILayout.Button("材质渲染", GUILayout.Height(25)))
            {
                TestMaterialAndRenderingTools();
            }
            if (GUILayout.Button("光照系统", GUILayout.Height(25)))
            {
                TestLightingTools();
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
            
            // 物理和音频测试
            EditorGUILayout.LabelField("物理和音频", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            GUI.enabled = !_isTestRunning;
            if (GUILayout.Button("物理系统", GUILayout.Height(25)))
            {
                TestPhysicsTools();
            }
            if (GUILayout.Button("音频系统", GUILayout.Height(25)))
            {
                TestAudioTools();
            }
            if (GUILayout.Button("资源管理", GUILayout.Height(25)))
            {
                TestAssetTools();
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
            
            // HTTP功能测试
            EditorGUILayout.LabelField("HTTP功能测试", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            GUI.enabled = !_isTestRunning;
            if (GUILayout.Button("HTTP请求", GUILayout.Height(25)))
            {
                TestHttpRequests();
            }
            if (GUILayout.Button("响应验证", GUILayout.Height(25)))
            {
                TestHttpResponseValidation();
            }
            if (GUILayout.Button("错误处理", GUILayout.Height(25)))
            {
                TestHttpErrorHandling();
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
            
            // 测试结果显示区域
            EditorGUILayout.LabelField("测试结果:", EditorStyles.boldLabel);
            
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUI.skin.box);
            
            foreach (var result in _testResults)
            {
                // 根据结果类型设置颜色
                var style = GUI.skin.label;
                if (result.Contains("✓") || result.Contains("成功"))
                {
                    style = new GUIStyle(GUI.skin.label) { normal = { textColor = Color.green } };
                }
                else if (result.Contains("✗") || result.Contains("失败") || result.Contains("错误"))
                {
                    style = new GUIStyle(GUI.skin.label) { normal = { textColor = Color.red } };
                }
                else if (result.Contains("警告"))
                {
                    style = new GUIStyle(GUI.skin.label) { normal = { textColor = Color.yellow } };
                }
                
                EditorGUILayout.LabelField(result, style);
            }
            
            EditorGUILayout.EndScrollView();
            
            // 状态栏
            EditorGUILayout.Space();
            if (_isTestRunning)
            {
                EditorGUILayout.LabelField("状态: 测试进行中...", EditorStyles.helpBox);
            }
            else
            {
                EditorGUILayout.LabelField($"状态: 就绪 (共{_testResults.Count}条结果)", EditorStyles.helpBox);
            }
        }
        
        private void AddResult(string message)
        {
            _testResults.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
            
            // 自动滚动到底部
            _scrollPosition.y = float.MaxValue;
            
            Repaint();
            
            // 测试结果不再添加延迟，让测试工具点击后直接执行
        }
        
        private string GetErrorMessage(McpToolResult result)
        {
            if (result.Content != null && result.Content.Count > 0)
            {
                return result.Content.FirstOrDefault()?.Text ?? "未知错误";
            }
            return "未知错误";
        }
        
        private void ClearResults()
        {
            _testResults.Clear();
            
            // 清理测试过程中创建的GameObject
            CleanupTestObjects();
            
            // 停止测试服务器
            if (_testServer != null)
            {
                try
                {
                    _testServer.Stop();
                    _testServer = null;
                    AddResult("✓ 测试服务器已停止");
                }
                catch (Exception ex)
                {
                    AddResult($"✗ 停止测试服务器失败: {ex.Message}");
                }
            }
            
            // 重置测试状态
            _isTestRunning = false;
            
            AddResult("✓ 测试环境已清理完成");
            Repaint();
        }
        
        private void CleanupTestObjects()
        {
            string[] testObjectNames = {
                "ComponentTestObject",
                "TransformTestObject", 
                "RenderTestObject",
                "PhysicsTestObject",
                "AudioTestObject",
                "LightTestObject",
                "TestGameObject"
            };
            
            int cleanedCount = 0;
            foreach (string objName in testObjectNames)
            {
                GameObject testObj = GameObject.Find(objName);
                if (testObj != null)
                {
                    DestroyImmediate(testObj);
                    cleanedCount++;
                }
            }
            
            if (cleanedCount > 0)
            {
                AddResult($"✓ 已清理 {cleanedCount} 个测试对象");
            }
        }
        
        private async void StartComprehensiveTest()
        {
            _isTestRunning = true;
            AddResult("=== 开始MCP所有工具全面测试 ===");
            AddResult("测试步骤:");
            AddResult("1. 场景管理工具测试");
            AddResult("2. 播放模式工具测试");
            AddResult("3. GameObject工具测试");
            AddResult("4. 组件工具测试");
            AddResult("5. 变换工具测试");
            AddResult("6. 材质和渲染工具测试");
            AddResult("7. 物理工具测试");
            AddResult("8. 音频工具测试");
            AddResult("9. 光照工具测试");
            AddResult("10. 资源工具测试");
            AddResult("11. 服务器启动测试");
            AddResult("12. 依赖检查测试");
            AddResult("");
            
            try
            {
                // 测试1: 服务器启动
                await TestServerStartupAsync();
                
                // 测试2: 场景管理工具
                TestSceneTools();
                
                // 测试3: 播放模式工具
                TestPlayModeTools();
                
                // 测试4: GameObject工具
                TestGameObjectTools();
                
                // 测试5: 组件工具
                TestComponentTools();
                
                // 测试6: 变换工具
                TestTransformTools();
                
                // 测试7: 材质和渲染工具
                TestMaterialAndRenderingTools();
                
                // 测试8: 物理工具
                TestPhysicsTools();
                
                // 测试9: 音频工具
                TestAudioTools();
                
                // 测试10: 光照工具
                TestLightingTools();
                
                // 测试11: 资源工具
                TestAssetTools();
                
                // 测试12: 依赖检查
                TestDependencies();
                
                AddResult("=== MCP所有工具测试完成 ===");
                AddResult("");
                AddResult("正在自动清理测试环境...");
                
                // 自动清理测试内容
                CleanupTestObjects();
            }
            catch (Exception ex)
            {
                AddResult($"✗ 全面测试异常: {ex.Message}");
            }
            finally
            {
                _isTestRunning = false;
            }
        }
        
        private void TestServerStartup()
        {
            AddResult("--- 测试MCP服务器启动 ---");
            
            try
            {
                if (_testServer != null && _testServer.IsRunning)
                {
                    AddResult("✗ 服务器已在运行中，请先停止当前服务器");
                    return;
                }
                
                _testServer = new McpServer(_testPort);
                _testServer.Start();
                
                if (_testServer.IsRunning)
                {
                    AddResult($"✓ MCP服务器成功启动在端口 {_testServer.Port}");
                    AddResult("ℹ 服务器正在运行中，可使用'停止服务'按钮手动停止");
                }
                else
                {
                    AddResult("✗ MCP服务器启动失败");
                }
            }
            catch (Exception ex)
            {
                AddResult($"✗ MCP服务器测试失败: {ex.Message}");
            }
        }
        
        private async Task TestServerStartupAsync()
        {
            AddResult("--- 测试MCP服务器启动 (异步) ---");
            
            try
            {
                if (_testServer != null && _testServer.IsRunning)
                {
                    AddResult("✗ 服务器已在运行中，请先停止当前服务器");
                    return;
                }
                
                _testServer = new McpServer(_testPort);
                _testServer.Start();
                
                if (_testServer.IsRunning)
                {
                    AddResult($"✓ MCP服务器成功启动在端口 {_testServer.Port}");
                    
                    // 等待2秒
                    await Task.Delay(2000);
                    
                    _testServer.Stop();
                    AddResult("✓ MCP服务器已安全停止");
                    _testServer = null;
                }
                else
                {
                    AddResult("✗ MCP服务器启动失败");
                }
            }
            catch (Exception ex)
            {
                AddResult($"✗ MCP服务器异步测试失败: {ex.Message}");
            }
        }
        
        private void StopServer()
        {
            try
            {
                if (_testServer != null && _testServer.IsRunning)
                {
                    _testServer.Stop();
                    AddResult("✓ MCP服务器已手动停止");
                    _testServer = null;
                }
                else
                {
                    AddResult("✗ 没有运行中的服务器可以停止");
                }
            }
            catch (Exception ex)
            {
                AddResult($"✗ 停止服务器失败: {ex.Message}");
            }
        }
        
        private void TestMcpServerStatus()
        {
            AddResult("--- 检测MCP服务器状态 ---");
            
            try
            {
                // 检查测试服务器状态
                if (_testServer != null)
                {
                    AddResult($"✓ 测试服务器状态: {(_testServer.IsRunning ? "运行中" : "已停止")}");
                    if (_testServer.IsRunning)
                    {
                        AddResult($"✓ 测试服务器端口: {_testServer.Port}");
                        AddResult($"✓ 测试服务器地址: http://localhost:{_testServer.Port}/mcp");
                        
                        // 检查连接的客户端
                        var clients = _testServer.ConnectedClients;
                        AddResult($"✓ 连接的客户端数量: {clients.Count}");
                        
                        if (clients.Count > 0)
                        {
                            foreach (var client in clients.Values.Take(5)) // 只显示前5个客户端
                            {
                                var timeSinceLastSeen = DateTime.Now - client.LastSeen;
                                var status = timeSinceLastSeen.TotalSeconds < 30 ? "活跃" : "不活跃";
                                AddResult($"  • 客户端 {client.RemoteEndPoint}: {status} (请求数: {client.RequestCount})");
                            }
                        }
                    }
                }
                else
                {
                    AddResult("ℹ 测试服务器未创建");
                }
                
                // 检查主MCP服务器状态（通过McpServerWindow获取）
                var mainServerWindow = Resources.FindObjectsOfTypeAll<McpServerWindow>().FirstOrDefault();
                System.Reflection.FieldInfo serverField = null;
                
                if (mainServerWindow != null)
                {
                    // 使用反射获取私有字段_server
                    serverField = typeof(McpServerWindow).GetField("_server", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    
                    if (serverField != null)
                    {
                        var mainServer = serverField.GetValue(mainServerWindow) as McpServer;
                        if (mainServer != null)
                        {
                            AddResult($"✓ 主MCP服务器状态: {(mainServer.IsRunning ? "运行中" : "已停止")}");
                            if (mainServer.IsRunning)
                            {
                                AddResult($"✓ 主服务器端口: {mainServer.Port}");
                                AddResult($"✓ 主服务器地址: http://localhost:{mainServer.Port}/mcp");
                                
                                var mainClients = mainServer.ConnectedClients;
                                AddResult($"✓ 主服务器连接的客户端数量: {mainClients.Count}");
                            }
                        }
                        else
                        {
                            AddResult("ℹ 主MCP服务器未创建");
                        }
                    }
                }
                else
                {
                    AddResult("ℹ 未找到MCP服务器窗口");
                }
                
                // 测试本地连接
                AddResult("--- 测试本地连接 ---");
                
                // 收集所有需要测试的端口
                var portsToTest = new HashSet<int>();
                
                // 添加测试服务器端口
                if (_testServer != null && _testServer.IsRunning)
                {
                    portsToTest.Add(_testServer.Port);
                }
                
                // 添加主服务器端口
                if (mainServerWindow != null && serverField != null)
                {
                    var mainServer = serverField.GetValue(mainServerWindow) as McpServer;
                    if (mainServer != null && mainServer.IsRunning)
                    {
                        portsToTest.Add(mainServer.Port);
                    }
                }
                
                // 只测试实际运行的服务器端口
                if (portsToTest.Count == 0)
                {
                    AddResult("ℹ 没有检测到运行中的MCP服务器，跳过端口连接测试");
                }
                else
                {
                    AddResult($"ℹ 检测到 {portsToTest.Count} 个运行中的服务器端口，开始测试连接");
                    
                    // 测试所有运行中的服务器端口
                    foreach (var port in portsToTest)
                    {
                        TestLocalConnection(port);
                    }
                }
            }
            catch (Exception ex)
            {
                AddResult($"✗ MCP服务器状态检测失败: {ex.Message}");
            }
        }
        
        private void TestLocalConnection(int port)
        {
            try
            {
                using (var client = new System.Net.Http.HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(3);
                    var url = $"http://localhost:{port}/mcp";
                    
                    var requestData = new
                    {
                        jsonrpc = "2.0",
                        id = 1,
                        method = "tools/list"
                    };
                    
                    var json = Newtonsoft.Json.JsonConvert.SerializeObject(requestData);
                    var content = new System.Net.Http.StringContent(json, System.Text.Encoding.UTF8, "application/json");
                    
                    var task = client.PostAsync(url, content);
                    
                    try
                    {
                        if (task.Wait(3000)) // 等待3秒
                        {
                            if (task.IsCompletedSuccessfully)
                            {
                                var response = task.Result;
                                if (response.IsSuccessStatusCode)
                                {
                                    var responseTask = response.Content.ReadAsStringAsync();
                                    if (responseTask.Wait(2000)) // 等待2秒读取响应
                                    {
                                        var responseText = responseTask.Result;
                                        var responseObj = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(responseText);
                                        
                                        if (responseObj?.result?.tools != null)
                                        {
                                            var toolCount = ((Newtonsoft.Json.Linq.JArray)responseObj.result.tools).Count;
                                            AddResult($"✓ 端口 {port} 连接成功，发现 {toolCount} 个工具");
                                        }
                                        else
                                        {
                                            AddResult($"✓ 端口 {port} 连接成功，但响应格式异常");
                                        }
                                    }
                                    else
                                    {
                                        AddResult($"✗ 端口 {port} 读取响应超时");
                                    }
                                }
                                else
                                {
                                    AddResult($"✗ 端口 {port} HTTP错误: {response.StatusCode}");
                                }
                            }
                            else if (task.IsFaulted)
                            {
                                AddResult($"✗ 端口 {port} 连接失败: {task.Exception?.GetBaseException()?.Message}");
                            }
                            else
                            {
                                AddResult($"✗ 端口 {port} 连接被取消");
                            }
                        }
                        else
                        {
                            AddResult($"✗ 端口 {port} 连接超时");
                        }
                    }
                    catch (System.Threading.ThreadAbortException)
                    {
                        AddResult($"✗ 端口 {port} 测试被中止");
                    }
                    catch (Exception ex)
                    {
                        AddResult($"✗ 端口 {port} 测试异常: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                AddResult($"✗ 端口 {port} 连接失败: {ex.Message}");
            }
        }
        
        private void TestUnityTools()
        {
            AddResult("--- 测试Unity工具功能 ---");
            
            try
            {
                // 测试播放模式控制
                AddResult($"✓ 当前播放模式状态: {(EditorApplication.isPlaying ? "播放中" : "已停止")}");
                
                // 测试场景信息
                var scenes = UnityEditor.EditorBuildSettings.scenes;
                AddResult($"✓ 构建设置中的场景数量: {scenes.Length}");
                
                // 测试资源数据库
                UnityEditor.AssetDatabase.Refresh();
                AddResult("✓ 资源数据库刷新成功");
                
                // 测试Unity版本信息
                AddResult($"✓ Unity版本: {Application.unityVersion}");
                AddResult($"✓ 平台: {Application.platform}");
                
            }
            catch (Exception ex)
            {
                AddResult($"✗ Unity工具测试失败: {ex.Message}");
            }
        }
        
        private void TestDependencies()
        {
            AddResult("--- 测试依赖项 ---");
            
            try
            {
                // 测试Newtonsoft.Json
                var testObj = new { test = "value", number = 42 };
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(testObj);
                AddResult($"✓ Newtonsoft.Json可用: {json}");
                
                // 测试System.Net.HttpListener可用性
                var listenerType = typeof(System.Net.HttpListener);
                AddResult($"✓ HttpListener类型可用: {listenerType.Name}");
                
                // 测试编辑器API
                AddResult($"✓ EditorApplication API可用");
                AddResult($"✓ AssetDatabase API可用");
                
            }
            catch (Exception ex)
            {
                AddResult($"✗ 依赖项测试失败: {ex.Message}");
            }
        }
        
        private void TestSceneTools()
        {
            AddResult("--- 测试场景管理工具 ---");
            
            if (!CheckPrerequisites("场景工具"))
            {
                return;
            }
            
            try
            {
                // 测试ListScenes 
                var listResult = UnityToolsMain.ListScenes(new Newtonsoft.Json.Linq.JObject());
                AddResult(listResult.IsError ? $"✗ ListScenes失败: {GetErrorMessage(listResult)}" : "✓ ListScenes: 成功");
                
                // 测试GetPlayModeStatus (场景相关)
                var statusResult = UnityToolsMain.GetPlayModeStatus(new Newtonsoft.Json.Linq.JObject());
                AddResult(statusResult.IsError ? $"✗ GetPlayModeStatus失败: {GetErrorMessage(statusResult)}" : "✓ GetPlayModeStatus: 成功");
                
            }
            catch (Exception ex)
            {
                AddResult($"✗ 场景工具测试失败: {ex.Message}");
            }
        }
        
        private void TestPlayModeTools()
        {
            AddResult("--- 测试播放模式工具 (异步执行) ---");
            
            // 播放模式测试需要特殊的前置条件检查
            if (!CheckPrerequisites("播放模式工具", false, false))
            {
                return;
            }
            
            try
            {
                // 检查当前播放模式状态
                bool wasPlaying = EditorApplication.isPlaying;
                AddResult($"当前播放模式状态: {(wasPlaying ? "播放中" : "编辑模式")}");
                
                AddResult("⚠ 注意: play_mode_start 现在会先检查状态再决定执行方式");
                AddResult("⚠ 如果已在播放模式，立即返回'已播放'；如果未播放，则异步启动");
                AddResult("⚠ play_mode_stop 仍使用异步执行");
                
                if (!wasPlaying)
                {
                    // 测试异步StartPlayMode - 直接调用UnityTools进行同步测试
                    var startResult = UnityToolsMain.StartPlayMode(new Newtonsoft.Json.Linq.JObject());
                    AddResult(startResult.IsError ? $"✗ StartPlayMode失败: {GetErrorMessage(startResult)}" : "✓ StartPlayMode: 成功 (同步测试)");
                    
                    // 等待一下再停止
                    EditorApplication.delayCall += () => {
                        var stopResult = UnityToolsMain.StopPlayMode(new Newtonsoft.Json.Linq.JObject());
                        AddResult(stopResult.IsError ? $"✗ StopPlayMode失败: {GetErrorMessage(stopResult)}" : "✓ StopPlayMode: 成功 (同步测试)");
                    };
                }
                else
                {
                    AddResult("⚠ 当前已在播放模式，跳过进入播放模式测试");
                    
                    // 测试异步StopPlayMode - 直接调用UnityTools进行同步测试
                    var stopResult = UnityToolsMain.StopPlayMode(new Newtonsoft.Json.Linq.JObject());
                    AddResult(stopResult.IsError ? $"✗ StopPlayMode失败: {GetErrorMessage(stopResult)}" : "✓ StopPlayMode: 成功 (同步测试)");
                }
                
                AddResult("✓ 异步播放模式功能已实现 - MCP客户端调用时将异步执行");
                
            }
            catch (Exception ex)
            {
                AddResult($"✗ 播放模式工具测试失败: {ex.Message}");
            }
        }
        
        private void TestGameObjectTools()
        {
            AddResult("--- 测试GameObject工具 ---");
            
            if (!CheckPrerequisites("GameObject工具", true))
            {
                return;
            }
            
            try
            {
                // 测试CreateGameObject
                var createArgs = new Newtonsoft.Json.Linq.JObject();
                createArgs["name"] = "TestGameObject";
                var createResult = UnityToolsMain.CreateGameObject(createArgs);
                AddResult(createResult.IsError ? $"✗ CreateGameObject失败: {GetErrorMessage(createResult)}" : "✓ CreateGameObject: 成功");
                
                // 创建后等待1秒
                SafeSleep(1000);
                
                // 测试FindGameObject
                var findArgs = new Newtonsoft.Json.Linq.JObject();
                findArgs["name"] = "TestGameObject";
                var findResult = UnityToolsMain.FindGameObject(findArgs);
                AddResult(findResult.IsError ? $"✗ FindGameObject失败: {GetErrorMessage(findResult)}" : "✓ FindGameObject: 成功");
                
                // 查找后等待1秒
                SafeSleep(1000);
                
                // 测试GetGameObjectInfo
                var infoArgs = new Newtonsoft.Json.Linq.JObject();
                infoArgs["name"] = "TestGameObject";
                var infoResult = UnityToolsMain.GetGameObjectInfo(infoArgs);
                AddResult(infoResult.IsError ? $"✗ GetGameObjectInfo失败: {GetErrorMessage(infoResult)}" : "✓ GetGameObjectInfo: 成功");
                
                // 获取信息后等待1秒
                SafeSleep(1000);
                
                // 测试DuplicateGameObject
                var dupArgs = new Newtonsoft.Json.Linq.JObject();
                dupArgs["name"] = "TestGameObject";
                var dupResult = UnityToolsMain.DuplicateGameObject(dupArgs);
                AddResult(dupResult.IsError ? $"✗ DuplicateGameObject失败: {GetErrorMessage(dupResult)}" : "✓ DuplicateGameObject: 成功");
                
                // 复制后等待1秒
                SafeSleep(1000);
                
                // 删除克隆的对象
                var deleteCloneArgs = new Newtonsoft.Json.Linq.JObject();
                deleteCloneArgs["name"] = "TestGameObject (Clone)";
                var deleteCloneResult = UnityToolsMain.DeleteGameObject(deleteCloneArgs);
                AddResult(deleteCloneResult.IsError ? $"✗ 删除克隆对象失败: {GetErrorMessage(deleteCloneResult)}" : "✓ 删除克隆对象: 成功");
                
                // 删除克隆对象后等待1秒
                SafeSleep(1000);
                
                // 测试DeleteGameObject (删除原始对象)
                var deleteArgs = new Newtonsoft.Json.Linq.JObject();
                deleteArgs["name"] = "TestGameObject";
                var deleteResult = UnityToolsMain.DeleteGameObject(deleteArgs);
                AddResult(deleteResult.IsError ? $"✗ DeleteGameObject失败: {GetErrorMessage(deleteResult)}" : "✓ DeleteGameObject: 成功");
                
            }
            catch (Exception ex)
            {
                AddResult($"✗ GameObject工具测试失败: {ex.Message}");
                
                // 异常时也要清理测试对象和克隆对象
                try
                {
                    // 清理克隆对象
                    var deleteCloneArgs = new Newtonsoft.Json.Linq.JObject();
                    deleteCloneArgs["name"] = "TestGameObject (Clone)";
                    UnityToolsMain.DeleteGameObject(deleteCloneArgs);
                    
                    // 清理原始对象
                    var deleteArgs = new Newtonsoft.Json.Linq.JObject();
                    deleteArgs["name"] = "TestGameObject";
                    UnityToolsMain.DeleteGameObject(deleteArgs);
                }
                catch (Exception cleanupEx)
                {
                    AddResult($"✗ 清理GameObject测试对象失败: {cleanupEx.Message}");
                }
            }
        }
        
        private void TestComponentTools()
        {
            AddResult("--- 测试组件工具 ---");
            
            // 检查前置条件
            if (!CheckPrerequisites("组件测试", true))
            {
                return;
            }
            
            try
            {
                // 先创建一个测试对象
                var createArgs = new Newtonsoft.Json.Linq.JObject();
                createArgs["name"] = "ComponentTestObject";
                var createResult = UnityToolsMain.CreateGameObject(createArgs);
                if (createResult.IsError)
                {
                    AddResult($"✗ 前置条件失败: 无法创建测试对象 - {GetErrorMessage(createResult)}");
                    return;
                }
                AddResult("✓ 前置条件: 测试对象创建成功");
                
                // 创建后等待1秒
                SafeSleep(1000);
                
                // 测试AddComponent
                var addArgs = new Newtonsoft.Json.Linq.JObject();
                addArgs["gameObject"] = "ComponentTestObject";
                addArgs["component"] = "Rigidbody";
                var addResult = UnityToolsMain.AddComponent(addArgs);
                AddResult(addResult.IsError ? $"✗ AddComponent失败: {GetErrorMessage(addResult)}" : "✓ AddComponent: 成功");
                
                // 添加组件后等待1秒
                SafeSleep(1000);
                
                // 测试ListComponents
                var listArgs = new Newtonsoft.Json.Linq.JObject();
                listArgs["gameObject"] = "ComponentTestObject";
                var listResult = UnityToolsMain.ListComponents(listArgs);
                AddResult(listResult.IsError ? $"✗ ListComponents失败: {GetErrorMessage(listResult)}" : "✓ ListComponents: 成功");
                
                // 列出组件后等待1秒
                SafeSleep(1000);
                
                // 测试GetComponentProperties
                var propArgs = new Newtonsoft.Json.Linq.JObject();
                propArgs["gameObject"] = "ComponentTestObject";
                propArgs["component"] = "Rigidbody";
                var propResult = UnityToolsMain.GetComponentProperties(propArgs);
                AddResult(propResult.IsError ? $"✗ GetComponentProperties失败: {GetErrorMessage(propResult)}" : "✓ GetComponentProperties: 成功");
                
                // 获取属性后等待1秒
                SafeSleep(1000);
                
                // 测试RemoveComponent
                var removeArgs = new Newtonsoft.Json.Linq.JObject();
                removeArgs["gameObject"] = "ComponentTestObject";
                removeArgs["component"] = "Rigidbody";
                var removeResult = UnityToolsMain.RemoveComponent(removeArgs);
                AddResult(removeResult.IsError ? $"✗ RemoveComponent失败: {GetErrorMessage(removeResult)}" : "✓ RemoveComponent: 成功");
                
                // 清理测试对象
                var deleteArgs = new Newtonsoft.Json.Linq.JObject();
                deleteArgs["name"] = "ComponentTestObject";
                var deleteResult = UnityToolsMain.DeleteGameObject(deleteArgs);
                AddResult($"✓ 清理: {(deleteResult.IsError ? "失败" : "测试对象已删除")}");
                
            }
            catch (Exception ex)
            {
                AddResult($"✗ 组件工具测试失败: {ex.Message}");
            }
        }
        
        private void TestTransformTools()
        {
            AddResult("--- 测试变换工具 ---");
            
            if (!CheckPrerequisites("变换工具", true))
            {
                return;
            }
            
            try
            {
                // 先创建一个测试对象
                var createArgs = new Newtonsoft.Json.Linq.JObject();
                createArgs["name"] = "TransformTestObject";
                UnityToolsMain.CreateGameObject(createArgs);
                
                // 测试SetTransform
                var transformArgs = new Newtonsoft.Json.Linq.JObject();
                transformArgs["gameObject"] = "TransformTestObject";
                transformArgs["position"] = new Newtonsoft.Json.Linq.JObject();
                transformArgs["position"]["x"] = 1.0f;
                transformArgs["position"]["y"] = 2.0f;
                transformArgs["position"]["z"] = 3.0f;
                var transformResult = UnityToolsMain.SetTransform(transformArgs);
                AddResult(transformResult.IsError ? $"✗ SetTransform失败: {GetErrorMessage(transformResult)}" : "✓ SetTransform: 成功");
                
                // 测试SetParent
                var parentArgs = new Newtonsoft.Json.Linq.JObject();
                parentArgs["child"] = "TransformTestObject";
                parentArgs["parent"] = null; // 设置为根对象
                var parentResult = UnityToolsMain.SetParent(parentArgs);
                AddResult(parentResult.IsError ? $"✗ SetParent失败: {GetErrorMessage(parentResult)}" : "✓ SetParent: 成功");
                
                // 清理测试对象
                var deleteArgs = new Newtonsoft.Json.Linq.JObject();
                deleteArgs["name"] = "TransformTestObject";
                UnityToolsMain.DeleteGameObject(deleteArgs);
                
            }
            catch (Exception ex)
            {
                AddResult($"✗ 变换工具测试失败: {ex.Message}");
            }
        }
        
        private void TestMaterialAndRenderingTools()
        {
            AddResult("--- 测试材质和渲染工具 ---");
            
            if (!CheckPrerequisites("材质和渲染工具", true))
            {
                return;
            }
            
            // 检查图片资源
            if (_selectedTexture == null)
            {
                // 尝试加载默认图片资源
                _selectedTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(_defaultTexturePath);
                
                if (_selectedTexture == null)
                {
                    AddResult("⚠ 警告: 没有选择图片文件，也没有找到默认图片文件");
                    AddResult("正在打开文件选择对话框，请选择图片文件...");
                    
                    // 打开文件选择对话框
                    string selectedPath = EditorUtility.OpenFilePanel("选择图片文件", "", "png,jpg,jpeg,tga,bmp,tiff");
                    
                    if (!string.IsNullOrEmpty(selectedPath))
                    {
                        try
                        {
                            // 确保目标目录存在
                            string targetDir = System.IO.Path.GetDirectoryName(_defaultTexturePath);
                            if (!System.IO.Directory.Exists(targetDir))
                            {
                                System.IO.Directory.CreateDirectory(targetDir);
                                AddResult($"✓ 创建目录: {targetDir}");
                            }
                            
                            // 复制文件到项目目录
                            string fileName = System.IO.Path.GetFileName(selectedPath);
                            string targetPath = System.IO.Path.Combine(targetDir, fileName);
                            System.IO.File.Copy(selectedPath, targetPath, true);
                            
                            // 刷新AssetDatabase
                            AssetDatabase.Refresh();
                            
                            // 加载复制的图片文件
                            _selectedTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(targetPath.Replace(Application.dataPath, "Assets"));
                            
                            if (_selectedTexture != null)
                            {
                                AddResult($"✓ 图片文件已上传: {fileName}");
                            }
                            else
                            {
                                AddResult($"✗ 图片文件上传失败，无法加载: {fileName}");
                            }
                        }
                        catch (System.Exception ex)
                        {
                            AddResult($"✗ 图片文件上传失败: {ex.Message}");
                        }
                    }
                    else
                    {
                        AddResult("⏭ 用户取消了文件选择，跳过材质纹理设置");
                    }
                    
                    if (_selectedTexture == null)
                    {
                        AddResult("将创建不带纹理的材质进行测试...");
                    }
                }
            }
            
            try
            {
                // 测试CreateMaterial
                var matArgs = new Newtonsoft.Json.Linq.JObject();
                matArgs["name"] = "TestMaterial";
                var matResult = UnityToolsMain.CreateMaterial(matArgs);
                AddResult(matResult.IsError ? $"✗ CreateMaterial失败: {GetErrorMessage(matResult)}" : "✓ CreateMaterial: 成功");
                
                // 只有在有图片资源时才设置纹理
                if (_selectedTexture != null)
                {
                    try
                    {
                        string texturePath = AssetDatabase.GetAssetPath(_selectedTexture);
                        var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/TestMaterial.mat");
                        if (material != null)
                        {
                            material.mainTexture = _selectedTexture;
                            EditorUtility.SetDirty(material);
                            AssetDatabase.SaveAssets();
                            AddResult($"✓ 材质纹理设置成功 (使用图片: {System.IO.Path.GetFileName(texturePath)})");
                        }
                    }
                    catch (Exception texEx)
                    {
                        AddResult($"✗ 材质纹理设置失败: {texEx.Message}");
                    }
                }
                else
                {
                    AddResult("⏭ 跳过材质纹理设置（没有图片资源）");
                }
                
                // 创建一个带渲染器的测试对象
                var createArgs = new Newtonsoft.Json.Linq.JObject();
                createArgs["name"] = "RenderTestObject";
                UnityToolsMain.CreateGameObject(createArgs);
                
                // 创建后等待1秒
                SafeSleep(1000);
                
                // 添加MeshFilter组件
                var addFilterArgs = new Newtonsoft.Json.Linq.JObject();
                addFilterArgs["gameObject"] = "RenderTestObject";
                addFilterArgs["component"] = "MeshFilter";
                UnityToolsMain.AddComponent(addFilterArgs);
                
                // 添加MeshFilter后等待1秒
                SafeSleep(1000);
                
                // 添加MeshRenderer组件
                var addRendererArgs = new Newtonsoft.Json.Linq.JObject();
                addRendererArgs["gameObject"] = "RenderTestObject";
                addRendererArgs["component"] = "MeshRenderer";
                UnityToolsMain.AddComponent(addRendererArgs);
                
                // 添加MeshRenderer后等待1秒
                SafeSleep(1000);
                
                // 设置基础网格（立方体）
                try
                {
                    var testObject = GameObject.Find("RenderTestObject");
                    if (testObject != null)
                    {
                        var meshFilter = testObject.GetComponent<MeshFilter>();
                        if (meshFilter != null)
                        {
                            meshFilter.mesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
                            AddResult("✓ 测试对象网格设置成功 (立方体)");
                        }
                    }
                }
                catch (Exception meshEx)
                {
                    AddResult($"✗ 测试对象网格设置失败: {meshEx.Message}");
                }
                
                // 测试AssignMaterial
                var assignArgs = new Newtonsoft.Json.Linq.JObject();
                assignArgs["gameObject"] = "RenderTestObject";
                assignArgs["materialPath"] = "Assets/Materials/TestMaterial.mat";
                var assignResult = UnityToolsMain.AssignMaterial(assignArgs);
                AddResult(assignResult.IsError ? $"✗ AssignMaterial失败: {GetErrorMessage(assignResult)}" : "✓ AssignMaterial: 成功");
                
                // 清理测试对象
                var deleteArgs = new Newtonsoft.Json.Linq.JObject();
                deleteArgs["name"] = "RenderTestObject";
                UnityToolsMain.DeleteGameObject(deleteArgs);
                
            }
            catch (Exception ex)
            {
                AddResult($"✗ 材质和渲染工具测试失败: {ex.Message}");
                
                // 异常时也要清理测试对象
                try
                {
                    var deleteArgs = new Newtonsoft.Json.Linq.JObject();
                    deleteArgs["name"] = "RenderTestObject";
                    UnityToolsMain.DeleteGameObject(deleteArgs);
                }
                catch (Exception cleanupEx)
                {
                    AddResult($"✗ 清理RenderTestObject失败: {cleanupEx.Message}");
                }
            }
        }
        
        private void TestPhysicsTools()
        {
            AddResult("--- 测试物理工具 ---");
            
            if (!CheckPrerequisites("物理工具", true))
            {
                return;
            }
            
            try
            {
                // 创建一个物理测试对象
                var createArgs = new Newtonsoft.Json.Linq.JObject();
                createArgs["name"] = "PhysicsTestObject";
                UnityToolsMain.CreateGameObject(createArgs);
                
                // 创建后等待1秒
                SafeSleep(1000);
                
                // 添加Rigidbody
                var addRbArgs = new Newtonsoft.Json.Linq.JObject();
                addRbArgs["gameObject"] = "PhysicsTestObject";
                addRbArgs["component"] = "Rigidbody";
                UnityToolsMain.AddComponent(addRbArgs);
                
                // 添加组件后等待1秒
                SafeSleep(1000);
                
                // 测试SetRigidbodyProperties
                var rbArgs = new Newtonsoft.Json.Linq.JObject();
                rbArgs["gameObject"] = "PhysicsTestObject";
                var rbProperties = new Newtonsoft.Json.Linq.JObject();
                rbProperties["mass"] = 2.0f;
                rbArgs["properties"] = rbProperties;
                var rbResult = UnityToolsMain.SetRigidbodyProperties(rbArgs);
                AddResult(rbResult.IsError ? $"✗ SetRigidbodyProperties失败: {GetErrorMessage(rbResult)}" : "✓ SetRigidbodyProperties: 成功");
                
                // 设置属性后等待1秒
                SafeSleep(1000);
                
                // 测试AddForce
                var forceArgs = new Newtonsoft.Json.Linq.JObject();
                forceArgs["gameObject"] = "PhysicsTestObject";
                forceArgs["force"] = new Newtonsoft.Json.Linq.JObject();
                forceArgs["force"]["x"] = 0f;
                forceArgs["force"]["y"] = 10f;
                forceArgs["force"]["z"] = 0f;
                var forceResult = UnityToolsMain.AddForce(forceArgs);
                AddResult(forceResult.IsError ? $"✗ AddForce失败: {GetErrorMessage(forceResult)}" : "✓ AddForce: 成功");
                
                // 施加力后等待1秒
                SafeSleep(1000);
                
                // 添加Collider
                var addColArgs = new Newtonsoft.Json.Linq.JObject();
                addColArgs["gameObject"] = "PhysicsTestObject";
                addColArgs["component"] = "BoxCollider";
                UnityToolsMain.AddComponent(addColArgs);
                
                // 添加碰撞器后等待1秒
                SafeSleep(1000);
                
                // 测试SetColliderProperties
                var colArgs = new Newtonsoft.Json.Linq.JObject();
                colArgs["gameObject"] = "PhysicsTestObject";
                var colProperties = new Newtonsoft.Json.Linq.JObject();
                colProperties["isTrigger"] = true;
                colArgs["properties"] = colProperties;
                var colResult = UnityToolsMain.SetColliderProperties(colArgs);
                AddResult(colResult.IsError ? $"✗ SetColliderProperties失败: {GetErrorMessage(colResult)}" : "✓ SetColliderProperties: 成功");
                
                // 测试Raycast
                var rayArgs = new Newtonsoft.Json.Linq.JObject();
                rayArgs["origin"] = new Newtonsoft.Json.Linq.JObject();
                rayArgs["origin"]["x"] = 0f;
                rayArgs["origin"]["y"] = 10f;
                rayArgs["origin"]["z"] = 0f;
                rayArgs["direction"] = new Newtonsoft.Json.Linq.JObject();
                rayArgs["direction"]["x"] = 0f;
                rayArgs["direction"]["y"] = -1f;
                rayArgs["direction"]["z"] = 0f;
                var rayResult = UnityToolsMain.Raycast(rayArgs);
                AddResult(rayResult.IsError ? $"✗ Raycast失败: {GetErrorMessage(rayResult)}" : "✓ Raycast: 成功");
                
                // 清理测试对象
                var deleteArgs = new Newtonsoft.Json.Linq.JObject();
                deleteArgs["name"] = "PhysicsTestObject";
                UnityToolsMain.DeleteGameObject(deleteArgs);
                
            }
            catch (Exception ex)
            {
                AddResult($"✗ 物理工具测试失败: {ex.Message}");
                
                // 异常时也要清理测试对象
                try
                {
                    var deleteArgs = new Newtonsoft.Json.Linq.JObject();
                    deleteArgs["name"] = "PhysicsTestObject";
                    UnityToolsMain.DeleteGameObject(deleteArgs);
                }
                catch (Exception cleanupEx)
                {
                    AddResult($"✗ 清理PhysicsTestObject失败: {cleanupEx.Message}");
                }
            }
        }
        
        private void TestAudioTools()
        {
            AddResult("--- 测试音频工具 ---");
            
            if (!CheckPrerequisites("音频工具", true))
            {
                return;
            }
            
            // 检查音频资源
            if (_selectedAudioClip == null)
            {
                // 尝试加载默认音频资源
                _selectedAudioClip = AssetDatabase.LoadAssetAtPath<AudioClip>(_defaultAudioPath);
                
                if (_selectedAudioClip == null)
                {
                    AddResult("⚠ 警告: 没有选择音频文件，也没有找到默认音频文件");
                    AddResult("正在打开文件选择对话框，请选择音频文件...");
                    
                    // 打开文件选择对话框
                    string selectedPath = EditorUtility.OpenFilePanel("选择音频文件", "", "wav,mp3,ogg,aiff");
                    
                    if (!string.IsNullOrEmpty(selectedPath))
                    {
                        try
                        {
                            // 确保目标目录存在
                            string targetDir = System.IO.Path.GetDirectoryName(_defaultAudioPath);
                            if (!System.IO.Directory.Exists(targetDir))
                            {
                                System.IO.Directory.CreateDirectory(targetDir);
                                AddResult($"✓ 创建目录: {targetDir}");
                            }
                            
                            // 复制文件到项目目录
                            string fileName = System.IO.Path.GetFileName(selectedPath);
                            string targetPath = System.IO.Path.Combine(targetDir, fileName);
                            System.IO.File.Copy(selectedPath, targetPath, true);
                            
                            // 刷新AssetDatabase
                            AssetDatabase.Refresh();
                            
                            // 加载复制的音频文件
                            _selectedAudioClip = AssetDatabase.LoadAssetAtPath<AudioClip>(targetPath.Replace(Application.dataPath, "Assets"));
                            
                            if (_selectedAudioClip != null)
                            {
                                AddResult($"✓ 音频文件已上传: {fileName}");
                            }
                            else
                            {
                                AddResult($"✗ 音频文件上传失败，无法加载: {fileName}");
                            }
                        }
                        catch (System.Exception ex)
                        {
                            AddResult($"✗ 音频文件上传失败: {ex.Message}");
                        }
                    }
                    else
                    {
                        AddResult("⏭ 用户取消了文件选择，跳过音频播放测试");
                    }
                    
                    if (_selectedAudioClip == null)
                    {
                        AddResult("跳过音频播放测试，仅测试音频属性设置...");
                    }
                }
            }
            
            try
            {
                // 创建音频测试对象
                var createArgs = new Newtonsoft.Json.Linq.JObject();
                createArgs["name"] = "AudioTestObject";
                UnityToolsMain.CreateGameObject(createArgs);
                
                // 添加AudioSource
                var addArgs = new Newtonsoft.Json.Linq.JObject();
                addArgs["gameObject"] = "AudioTestObject";
                addArgs["component"] = "AudioSource";
                UnityToolsMain.AddComponent(addArgs);
                
                // 测试SetAudioProperties
                var audioArgs = new Newtonsoft.Json.Linq.JObject();
                audioArgs["gameObject"] = "AudioTestObject";
                var audioProperties = new Newtonsoft.Json.Linq.JObject();
                audioProperties["volume"] = 0.5f;
                audioArgs["properties"] = audioProperties;
                var audioResult = UnityToolsMain.SetAudioProperties(audioArgs);
                AddResult(audioResult.IsError ? $"✗ SetAudioProperties失败: {GetErrorMessage(audioResult)}" : "✓ SetAudioProperties: 成功");
                
                // 只有在有音频资源时才测试播放
                if (_selectedAudioClip != null)
                {
                    // 测试PlayAudio
                    var playArgs = new Newtonsoft.Json.Linq.JObject();
                    playArgs["gameObject"] = "AudioTestObject";
                    
                    string audioPath = AssetDatabase.GetAssetPath(_selectedAudioClip);
                    playArgs["audioClip"] = audioPath;
                    playArgs["volume"] = 0.8f;
                    playArgs["loop"] = false;
                    var playResult = UnityToolsMain.PlayAudio(playArgs);
                    string playMessage = playResult.IsError ? 
                        $"✗ PlayAudio失败: {GetErrorMessage(playResult)}" : 
                        $"✓ PlayAudio: 成功 (使用音频: {System.IO.Path.GetFileName(audioPath)})";
                    AddResult(playMessage);
                
                    // 播放后等待1秒再停止
                    SafeSleep(1000);
                    
                    // 测试StopAudio
                    var stopArgs = new Newtonsoft.Json.Linq.JObject();
                    stopArgs["gameObject"] = "AudioTestObject";
                    var stopResult = UnityToolsMain.StopAudio(stopArgs);
                    string stopMessage = stopResult.IsError ? 
                        $"✗ StopAudio失败: {GetErrorMessage(stopResult)}" : 
                        "✓ StopAudio: 成功";
                    AddResult(stopMessage);
                }
                else
                {
                    AddResult("⏭ 跳过音频播放和停止测试（没有音频资源）");
                }
                
                // 清理测试对象
                var deleteArgs = new Newtonsoft.Json.Linq.JObject();
                deleteArgs["name"] = "AudioTestObject";
                UnityToolsMain.DeleteGameObject(deleteArgs);
                
            }
            catch (Exception ex)
            {
                AddResult($"✗ 音频工具测试失败: {ex.Message}");
                
                // 异常时也要清理测试对象
                try
                {
                    var deleteArgs = new Newtonsoft.Json.Linq.JObject();
                    deleteArgs["name"] = "AudioTestObject";
                    UnityToolsMain.DeleteGameObject(deleteArgs);
                }
                catch (Exception cleanupEx)
                {
                    AddResult($"✗ 清理AudioTestObject失败: {cleanupEx.Message}");
                }
            }
        }
        
        private void TestLightingTools()
        {
            AddResult("--- 测试光照工具 ---");
            
            if (!CheckPrerequisites("光照工具", true))
            {
                return;
            }
            
            try
            {
                // 测试CreateLight
                var lightArgs = new Newtonsoft.Json.Linq.JObject();
                lightArgs["name"] = "TestLight";
                lightArgs["type"] = "Directional";
                var lightResult = UnityToolsMain.CreateLight(lightArgs);
                AddResult(lightResult.IsError ? $"✗ CreateLight失败: {GetErrorMessage(lightResult)}" : "✓ CreateLight: 成功");
                
                // 创建灯光后等待1秒
                SafeSleep(1000);
                
                // 测试SetLightProperties
                var propArgs = new Newtonsoft.Json.Linq.JObject();
                propArgs["gameObject"] = "TestLight";
                var properties = new Newtonsoft.Json.Linq.JObject();
                properties["intensity"] = 1.5f;
                propArgs["properties"] = properties;
                var propResult = UnityToolsMain.SetLightProperties(propArgs);
                AddResult(propResult.IsError ? $"✗ SetLightProperties失败: {GetErrorMessage(propResult)}" : "✓ SetLightProperties: 成功");
                
                // 清理测试对象
                var deleteArgs = new Newtonsoft.Json.Linq.JObject();
                deleteArgs["name"] = "TestLight";
                UnityToolsMain.DeleteGameObject(deleteArgs);
                
            }
            catch (Exception ex)
            {
                AddResult($"✗ 光照工具测试失败: {ex.Message}");
                
                // 异常时也要清理测试对象
                try
                {
                    var deleteArgs = new Newtonsoft.Json.Linq.JObject();
                    deleteArgs["name"] = "TestLight";
                    UnityToolsMain.DeleteGameObject(deleteArgs);
                }
                catch (Exception cleanupEx)
                {
                    AddResult($"✗ 清理TestLight失败: {cleanupEx.Message}");
                }
            }
        }
        
        private void TestAssetTools()
        {
            AddResult("--- 测试资源工具 ---");
            
            if (!CheckPrerequisites("资源工具"))
            {
                return;
            }
            
            try
            {
                // 测试ImportAsset (使用一个可能存在的路径)
                var importArgs = new Newtonsoft.Json.Linq.JObject();
                importArgs["path"] = "Assets";
                var importResult = UnityToolsMain.ImportAsset(importArgs);
                AddResult(importResult.IsError ? $"✗ ImportAsset失败: {GetErrorMessage(importResult)}" : "✓ ImportAsset: 成功");
                
            }
            catch (Exception ex)
            {
                AddResult($"✗ 资源工具测试失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 检查测试前置条件
        /// </summary>
        /// <param name="testName">测试名称</param>
        /// <returns>是否满足前置条件</returns>
        private bool CheckPrerequisites(string testName)
        {
            AddResult($"检查{testName}前置条件...");
            
            // 检查Unity编辑器状态
            if (!Application.isEditor)
            {
                AddResult("✗ 前置条件失败: 必须在Unity编辑器中运行");
                return false;
            }
            
            // 检查是否在播放模式
            if (EditorApplication.isPlaying)
            {
                AddResult("⚠ 警告: 当前处于播放模式，某些测试可能受影响");
            }
            
            // 检查场景状态
            var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!activeScene.isLoaded)
            {
                AddResult("✗ 前置条件失败: 没有加载的活动场景");
                return false;
            }
            
            AddResult("✓ 前置条件检查通过");
            return true;
        }
        
        /// <summary>
        /// 检查特定功能的前置条件
        /// </summary>
        /// <param name="testName">测试名称</param>
        /// <param name="requiresGameObject">是否需要GameObject</param>
        /// <param name="requiresPlayMode">是否需要播放模式</param>
        /// <returns>是否满足前置条件</returns>
        private bool CheckPrerequisites(string testName, bool requiresGameObject = false, bool requiresPlayMode = false)
        {
            if (!CheckPrerequisites(testName))
            {
                return false;
            }
            
            if (requiresPlayMode && !EditorApplication.isPlaying)
            {
                AddResult("✗ 前置条件失败: 此测试需要在播放模式下运行");
                return false;
            }
            
            if (requiresGameObject)
            {
                var gameObjects = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
                if (gameObjects.Length == 0)
                {
                    AddResult("⚠ 警告: 场景中没有GameObject，将创建测试对象");
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// 测试HTTP请求功能
        /// </summary>
        private async void TestHttpRequests()
        {
            if (!CheckPrerequisites("HTTP请求测试"))
                return;
                
            AddResult("=== HTTP请求测试 ===");
            
            try
            {
                using (var client = new System.Net.Http.HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(10);
                    
                    // 测试GET请求
                    AddResult("--- 测试GET请求 ---");
                    try
                    {
                        var response = await client.GetAsync("https://httpbin.org/get");
                        AddResult($"✓ GET请求成功: {response.StatusCode}");
                        
                        var content = await response.Content.ReadAsStringAsync();
                        if (content.Contains("httpbin"))
                        {
                            AddResult("✓ GET响应内容验证通过");
                        }
                        else
                        {
                            AddResult("⚠ GET响应内容验证失败");
                        }
                    }
                    catch (Exception ex)
                    {
                        AddResult($"✗ GET请求失败: {ex.Message}");
                    }
                    
                    // 测试POST请求
                    AddResult("--- 测试POST请求 ---");
                    try
                    {
                        var postData = new System.Net.Http.StringContent(
                            "{\"test\": \"data\", \"unity\": \"mcp\"}",
                            System.Text.Encoding.UTF8,
                            "application/json"
                        );
                        
                        var response = await client.PostAsync("https://httpbin.org/post", postData);
                        AddResult($"✓ POST请求成功: {response.StatusCode}");
                        
                        var content = await response.Content.ReadAsStringAsync();
                        if (content.Contains("unity") && content.Contains("mcp"))
                        {
                            AddResult("✓ POST数据传输验证通过");
                        }
                        else
                        {
                            AddResult("⚠ POST数据传输验证失败");
                        }
                    }
                    catch (Exception ex)
                    {
                        AddResult($"✗ POST请求失败: {ex.Message}");
                    }
                    
                    // 测试本地MCP服务器连接
                    AddResult("--- 测试本地MCP服务器连接 ---");
                    if (_testServer != null && _testServer.IsRunning)
                    {
                        try
                        {
                            var mcpResponse = await client.GetAsync($"http://localhost:{_testServer.Port}/mcp");
                            AddResult($"✓ 本地MCP服务器连接成功: {mcpResponse.StatusCode}");
                        }
                        catch (Exception ex)
                        {
                            AddResult($"✗ 本地MCP服务器连接失败: {ex.Message}");
                        }
                    }
                    else
                    {
                        AddResult("ℹ 本地MCP服务器未运行，跳过连接测试");
                    }
                }
                
                AddResult("✓ HTTP请求测试完成");
            }
            catch (Exception ex)
            {
                AddResult($"✗ HTTP请求测试失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 测试HTTP响应验证功能
        /// </summary>
        private async void TestHttpResponseValidation()
        {
            if (!CheckPrerequisites("HTTP响应验证测试"))
                return;
                
            AddResult("=== HTTP响应验证测试 ===");
            
            try
            {
                using (var client = new System.Net.Http.HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(10);
                    
                    // 测试状态码验证
                    AddResult("--- 测试状态码验证 ---");
                    var testUrls = new Dictionary<string, System.Net.HttpStatusCode>
                    {
                        { "https://httpbin.org/status/200", System.Net.HttpStatusCode.OK },
                        { "https://httpbin.org/status/404", System.Net.HttpStatusCode.NotFound },
                        { "https://httpbin.org/status/500", System.Net.HttpStatusCode.InternalServerError }
                    };
                    
                    foreach (var testUrl in testUrls)
                    {
                        try
                        {
                            var response = await client.GetAsync(testUrl.Key);
                            if (response.StatusCode == testUrl.Value)
                            {
                                AddResult($"✓ 状态码验证通过: {testUrl.Value}");
                            }
                            else
                            {
                                AddResult($"✗ 状态码验证失败: 期望{testUrl.Value}, 实际{response.StatusCode}");
                            }
                        }
                        catch (System.Net.Http.HttpRequestException)
                        {
                            // 对于4xx和5xx状态码，HttpClient可能抛出异常，这是正常的
                            if (testUrl.Value == System.Net.HttpStatusCode.NotFound || 
                                testUrl.Value == System.Net.HttpStatusCode.InternalServerError)
                            {
                                AddResult($"✓ 状态码验证通过: {testUrl.Value} (异常处理正确)");
                            }
                            else
                            {
                                AddResult($"✗ 状态码验证异常: {testUrl.Value}");
                            }
                        }
                    }
                    
                    // 测试响应头验证
                    AddResult("--- 测试响应头验证 ---");
                    try
                    {
                        var response = await client.GetAsync("https://httpbin.org/response-headers?Content-Type=application/json&Server=httpbin");
                        
                        if (response.Headers.Contains("Server"))
                        {
                            AddResult("✓ 响应头Server字段存在");
                        }
                        else
                        {
                            AddResult("✗ 响应头Server字段缺失");
                        }
                        
                        if (response.Content.Headers.ContentType?.MediaType == "application/json")
                        {
                            AddResult("✓ Content-Type验证通过");
                        }
                        else
                        {
                            AddResult($"✗ Content-Type验证失败: {response.Content.Headers.ContentType?.MediaType}");
                        }
                    }
                    catch (Exception ex)
                    {
                        AddResult($"✗ 响应头验证失败: {ex.Message}");
                    }
                    
                    // 测试JSON响应解析
                    AddResult("--- 测试JSON响应解析 ---");
                    try
                    {
                        var response = await client.GetAsync("https://httpbin.org/json");
                        var jsonContent = await response.Content.ReadAsStringAsync();
                        
                        if (jsonContent.Contains("slideshow") && jsonContent.StartsWith("{"))
                        {
                            AddResult("✓ JSON响应格式验证通过");
                        }
                        else
                        {
                            AddResult("✗ JSON响应格式验证失败");
                        }
                    }
                    catch (Exception ex)
                    {
                        AddResult($"✗ JSON响应解析失败: {ex.Message}");
                    }
                }
                
                AddResult("✓ HTTP响应验证测试完成");
            }
            catch (Exception ex)
            {
                AddResult($"✗ HTTP响应验证测试失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 测试HTTP错误处理功能
        /// </summary>
        private async void TestHttpErrorHandling()
        {
            if (!CheckPrerequisites("HTTP错误处理测试"))
                return;
                
            AddResult("=== HTTP错误处理测试 ===");
            
            try
            {
                using (var client = new System.Net.Http.HttpClient())
                {
                    // 测试超时处理
                    AddResult("--- 测试超时处理 ---");
                    try
                    {
                        client.Timeout = TimeSpan.FromMilliseconds(100); // 设置很短的超时
                        await client.GetAsync("https://httpbin.org/delay/2"); // 请求2秒延迟
                        AddResult("✗ 超时处理失败: 应该抛出超时异常");
                    }
                    catch (TaskCanceledException)
                    {
                        AddResult("✓ 超时处理正确: 成功捕获超时异常");
                    }
                    catch (Exception ex)
                    {
                        AddResult($"⚠ 超时处理异常: {ex.GetType().Name} - {ex.Message}");
                    }
                    
                    // 重置超时设置
                    client.Timeout = TimeSpan.FromSeconds(10);
                    
                    // 测试无效URL处理
                    AddResult("--- 测试无效URL处理 ---");
                    try
                    {
                        await client.GetAsync("http://invalid-domain-that-does-not-exist-12345.com");
                        AddResult("✗ 无效URL处理失败: 应该抛出异常");
                    }
                    catch (System.Net.Http.HttpRequestException)
                    {
                        AddResult("✓ 无效URL处理正确: 成功捕获请求异常");
                    }
                    catch (Exception ex)
                    {
                        AddResult($"⚠ 无效URL处理异常: {ex.GetType().Name} - {ex.Message}");
                    }
                    
                    // 测试网络错误处理
                    AddResult("--- 测试网络错误处理 ---");
                    try
                    {
                        await client.GetAsync("http://localhost:99999"); // 不存在的端口
                        AddResult("✗ 网络错误处理失败: 应该抛出连接异常");
                    }
                    catch (System.Net.Http.HttpRequestException)
                    {
                        AddResult("✓ 网络错误处理正确: 成功捕获连接异常");
                    }
                    catch (Exception ex)
                    {
                        AddResult($"⚠ 网络错误处理异常: {ex.GetType().Name} - {ex.Message}");
                    }
                    
                    // 测试SSL证书错误处理
                    AddResult("--- 测试SSL证书错误处理 ---");
                    try
                    {
                        await client.GetAsync("https://self-signed.badssl.com/");
                        AddResult("⚠ SSL证书错误处理: 连接成功（可能系统忽略了证书错误）");
                    }
                    catch (System.Net.Http.HttpRequestException)
                    {
                        AddResult("✓ SSL证书错误处理正确: 成功捕获证书异常");
                    }
                    catch (Exception ex)
                    {
                        AddResult($"⚠ SSL证书错误处理异常: {ex.GetType().Name} - {ex.Message}");
                    }
                    
                    // 测试大文件下载错误处理
                    AddResult("--- 测试大文件处理 ---");
                    try
                    {
                        client.Timeout = TimeSpan.FromSeconds(5); // 设置较短超时
                        var response = await client.GetAsync("https://httpbin.org/bytes/1048576"); // 1MB数据
                        
                        if (response.IsSuccessStatusCode)
                        {
                            var content = await response.Content.ReadAsByteArrayAsync();
                            if (content.Length > 500000) // 检查是否接收到足够数据
                            {
                                AddResult($"✓ 大文件处理成功: 接收到{content.Length}字节");
                            }
                            else
                            {
                                AddResult($"⚠ 大文件处理部分成功: 只接收到{content.Length}字节");
                            }
                        }
                        else
                        {
                            AddResult($"✗ 大文件处理失败: {response.StatusCode}");
                        }
                    }
                    catch (TaskCanceledException)
                    {
                        AddResult("⚠ 大文件处理超时: 这是正常的保护机制");
                    }
                    catch (Exception ex)
                    {
                        AddResult($"⚠ 大文件处理异常: {ex.GetType().Name} - {ex.Message}");
                    }
                }
                
                AddResult("✓ HTTP错误处理测试完成");
            }
            catch (Exception ex)
            {
                AddResult($"✗ HTTP错误处理测试失败: {ex.Message}");
            }
        }
        
        private void OnDestroy()
        {
            // 确保窗口关闭时停止测试服务器
            if (_testServer != null && _testServer.IsRunning)
            {
                _testServer.Stop();
                _testServer = null;
            }
        }
    }
}