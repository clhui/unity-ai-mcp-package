using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.MCP.Tools.Editor;

namespace Unity.MCP.Editor
{
    public class ClientInfo
    {
        public string RemoteEndPoint { get; set; }
        public string UserAgent { get; set; }
        public DateTime LastSeen { get; set; }
        public int RequestCount { get; set; }
        public string LastMethod { get; set; }
    }
    
    public class McpServer
    {
        private HttpListener _httpListener;
        private Thread _listenerThread;
        private bool _isRunning;
        private readonly int _port;
        private readonly Dictionary<string, Func<JObject, McpToolResult>> _tools;
        private readonly Dictionary<string, (string description, JObject inputSchema)> _toolDefinitions;
        private readonly Dictionary<string, ClientInfo> _connectedClients;
        private readonly object _clientsLock = new object();
        
        // 线程安全的播放模式状态跟踪
        private static volatile bool _isPlayModeChanging = false;
        private static readonly object _playModeLock = new object();
        
        public bool IsRunning => _isRunning;
        public int Port => _port;
        public IReadOnlyDictionary<string, ClientInfo> ConnectedClients
        {
            get
            {
                lock (_clientsLock)
                {
                    return new Dictionary<string, ClientInfo>(_connectedClients);
                }
            }
        }
        
        public McpServer(int port = 9123)
        {
            _port = port;
            _tools = new Dictionary<string, Func<JObject, McpToolResult>>();
            _toolDefinitions = new Dictionary<string, (string description, JObject inputSchema)>();
            _connectedClients = new Dictionary<string, ClientInfo>();
            
            // 初始化播放模式状态监听（需要在主线程调用）
            if (UnityEditorInternal.InternalEditorUtility.CurrentThreadIsMainThread())
            {
                InitializePlayModeTracking();
            }
            
            RegisterDefaultTools();
        }
        
        /// <summary>
        /// 初始化播放模式状态跟踪
        /// </summary>
        private static void InitializePlayModeTracking()
        {
            // 移除可能存在的旧监听器
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            // 添加新监听器
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }
        
        /// <summary>
        /// 播放模式状态变化回调
        /// </summary>
        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            lock (_playModeLock)
            {
                switch (state)
                {
                    case PlayModeStateChange.ExitingEditMode:
                    case PlayModeStateChange.ExitingPlayMode:
                        _isPlayModeChanging = true;
                        break;
                    case PlayModeStateChange.EnteredEditMode:
                    case PlayModeStateChange.EnteredPlayMode:
                        _isPlayModeChanging = false;
                        break;
                }
            }
        }
        
        /// <summary>
        /// 线程安全的播放模式变化检查
        /// </summary>
        private static bool IsPlayModeChanging()
        {
            lock (_playModeLock)
            {
                return _isPlayModeChanging;
            }
        }

        public void Start()
        {
            if (_isRunning) 
            {
                return;
            }
            
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            
            try
            {
                // 确保播放模式跟踪已初始化
                if (UnityEditorInternal.InternalEditorUtility.CurrentThreadIsMainThread())
                {
                    InitializePlayModeTracking();
                }
                
                _httpListener = new HttpListener();
                _httpListener.Prefixes.Add($"http://localhost:{_port}/");
                
                _httpListener.Start();
                
                _isRunning = true;
                
                _listenerThread = new Thread(ListenForRequests) 
                { 
                    IsBackground = true,
                    Name = "MCP-Listener-Thread"
                };
                _listenerThread.Start();
                
                // 初始化场景变化监听器
                // UnityToolsMain.InitializeSceneListener();
                
                // 订阅场景变化事件，实现场景切换时重新初始化插件
                // UnityToolsMain.OnSceneChanged += OnSceneChanged;
            }
            catch (Exception ex)
            {
                McpLogger.LogException(ex, "启动服务器时发生异常");
            }
        }
        
        public void Stop()
        {
            if (!_isRunning) 
            {
                return;
            }
            
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var connectedClientsCount = _connectedClients.Count;
            
            _isRunning = false;
            
            // 停止HTTP监听器
            try
            {
                _httpListener?.Stop();
                _httpListener?.Close();
            }
            catch (Exception ex)
            {
                McpLogger.LogWarning($"停止HTTP监听器时发生异常: {ex.Message}");
            }
            
            // 安全地等待监听线程结束
            if (_listenerThread != null && _listenerThread.IsAlive)
            {
                try
                {
                    // 使用更短的超时时间，避免长时间阻塞
                    if (!_listenerThread.Join(500)) // 500ms超时
                    {
                        McpLogger.LogDebug("监听线程未能在500ms内正常结束，强制中止");
                        try
                        {
                            _listenerThread.Abort();
                        }
                        catch (Exception ex)
                        {
                            McpLogger.LogDebug($"中止监听线程时发生异常: {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    McpLogger.LogDebug($"等待监听线程结束时发生异常: {ex.Message}");
                }
            }
            
            // 取消订阅场景变化事件
            try
            {
                UnityToolsMain.OnSceneChanged -= OnSceneChanged;
            }
            catch (Exception ex)
            {
                McpLogger.LogDebug($"取消订阅场景事件时发生异常: {ex.Message}");
            }
            
            // 清理客户端连接记录
            try
            {
                lock (_clientsLock)
                {
                    _connectedClients.Clear();
                }
            }
            catch (Exception ex)
            {
                McpLogger.LogDebug($"清理客户端连接记录时发生异常: {ex.Message}");
            }
            

        }
        
        /// <summary>
        /// 场景变化事件处理方法 - 重新初始化整个插件
        /// </summary>
        /// <param name="previousScenePath">之前的场景路径</param>
        /// <param name="newScenePath">新的场景路径</param>
        private void OnSceneChanged(string previousScenePath, string newScenePath)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

            
            try
            {
                // 1. 清理当前连接的客户端状态
                lock (_clientsLock)
                {

                    foreach (var client in _connectedClients.Values)
                    {
                        client.LastSeen = DateTime.Now;
                        client.LastMethod = "scene_changed";
                    }
                }
                
                // 2. 重新注册所有工具

                _tools.Clear();
                _toolDefinitions.Clear();
                RegisterDefaultTools();
                
                // 3. 强制垃圾回收，清理旧场景的资源引用
                System.GC.Collect();
                System.GC.WaitForPendingFinalizers();
                

            }
            catch (Exception ex)
            {
                McpLogger.LogException(ex, "停止服务器时发生异常");
            }
        }
        
        private void ListenForRequests()
        {

            
            while (_isRunning)
            {
                try
                {

                    
                    var context = _httpListener.GetContext();
                    

                    HandleRequest(context);
                }
                catch (ThreadAbortException)
                {

                    // 线程正常中止，不需要记录错误
                    break;
                }
                catch (HttpListenerException ex) when (!_isRunning)
                {

                    // 服务器停止时的正常异常，不需要记录错误
                    break;
                }
                catch (Exception ex)
                {
                    if (_isRunning)
                    {
                        McpLogger.LogException(ex, "监听请求时发生异常");
                    }
                }
            }
            

        }
        
        private void HandleRequest(object state)
        {
            var context = (HttpListenerContext)state;
            var request = context.Request;
            var response = context.Response;
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            

            
            try
            {
                // 记录客户端连接信息
                TrackClientConnection(request);
                

                
                // 设置CORS头
                response.Headers.Add("Access-Control-Allow-Origin", "*");
                response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
                response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Accept");
                

                
                if (request.HttpMethod == "OPTIONS")
                {

                    response.StatusCode = 200;
                    response.Close();
                    return;
                }
                
                if (request.Url.AbsolutePath == "/mcp")
                {
                    if (request.HttpMethod == "POST")
                    {

                        HandleJsonRpcRequest(request, response);
                    }
                    else if (request.HttpMethod == "GET")
                    {

                        // 暂不支持SSE流
                        response.StatusCode = 405;
                        response.Close();
                    }
                }
                else
                {

                    response.StatusCode = 404;
                    response.Close();
                }
                

            }//如果是above的异常，只打印info级别
            catch(ThreadAbortException ex){ 
                //通过editorapp检测是否在切换场景，如果是切换场景打印info级别，如果是其他情况打印error
                if(IsPlayModeChanging()){
                    McpLogger.LogDebug($"Operation {request.HttpMethod} {request.Url.AbsolutePath} 正在切换场景，忽略异常: {ex.Message}");
                }else{
                    McpLogger.LogException(ex, $"Operation {request.HttpMethod} {request.Url.AbsolutePath} 发生异常");
                }
                throw;
            }
            catch (Exception ex)
            {
                McpLogger.LogException(ex, "处理请求时发生异常");
                
                try
                {
                    response.StatusCode = 500;
                    response.Close();
                }
                catch { }
            }
        }
        
        private void HandleJsonRpcRequest(HttpListenerRequest request, HttpListenerResponse response)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var clientEndpoint = request.RemoteEndPoint?.ToString() ?? "unknown";
            

            
            string requestBody;
            using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
            {
                requestBody = reader.ReadToEnd();
            }
            

            

            
            JsonRpcResponse jsonResponse;
            
            try
            {
                var jsonRequest = JsonConvert.DeserializeObject<JsonRpcRequest>(requestBody);
                

                
                jsonResponse = ProcessJsonRpcRequest(jsonRequest);
                

            }
            //如果是above的异常，只打印info级别
            catch(ThreadAbortException ex){ 
                //通过线程安全方法检测是否在切换场景，如果是切换场景打印info级别，如果是其他情况打印error
                if(IsPlayModeChanging()){
                    McpLogger.LogDebug($"Operation timed out after: {ex.Message}");
                    jsonResponse = new JsonRpcResponse
                    {
                        Error = new JsonRpcError
                        {
                            Code = -32700,
                            Message = "正在切换场景",
                            Data = JToken.FromObject(ex.Message)
                        }
                    };
                }else{
                    McpLogger.LogException(ex, "Operation timed out after");
                    jsonResponse = new JsonRpcResponse
                    {
                        Error = new JsonRpcError
                        {
                            Code = -32700,
                            Message = ex.Message,
                            Data = JToken.FromObject(ex.Message)
                        }
                    };
                }
            }
            catch (Exception ex)
            {
                McpLogger.LogException(ex, "解析JSON-RPC请求时发生异常");
                
                jsonResponse = new JsonRpcResponse
                {
                    Error = new JsonRpcError
                    {
                        Code = -32700,
                        Message = "Parse error",
                        Data = JToken.FromObject(ex.Message)
                    }
                };
            }
            

            
            var responseJson = JsonConvert.SerializeObject(jsonResponse);
            var responseBytes = Encoding.UTF8.GetBytes(responseJson);
            

            

            
            response.ContentType = "application/json";
            response.ContentLength64 = responseBytes.Length;
            response.StatusCode = 200;
            

            
            response.OutputStream.Write(responseBytes, 0, responseBytes.Length);
            response.Close();
            

        }
        
        private JsonRpcResponse ProcessJsonRpcRequest(JsonRpcRequest request)
        {
            var response = new JsonRpcResponse { Id = request.Id };
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            

            

            
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            try
            {
                switch (request.Method)
                {
                    case "initialize":
                        response.Result = HandleInitialize(request.Params);
                        break;
                        
                    case "tools/list":
                        response.Result = HandleToolsList(request.Params);
                        break;
                        
                    case "tools/call":
                        response.Result = HandleToolsCall(request.Params);
                        break;
                        
                    default:
                        response.Error = new JsonRpcError
                        {
                            Code = -32601,
                            Message = "Method not found",
                            Data = JToken.FromObject(request.Method)
                        };
                        break;
                }
                
                stopwatch.Stop();
                

            }catch(ThreadAbortException ex){ 
                //通过线程安全方法检测是否在切换场景，如果是切换场景打印info级别，如果是其他情况打印error
                if(IsPlayModeChanging()){
                    McpLogger.LogDebug($"Operation {request.Method} 正在切换场景，忽略异常: {ex.Message}");
                    response.Error = new JsonRpcError
                        {
                            Code = -32603,
                            Message = "正在切换场景",
                            Data = JToken.FromObject(ex.Message)
                        };
                }else{
                    McpLogger.LogException(ex, $"Operation {request.Method} 发生异常中断");
                     response.Error = new JsonRpcError
                        {
                            Code = -32603,
                            Message = ex.Message,
                            Data = JToken.FromObject(ex.Message)
                        };
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                McpLogger.LogException(ex, $"处理JSON-RPC请求时发生异常 (方法: {request.Method})");
                
                
            }
            

            
            return response;
        }
        
        private JObject HandleInitialize(JObject parameters)
        {
            return JObject.FromObject(new
            {
                protocolVersion = "2024-11-05",
                capabilities = new McpCapabilities
                {
                    Tools = new McpToolsCapability { ListChanged = false }
                },
                serverInfo = new
                {
                    name = "Unity MCP Server",
                    version = "1.0.2"
                }
            });
        }
        
        private JObject HandleToolsList(JObject parameters)
        {
            var tools = new List<McpTool>();
            var config = McpToolConfig.Instance;
            
            foreach (var toolName in _tools.Keys)
            {
                // 只返回配置中启用的工具
                if (config.IsToolEnabled(toolName))
                {
                    tools.Add(GetToolDefinition(toolName));
                }
            }
            
            return JObject.FromObject(new { tools });
        }
        
        private JToken HandleToolsCall(JObject parameters)
        {
            var toolName = parameters["name"]?.ToString();
            var arguments = parameters["arguments"] as JObject ?? new JObject();
            
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            
            if (string.IsNullOrEmpty(toolName) || !_tools.ContainsKey(toolName))
            {
                throw new ArgumentException($"Unknown tool: {toolName}");
            }
            
            try
            {
                var startTime = DateTime.Now;
                var result = _tools[toolName](arguments);
                var endTime = DateTime.Now;
                var duration = (endTime - startTime).TotalMilliseconds;
                
                // 记录工具调用成功日志
                McpLogger.LogTool($"工具调用成功: {toolName}, 耗时: {duration:F2}ms");
                
                // 更新客户端最后操作记录
                UpdateClientLastMethod(toolName);
                
                return JToken.FromObject(result);
            }
            //如果是ThreadAbortException异常，只打印info级别
            catch(ThreadAbortException ex){ 
                //通过线程安全的方式检测是否在切换播放模式，如果是切换模式打印info级别，如果是其他情况打印error
                if(IsPlayModeChanging()){
                    McpLogger.LogDebug($"播放模式切换中，操作被中止: {ex.Message}");
                }else{
                    McpLogger.LogException(ex, "操作被意外中止");
                }
                throw;
            }
            catch (Exception ex)
            {
                McpLogger.LogException(ex, "工具调用失败");
                throw;
            }
        }
        
        private void RegisterDefaultTools()
        {
            var config = McpToolConfig.Instance;
            
            // 注册Unity工具 - 场景管理
            if (config.IsToolEnabled("list_scenes"))
            {
                RegisterTool("list_scenes", "List all scenes in the project", 
                    CreateInputSchema(new Dictionary<string, object>()), 
                    args => ExecuteOnMainThread(() => UnitySceneTools.ListScenes(args)));
            }
                
            if (config.IsToolEnabled("open_scene"))
            {
                RegisterTool("open_scene", "Open a specific scene", 
                    CreateInputSchema(new Dictionary<string, object> 
                    {
                        ["path"] = new { type = "string", description = "Path to the scene file" }
                    }), 
                    args => ExecuteOnMainThread(() => UnitySceneTools.OpenScene(args)));
            }
                
            if (config.IsToolEnabled("load_scene"))
            {
                RegisterTool("load_scene", "Load a scene (works in play mode)", 
                    CreateInputSchema(new Dictionary<string, object> 
                    {
                        ["name"] = new { type = "string", description = "Scene name (without extension)" },
                        ["path"] = new { type = "string", description = "Path to the scene file (optional, will extract name from path)" }
                    }), 
                    args => ExecuteOnMainThread(() => UnitySceneTools.LoadScene(args)));
            }
                
            // 播放模式工具
            if (config.IsToolEnabled("play_mode_start"))
            {
                RegisterTool("play_mode_start", "Start Unity play mode", 
                    CreateInputSchema(new Dictionary<string, object>()), 
                    args => ExecutePlayModeStartWithStatusCheck(args));
            }
                
            if (config.IsToolEnabled("play_mode_stop"))
            {
                RegisterTool("play_mode_stop", "Stop Unity play mode", 
                    CreateInputSchema(new Dictionary<string, object>()), 
                    args => ExecutePlayModeStopWithStatusCheck(args));
            }
                
            if (config.IsToolEnabled("get_play_mode_status"))
            {
                RegisterTool("get_play_mode_status", "Get current play mode status", 
                    CreateInputSchema(new Dictionary<string, object>()), 
                    args => ExecuteOnMainThread(() => UnityPlayModeTools.GetPlayModeStatus(args)));
            }
                
            if (config.IsToolEnabled("get_current_scene_info"))
            {
                RegisterTool("get_current_scene_info", "Get detailed information about the current active scene", 
                    CreateInputSchema(new Dictionary<string, object>()), 
                    args => ExecuteOnMainThread(() => UnitySceneTools.GetCurrentSceneInfo(args)));
            }

            // 调试工具
            if (config.IsToolEnabled("get_thread_stack_info"))
            {
                RegisterTool("get_thread_stack_info", "Get Unity thread stack information and deadlock detection with detailed call stack analysis", 
                    CreateInputSchema(new Dictionary<string, object>()), 
                    args => ExecuteOnMainThread(() => UnityToolsMain.GetThreadStackInfo(args)));
            }
                
            RegisterTool("create_gameobject", "Create a new GameObject", 
                CreateInputSchema(new Dictionary<string, object> 
                {
                    ["name"] = new { type = "string", description = "Name of the GameObject" },
                    ["parent"] = new { type = "string", description = "Parent GameObject path (optional)" }
                }), 
                args => ExecuteOnMainThread(() => UnityGameObjectTools.CreateGameObject(args)));
                
            RegisterTool("add_component", "Add a component to a GameObject", 
                CreateInputSchema(new Dictionary<string, object> 
                {
                    ["gameObject"] = new { type = "string", description = "Name of the GameObject" },
                    ["component"] = new { type = "string", description = "Type of component to add" }
                }), 
                args => ExecuteOnMainThread(() => UnityComponentTools.AddComponent(args)));
                
            RegisterTool("set_transform", "Set transform properties of a GameObject", 
                CreateInputSchema(new Dictionary<string, object> 
                {
                    ["gameObject"] = new { type = "string", description = "Name of the GameObject" },
                    ["position"] = new { type = "object", description = "Position {x, y, z}" },
                    ["rotation"] = new { type = "object", description = "Rotation {x, y, z}" },
                    ["scale"] = new { type = "object", description = "Scale {x, y, z}" }
                }), 
                args => ExecuteOnMainThread(() => UnityGameObjectTools.SetTransform(args)));
                
            RegisterTool("import_asset", "Import an asset into the project", 
                CreateInputSchema(new Dictionary<string, object> 
                {
                    ["path"] = new { type = "string", description = "Path to the asset file" }
                }), 
                args => ExecuteOnMainThread(() => UnityToolsMain.ImportAsset(args)));
                
            // 高级游戏对象操作接口
            RegisterTool("find_gameobject", "Find a GameObject by name or path", 
                CreateInputSchema(new Dictionary<string, object> 
                {
                    ["name"] = new { type = "string", description = "Name or path of the GameObject to find" }
                }), 
                args => ExecuteOnMainThread(() => UnityGameObjectTools.FindGameObject(args)));
                
            RegisterTool("delete_gameobject", "Delete a GameObject", 
                CreateInputSchema(new Dictionary<string, object> 
                {
                    ["name"] = new { type = "string", description = "Name of the GameObject to delete" }
                }), 
                args => ExecuteOnMainThread(() => UnityGameObjectTools.DeleteGameObject(args)));
                
            RegisterTool("duplicate_gameobject", "Duplicate a GameObject", 
                CreateInputSchema(new Dictionary<string, object> 
                {
                    ["name"] = new { type = "string", description = "Name of the GameObject to duplicate" },
                    ["newName"] = new { type = "string", description = "Name for the duplicated GameObject (optional)" }
                }), 
                args => ExecuteOnMainThread(() => UnityGameObjectTools.DuplicateGameObject(args)));
                
            RegisterTool("set_parent", "Set parent of a GameObject", 
                CreateInputSchema(new Dictionary<string, object> 
                {
                    ["child"] = new { type = "string", description = "Name of the child GameObject" },
                    ["parent"] = new { type = "string", description = "Name of the parent GameObject (null to unparent)" }
                }), 
                args => ExecuteOnMainThread(() => UnityGameObjectTools.SetParent(args)));
                
            RegisterTool("get_gameobject_info", "Get detailed information about a GameObject", 
                 CreateInputSchema(new Dictionary<string, object> 
                 {
                     ["name"] = new { type = "string", description = "Name of the GameObject" }
                 }), 
                 args => ExecuteOnMainThread(() => UnityGameObjectTools.GetGameObjectInfo(args)));
                
            // 组件管理接口
            RegisterTool("remove_component", "Remove a component from a GameObject", 
                CreateInputSchema(new Dictionary<string, object> 
                {
                    ["gameObject"] = new { type = "string", description = "Name of the GameObject" },
                    ["component"] = new { type = "string", description = "Type of component to remove" }
                }), 
                args => ExecuteOnMainThread(() => UnityComponentTools.RemoveComponent(args)));
                 
             RegisterTool("get_component_properties", "Get properties of a component", 
                 CreateInputSchema(new Dictionary<string, object> 
                 {
                     ["gameObject"] = new { type = "string", description = "Name of the GameObject" },
                     ["component"] = new { type = "string", description = "Type of component" }
                 }), 
                 args => ExecuteOnMainThread(() => UnityComponentTools.GetComponentProperties(args)));
                
            RegisterTool("set_component_properties", "Set properties of a component", 
                CreateInputSchema(new Dictionary<string, object> 
                {
                    ["gameObject"] = new { type = "string", description = "Name of the GameObject" },
                    ["component"] = new { type = "string", description = "Type of component" },
                    ["properties"] = new { type = "object", description = "Properties to set as key-value pairs" }
                }), 
                args => ExecuteOnMainThread(() => UnityComponentTools.SetComponentProperties(args)));
                 
             RegisterTool("list_components", "List all components on a GameObject", 
                 CreateInputSchema(new Dictionary<string, object> 
                 {
                     ["gameObject"] = new { type = "string", description = "Name of the GameObject" }
                 }), 
                 args => ExecuteOnMainThread(() => UnityComponentTools.ListComponents(args)));
                
            // 材质和渲染接口
            RegisterTool("create_material", "Create a new material", 
                CreateInputSchema(new Dictionary<string, object> 
                {
                    ["name"] = new { type = "string", description = "Name of the material" },
                    ["shader"] = new { type = "string", description = "Shader name (optional)" }
                }), 
                args => ExecuteOnMainThread(() => UnityMaterialTools.CreateMaterial(args)));
                 
             RegisterTool("set_material_properties", "Set properties of a material", 
                 CreateInputSchema(new Dictionary<string, object> 
                 {
                     ["material"] = new { type = "string", description = "Name of the material" },
                     ["properties"] = new { type = "object", description = "Properties to set as key-value pairs" }
                 }), 
                 args => ExecuteOnMainThread(() => UnityMaterialTools.SetMaterialProperties(args)));
                
            RegisterTool("assign_material", "Assign a material to a GameObject's renderer", 
                CreateInputSchema(new Dictionary<string, object> 
                {
                    ["gameObject"] = new { type = "string", description = "Name of the GameObject" },
                    ["material"] = new { type = "string", description = "Name of the material" },
                    ["materialIndex"] = new { type = "integer", description = "Material index (optional, default 0)" }
                }), 
                args => ExecuteOnMainThread(() => UnityMaterialTools.AssignMaterial(args)));
                 
             RegisterTool("set_renderer_properties", "Set properties of a renderer component", 
                 CreateInputSchema(new Dictionary<string, object> 
                 {
                     ["gameObject"] = new { type = "string", description = "Name of the GameObject" },
                     ["properties"] = new { type = "object", description = "Renderer properties to set" }
                 }), 
                 args => ExecuteOnMainThread(() => UnityMaterialTools.SetRendererProperties(args)));
                
            // 物理系统接口
            RegisterTool("set_rigidbody_properties", "Set properties of a rigidbody component", 
                CreateInputSchema(new Dictionary<string, object> 
                {
                    ["gameObject"] = new { type = "string", description = "Name of the GameObject" },
                    ["properties"] = new { type = "object", description = "Rigidbody properties to set" }
                }), 
                args => ExecuteOnMainThread(() => UnityPhysicsTools.SetRigidbodyProperties(args)));
                 
             RegisterTool("add_force", "Add force to a rigidbody", 
                 CreateInputSchema(new Dictionary<string, object> 
                 {
                     ["gameObject"] = new { type = "string", description = "Name of the GameObject" },
                     ["force"] = new { type = "object", description = "Force vector {x, y, z}" },
                     ["forceMode"] = new { type = "string", description = "Force mode (Force, Acceleration, Impulse, VelocityChange)" }
                 }), 
                 args => ExecuteOnMainThread(() => UnityPhysicsTools.AddForce(args)));
                
            RegisterTool("set_collider_properties", "Set properties of a collider component", 
                CreateInputSchema(new Dictionary<string, object> 
                {
                    ["gameObject"] = new { type = "string", description = "Name of the GameObject" },
                    ["properties"] = new { type = "object", description = "Collider properties to set" }
                }), 
                args => ExecuteOnMainThread(() => UnityPhysicsTools.SetColliderProperties(args)));
                 
             RegisterTool("raycast", "Perform a raycast in the scene", 
                 CreateInputSchema(new Dictionary<string, object> 
                 {
                     ["origin"] = new { type = "object", description = "Ray origin {x, y, z}" },
                     ["direction"] = new { type = "object", description = "Ray direction {x, y, z}" },
                     ["maxDistance"] = new { type = "number", description = "Maximum distance (optional, default Infinity)" },
                     ["layerMask"] = new { type = "integer", description = "Layer mask (optional, default -1)" }
                 }), 
                 args => ExecuteOnMainThread(() => UnityPhysicsTools.Raycast(args)));
                
            // 音频系统接口
            RegisterTool("play_audio", "Play audio on a GameObject", 
                CreateInputSchema(new Dictionary<string, object> 
                {
                    ["gameObject"] = new { type = "string", description = "Name of the GameObject" },
                    ["audioClip"] = new { type = "string", description = "Path to the audio clip (optional)" },
                    ["loop"] = new { type = "boolean", description = "Whether to loop the audio (optional, default false)" },
                    ["volume"] = new { type = "number", description = "Volume level (optional, default 1.0)" },
                    ["pitch"] = new { type = "number", description = "Pitch level (optional, default 1.0)" }
                }), 
                args => ExecuteOnMainThread(() => UnityAudioTools.PlayAudio(args)));
                 
             RegisterTool("stop_audio", "Stop audio on a GameObject", 
                 CreateInputSchema(new Dictionary<string, object> 
                 {
                     ["gameObject"] = new { type = "string", description = "Name of the GameObject" },
                     ["fadeOut"] = new { type = "boolean", description = "Whether to fade out (optional, default false)" },
                     ["fadeTime"] = new { type = "number", description = "Fade out time in seconds (optional, default 1.0)" }
                 }), 
                 args => ExecuteOnMainThread(() => UnityAudioTools.StopAudio(args)));
                
            RegisterTool("set_audio_properties", "Set properties of an AudioSource component", 
                CreateInputSchema(new Dictionary<string, object> 
                {
                    ["gameObject"] = new { type = "string", description = "Name of the GameObject" },
                    ["properties"] = new { type = "object", description = "Audio properties to set" }
                }), 
                args => ExecuteOnMainThread(() => UnityToolsMain.SetAudioProperties(args)));
                 
             // 光照系统接口
             RegisterTool("create_light", "Create a new light in the scene", 
                 CreateInputSchema(new Dictionary<string, object> 
                 {
                     ["name"] = new { type = "string", description = "Name of the light GameObject" },
                     ["type"] = new { type = "string", description = "Type of light (Directional, Point, Spot, Area)" },
                     ["position"] = new { type = "object", description = "Position {x, y, z} (optional)" },
                     ["rotation"] = new { type = "object", description = "Rotation {x, y, z} (optional)" }
                 }), 
                 args => ExecuteOnMainThread(() => UnityToolsMain.CreateLight(args)));
                
            RegisterTool("set_light_properties", "Set properties of a Light component", 
                CreateInputSchema(new Dictionary<string, object> 
                {
                    ["gameObject"] = new { type = "string", description = "Name of the GameObject" },
                    ["properties"] = new { type = "object", description = "Light properties to set" }
                }), 
                args => ExecuteOnMainThread(() => UnityToolsMain.SetLightProperties(args)));
                
            // 脚本管理系统接口
            RegisterTool("create_script", "Create a new C# script file", 
                CreateInputSchema(new Dictionary<string, object> 
                {
                    ["name"] = new { type = "string", description = "Name of the script (without .cs extension)" },
                    ["path"] = new { type = "string", description = "Path where to create the script (optional, default: Assets/Scripts)" },
                    ["content"] = new { type = "string", description = "Script content (optional, will use template if not provided)" },
                    ["type"] = new { type = "string", description = "Script type: MonoBehaviour, ScriptableObject, Editor, Interface, Static (optional, default: MonoBehaviour)" }
                }), 
                args => ExecuteOnMainThread(() => UnityToolsMain.CreateScript(args)));
                
            RegisterTool("modify_script", "Modify an existing C# script file", 
                CreateInputSchema(new Dictionary<string, object> 
                {
                    ["path"] = new { type = "string", description = "Path to the script file" },
                    ["content"] = new { type = "string", description = "New script content" }
                }), 
                args => ExecuteOnMainThread(() => UnityToolsMain.ModifyScript(args)));
                
            RegisterTool("compile_scripts", "Compile all scripts and wait for completion", 
                CreateInputSchema(new Dictionary<string, object> 
                {
                }), 
                args => ExecuteOnMainThread(() => UnityToolsMain.CompileScripts(args)));
                
            RegisterTool("get_script_errors", "Get compilation errors and warnings", 
                CreateInputSchema(new Dictionary<string, object> 
                {
                }), 
                args => ExecuteOnMainThread(() => UnityToolsMain.GetScriptErrors(args)));
                
            // UI系统接口
            RegisterTool("create_canvas", "Create a new Canvas for UI elements", 
                CreateInputSchema(new Dictionary<string, object> 
                {
                    ["canvasName"] = new { type = "string", description = "Name of the canvas" },
                    ["renderMode"] = new { type = "string", description = "Render mode: ScreenSpaceOverlay, ScreenSpaceCamera, WorldSpace (optional, default: ScreenSpaceOverlay)" },
                    ["sortingOrder"] = new { type = "number", description = "Sorting order (optional, default: 0)" }
                }), 
                args => ExecuteOnMainThread(() => UnityToolsMain.CreateCanvas(
                    args.ContainsKey("canvasName") ? args["canvasName"].ToString() : "Canvas",
                    args.ContainsKey("renderMode") ? args["renderMode"].ToString() : "ScreenSpaceOverlay"
                )));
                
            RegisterTool("create_ui_element", "Create a UI element (Button, Text, Image, Slider, Panel)", 
                CreateInputSchema(new Dictionary<string, object> 
                {
                    ["elementName"] = new { type = "string", description = "Name of the UI element" },
                    ["elementType"] = new { type = "string", description = "Type of UI element: Button, Text, Image, Slider, Panel" },
                    ["parentName"] = new { type = "string", description = "Name of parent object (optional, will use Canvas if not specified)" }
                }), 
                args => ExecuteOnMainThread(() => UnityToolsMain.CreateUIElement(
                    args.ContainsKey("elementName") ? args["elementName"].ToString() : "UIElement",
                    args.ContainsKey("elementType") ? args["elementType"].ToString() : "Button",
                    args.ContainsKey("parentName") ? args["parentName"].ToString() : null
                )));
                
            RegisterTool("set_ui_properties", "Set properties for a UI element", 
                CreateInputSchema(new Dictionary<string, object> 
                {
                    ["elementName"] = new { type = "string", description = "Name of the UI element" },
                    ["properties"] = new { type = "string", description = "JSON string of properties to set" }
                }), 
                args => ExecuteOnMainThread(() => UnityToolsMain.SetUIProperties(
                    args.ContainsKey("elementName") ? args["elementName"].ToString() : "",
                    args.ContainsKey("properties") ? args["properties"].ToString() : "{}"
                )));
                
            RegisterTool("bind_ui_events", "Bind events to UI elements", 
                CreateInputSchema(new Dictionary<string, object> 
                {
                    ["elementName"] = new { type = "string", description = "Name of the UI element" },
                    ["eventType"] = new { type = "string", description = "Type of event: onClick, onValueChanged" },
                    ["methodName"] = new { type = "string", description = "Name of the method to call" },
                    ["targetObjectName"] = new { type = "string", description = "Name of target object (optional, uses element itself if not specified)" }
                }), 
                args => ExecuteOnMainThread(() => UnityToolsMain.BindUIEvents(
                    args.ContainsKey("elementName") ? args["elementName"].ToString() : "",
                    args.ContainsKey("eventType") ? args["eventType"].ToString() : "onClick",
                    args.ContainsKey("methodName") ? args["methodName"].ToString() : ""
                )));
                
            // Animation System
            RegisterTool("create_animator", "Create Animator component for GameObject", 
                CreateInputSchema(new Dictionary<string, object> 
                {
                    ["gameObjectName"] = new { type = "string", description = "Name of the GameObject" },
                    ["animatorControllerPath"] = new { type = "string", description = "Path to animator controller asset (optional)" }
                }), 
                args => ExecuteOnMainThread(() => UnityToolsMain.CreateAnimator(
                    args.ContainsKey("gameObjectName") ? args["gameObjectName"].ToString() : "",
                    args.ContainsKey("animatorControllerPath") ? args["animatorControllerPath"].ToString() : null
                )));
                
            RegisterTool("set_animation_clip", "Set animation clip for GameObject", 
                CreateInputSchema(new Dictionary<string, object> 
                {
                    ["gameObjectName"] = new { type = "string", description = "Name of the GameObject" },
                    ["clipName"] = new { type = "string", description = "Name for the animation clip" },
                    ["clipPath"] = new { type = "string", description = "Path to animation clip asset" }
                }), 
                args => ExecuteOnMainThread(() => UnityToolsMain.SetAnimationClip(
                    args.ContainsKey("gameObjectName") ? args["gameObjectName"].ToString() : "",
                    args.ContainsKey("clipName") ? args["clipName"].ToString() : "",
                    args.ContainsKey("clipPath") ? args["clipPath"].ToString() : ""
                )));
                
            RegisterTool("play_animation", "Play animation on GameObject", 
                CreateInputSchema(new Dictionary<string, object> 
                {
                    ["gameObjectName"] = new { type = "string", description = "Name of the GameObject" },
                    ["animationName"] = new { type = "string", description = "Name of animation to play (optional)" },
                    ["loop"] = new { type = "boolean", description = "Whether to loop the animation" }
                }), 
                args => ExecuteOnMainThread(() => UnityToolsMain.PlayAnimation(
                    args.ContainsKey("gameObjectName") ? args["gameObjectName"].ToString() : "",
                    args.ContainsKey("animationName") ? args["animationName"].ToString() : null,
                    args.ContainsKey("loop") && bool.Parse(args["loop"].ToString())
                )));
                
            RegisterTool("set_animation_parameters", "Set animation parameters for Animator", 
                CreateInputSchema(new Dictionary<string, object> 
                {
                    ["gameObjectName"] = new { type = "string", description = "Name of the GameObject" },
                    ["parameters"] = new { type = "string", description = "JSON string of animation parameters" }
                }), 
                args => ExecuteOnMainThread(() => UnityToolsMain.SetAnimationParameters(
                    args.ContainsKey("gameObjectName") ? args["gameObjectName"].ToString() : "",
                    args.ContainsKey("parameters") ? args["parameters"].ToString() : "{}"
                )));
                
            RegisterTool("create_animation_clip", "Create new animation clip", 
                 CreateInputSchema(new Dictionary<string, object> 
                 {
                     ["clipName"] = new { type = "string", description = "Name of the animation clip" },
                     ["savePath"] = new { type = "string", description = "Path to save the animation clip" },
                     ["targetObjectName"] = new { type = "string", description = "Name of target object for animation (optional)" }
                 }), 
                 args => ExecuteOnMainThread(() => UnityToolsMain.CreateAnimationClip(
                     args.ContainsKey("clipName") ? args["clipName"].ToString() : "",
                     args.ContainsKey("savePath") ? args["savePath"].ToString() : "",
                     args.ContainsKey("targetObjectName") ? args["targetObjectName"].ToString() : null
                 )));
                 
             // Input System
             RegisterTool("setup_input_actions", "Setup input actions for the project", 
                 CreateInputSchema(new Dictionary<string, object> 
                 {
                     ["inputActionsJson"] = new { type = "string", description = "JSON string defining input actions and bindings" }
                 }), 
                 args => ExecuteOnMainThread(() => UnityToolsMain.SetupInputActions(
                     args.ContainsKey("inputActionsJson") ? args["inputActionsJson"].ToString() : "{}"
                 )));
                 
             RegisterTool("bind_input_events", "Bind input events to GameObject", 
                 CreateInputSchema(new Dictionary<string, object> 
                 {
                     ["gameObjectName"] = new { type = "string", description = "Name of the GameObject" },
                     ["inputEventBindings"] = new { type = "string", description = "JSON string of input event bindings" }
                 }), 
                 args => ExecuteOnMainThread(() => UnityToolsMain.BindInputEvents(
                     args.ContainsKey("gameObjectName") ? args["gameObjectName"].ToString() : "",
                     args.ContainsKey("inputEventBindings") ? args["inputEventBindings"].ToString() : "{}"
                 )));
                 
             RegisterTool("simulate_input", "Simulate input events", 
                 CreateInputSchema(new Dictionary<string, object> 
                 {
                     ["inputType"] = new { type = "string", description = "Type of input: key, mouse" },
                     ["inputData"] = new { type = "string", description = "JSON string of input data" }
                 }), 
                 args => ExecuteOnMainThread(() => UnityToolsMain.SimulateInput(
                     args.ContainsKey("inputType") ? args["inputType"].ToString() : "key",
                     args.ContainsKey("inputData") ? args["inputData"].ToString() : "{}"
                 )));
                 
             RegisterTool("create_input_mapping", "Create input mapping configuration", 
                 CreateInputSchema(new Dictionary<string, object> 
                 {
                     ["mappingName"] = new { type = "string", description = "Name of the input mapping" },
                     ["inputMappingData"] = new { type = "string", description = "JSON string of input mapping data" }
                 }), 
                 args => ExecuteOnMainThread(() => UnityToolsMain.CreateInputMapping(
                     args.ContainsKey("mappingName") ? args["mappingName"].ToString() : "",
                     args.ContainsKey("inputMappingData") ? args["inputMappingData"].ToString() : "{}"
                 )));
                 
             // Particle System
             RegisterTool("create_particle_system", "Create particle system on GameObject", 
                 CreateInputSchema(new Dictionary<string, object> 
                 {
                     ["gameObjectName"] = new { type = "string", description = "Name of the GameObject (creates new if not found)" },
                     ["particleSystemName"] = new { type = "string", description = "Name for the particle system (optional)" }
                 }), 
                 args => ExecuteOnMainThread(() => {
                     string result = UnityToolsMain.CreateParticleSystem(
                         args.ContainsKey("gameObjectName") ? args["gameObjectName"].ToString() : "",
                         args.ContainsKey("particleSystemName") ? args["particleSystemName"].ToString() : ""
                     );
                     return new McpToolResult
                     {
                         Content = new List<McpContent>
                         {
                             new McpContent { Type = "text", Text = result }
                         }
                     };
                 }));
                 
             RegisterTool("set_particle_properties", "Set particle system properties", 
                 CreateInputSchema(new Dictionary<string, object> 
                 {
                     ["gameObjectName"] = new { type = "string", description = "Name of the GameObject with particle system" },
                     ["propertiesJson"] = new { type = "string", description = "JSON string of particle properties" }
                 }), 
                 args => ExecuteOnMainThread(() => {
                     string result = UnityToolsMain.SetParticleProperties(
                         args.ContainsKey("gameObjectName") ? args["gameObjectName"].ToString() : "",
                         args.ContainsKey("propertiesJson") ? args["propertiesJson"].ToString() : "{}"
                     );
                     return new McpToolResult
                     {
                         Content = new List<McpContent>
                         {
                             new McpContent { Type = "text", Text = result }
                         }
                     };
                 }));
                 
             RegisterTool("play_particle_effect", "Play or stop particle effect", 
                 CreateInputSchema(new Dictionary<string, object> 
                 {
                     ["gameObjectName"] = new { type = "string", description = "Name of the GameObject with particle system" },
                     ["play"] = new { type = "boolean", description = "True to play, false to stop" }
                 }), 
                 args => ExecuteOnMainThread(() => {
                     string result = UnityToolsMain.PlayParticleEffect(
                         args.ContainsKey("gameObjectName") ? args["gameObjectName"].ToString() : "",
                         args.ContainsKey("play") ? bool.Parse(args["play"].ToString()) : true
                     );
                     return new McpToolResult
                     {
                         Content = new List<McpContent>
                         {
                             new McpContent { Type = "text", Text = result }
                         }
                     };
                 }));
                 
             RegisterTool("create_particle_effect", "Create predefined particle effect", 
                 CreateInputSchema(new Dictionary<string, object> 
                 {
                     ["effectName"] = new { type = "string", description = "Name of the effect GameObject" },
                     ["effectType"] = new { type = "string", description = "Type of effect: fire, smoke, explosion, rain" },
                     ["targetObjectName"] = new { type = "string", description = "Target GameObject name (optional)" }
                 }), 
                 args => ExecuteOnMainThread(() => {
                     string result = UnityToolsMain.CreateParticleEffect(
                         args.ContainsKey("effectName") ? args["effectName"].ToString() : "",
                         args.ContainsKey("effectType") ? args["effectType"].ToString() : "default",
                         args.ContainsKey("targetObjectName") ? args["targetObjectName"].ToString() : ""
                     );
                     return new McpToolResult
                     {
                         Content = new List<McpContent>
                         {
                             new McpContent { Type = "text", Text = result }
                         }
                     };
                 }));
                 
             // Unity编辑器日志系统工具
             if (config.IsToolEnabled("get_unity_logs"))
             {
                 RegisterTool("get_unity_logs", "Get Unity Editor Console logs for debugging AI operations", 
                     CreateInputSchema(new Dictionary<string, object> 
                     {
                         ["maxCount"] = new { type = "number", description = "Maximum number of logs to retrieve (default: 100)" },
                         ["logLevel"] = new { type = "string", description = "Filter by log level: all, info, warning, error (default: all)" },
                         ["includeStackTrace"] = new { type = "boolean", description = "Include stack trace information (default: false)" },
                         ["searchText"] = new { type = "string", description = "Search for specific text in log messages (optional)" }
                     }), 
                     args => ExecuteOnMainThread(() => {
                         string result = UnityLogTools.GetUnityLogs(JsonConvert.SerializeObject(args));
                         return new McpToolResult
                         {
                             Content = new List<McpContent>
                             {
                                 new McpContent { Type = "text", Text = result }
                             }
                         };
                     }));
             }
                 
             if (config.IsToolEnabled("clear_unity_logs"))
             {
                 RegisterTool("clear_unity_logs", "Clear Unity Editor Console logs", 
                     CreateInputSchema(new Dictionary<string, object> 
                     {
                     }), 
                     args => ExecuteOnMainThread(() => {
                         string result = UnityLogTools.ClearUnityLogs(JsonConvert.SerializeObject(args));
                         return new McpToolResult
                         {
                             Content = new List<McpContent>
                             {
                                 new McpContent { Type = "text", Text = result }
                             }
                         };
                     }));
             }
                 
             if (config.IsToolEnabled("get_unity_log_stats"))
             {
                 RegisterTool("get_unity_log_stats", "Get Unity Editor Console log statistics", 
                     CreateInputSchema(new Dictionary<string, object> 
                     {
                     }), 
                     args => ExecuteOnMainThread(() => {
                         string result = UnityLogTools.GetUnityLogStats(JsonConvert.SerializeObject(args));
                         return new McpToolResult
                         {
                             Content = new List<McpContent>
                             {
                                 new McpContent { Type = "text", Text = result }
                             }
                         };
                     }));
             }
        }
        
        private void RegisterTool(string name, string description, JObject inputSchema, Func<JObject, McpToolResult> handler)
        {
            _tools[name] = handler;
            _toolDefinitions[name] = (description, inputSchema);
        }
        
        private void UnregisterTool(string name)
        {
            _tools.Remove(name);
            _toolDefinitions.Remove(name);
        }
        
        /// <summary>
        /// 刷新工具注册状态，根据配置重新注册所有工具
        /// </summary>
        public void RefreshTools()
        {
            // 清除所有现有工具
            _tools.Clear();
            _toolDefinitions.Clear();
            
            // 重新注册工具
            RegisterDefaultTools();
        }
        
        private JObject CreateInputSchema(Dictionary<string, object> properties)
        {
            var schema = new JObject
            {
                ["type"] = "object",
                ["properties"] = JObject.FromObject(properties)
            };
            
            if (properties.Count > 0)
            {
                schema["required"] = new JArray(properties.Keys.ToArray());
            }
            
            return schema;
        }
        
        private McpTool GetToolDefinition(string toolName)
        {
            if (_toolDefinitions.TryGetValue(toolName, out var toolDef))
            {
                return new McpTool
                {
                    Name = toolName,
                    Description = toolDef.description,
                    InputSchema = toolDef.inputSchema
                };
            }
            
            return new McpTool
            {
                Name = toolName,
                Description = "Unknown tool",
                InputSchema = new JObject()
            };
        }
        
        private McpToolResult ExecuteOnMainThread(Func<McpToolResult> func)
        {
            // 在编辑器模式下，检查当前线程
            bool isMainThread = UnityEditorInternal.InternalEditorUtility.CurrentThreadIsMainThread();

            if (isMainThread)
            {
                // 如果已经在主线程，直接执行
                return func();
            }

            IMainThreadDispatcher dispatcher = null;
            try
            {
                //打印日志记录当前线程id
                McpLogger.LogDebug($"🔧 ExecuteOnMainThread 初始化 dispatcher = MainThreadDispatcher.Instance; - 线程ID: {System.Threading.Thread.CurrentThread.ManagedThreadId}");
                dispatcher = MainThreadDispatcher.Instance;
                //打印日志记录MainThreadDispatcher实例
                // UnityEngine.Debug.Log($"[McpServer] ExecuteOnMainThread MainThreadDispatcher.Instance: {dispatcher}");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"MainThreadDispatcher initialization failed: {ex.Message}", ex);
            }

            // 如果MainThreadDispatcher还未初始化，等待并重试
            int retryCount = 0;
            const int maxRetries = 10;
            const int retryDelayMs = 200;

            while (dispatcher == null && retryCount < maxRetries)
            {
                System.Threading.Thread.Sleep(retryDelayMs);
                try
                {
                    dispatcher = MainThreadDispatcher.Instance;
                }
                catch (Exception ex)
                {
                    if (retryCount == maxRetries - 1) // 最后一次重试
                    {
                        throw new InvalidOperationException($"MainThreadDispatcher initialization failed: {ex.Message}", ex);
                    }
                }
                retryCount++;
            }

            if (dispatcher == null)
            {
                throw new InvalidOperationException($"MainThreadDispatcher failed to initialize after {maxRetries} attempts. Please ensure Unity is running and try again.");
            }

            // 使用EnqueueAndWait简化线程处理逻辑
            try
            {
                //打印当前线程，打印dispatcher类型
                McpLogger.LogDebug($"⚡ ExecuteOnMainThread 执行中 - 当前线程: {System.Threading.Thread.CurrentThread.ManagedThreadId}");
                McpLogger.LogDebug($"🔧 ExecuteOnMainThread 执行中 - dispatcher类型: {dispatcher.GetType()}");
                return dispatcher.EnqueueAndWait(func);
            }
                // catch (ThreadAbortException)
                // {

                //     // 线程正常中止，不需要记录错误
                //     break;
                // }
            catch (Exception ex)
            {
                McpLogger.LogDebug($"🔧 ExecuteOnMainThread 执行失败: {ex.Message}");
                throw;
            }
        }
        
        private McpToolResult ExecuteOnMainThreadAsync(Func<McpToolResult> func, string operationName = "Unknown")
        {
            // 在编辑器模式下，检查当前线程
            bool isMainThread = UnityEditorInternal.InternalEditorUtility.CurrentThreadIsMainThread();

            if (isMainThread)
            {
                // 如果已经在主线程，直接执行
                return func();
            }

            IMainThreadDispatcher dispatcher = null;
            try
            {
                dispatcher = MainThreadDispatcher.Instance;
            }
            catch (Exception ex)
            {
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"MainThreadDispatcher initialization failed: {ex.Message}" }
                    },
                    IsError = true
                };
            }

            if (dispatcher == null)
            {
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = "MainThreadDispatcher failed to initialize" }
                    },
                    IsError = true
                };
            }

            // 异步执行，不等待结果
            try
            {
                McpLogger.LogDebug($"🚀 ExecuteOnMainThreadAsync 异步启动 - 线程ID: {System.Threading.Thread.CurrentThread.ManagedThreadId}");
                //打印dispatcher类型
                McpLogger.LogDebug($"🔧 ExecuteOnMainThreadAsync 异步启动 - dispatcher类型: {dispatcher.GetType()}");
                // 使用Enqueue而不是EnqueueAndWait，实现异步执行
                dispatcher.Enqueue(() => {
                    try
                    {
                        func();
                    }
                    catch (Exception ex)
                    {
                        McpLogger.LogException(ex, "Async execution failed");
                    }
                });
                // 立即返回成功结果，不等待实际执行完成
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        //显示当前是什么操作，并提示大约需要几秒完成操作任务
                        new McpContent { Type = "text", Text = $"操作 '{operationName}' 已加入异步执行队列，请5秒后重试" }
                    }
                };
            }
            catch (Exception ex)
            {
                McpLogger.LogException(ex, $"ExecuteOnMainThreadAsync 操作 '{operationName}' failed");
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Failed to queue async execution: {ex.Message}" }
                    },
                    IsError = true
                };
            }
        }
        
        private McpToolResult ExecutePlayModeWithStatusCheck(JObject args, bool isStartOperation)
        {
            // 先检查当前播放模式状态
            bool isMainThread = UnityEditorInternal.InternalEditorUtility.CurrentThreadIsMainThread();
            
            if (isMainThread)
            {
                // 如果在主线程，直接检查状态
                bool currentlyPlaying = EditorApplication.isPlaying;
                bool shouldSkip = isStartOperation ? currentlyPlaying : !currentlyPlaying;
                
                if (shouldSkip)
                {
                    string message = isStartOperation ? "Play mode is already active" : "Play mode is not active";
                    return new McpToolResult
                    {
                        Content = new List<McpContent>
                        {
                            new McpContent { Type = "text", Text = message }
                        }
                    };
                }
                
                // 执行相应操作
                var operation = isStartOperation ? 
                    (Func<JObject, McpToolResult>)UnityToolsMain.StartPlayMode :
                UnityToolsMain.StopPlayMode;
                string operationName = isStartOperation ? "启动播放模式" : "停止播放模式";
                return ExecuteOnMainThreadAsync(() => operation(args), operationName);
            }
            
            // 如果不在主线程，需要先同步检查状态
            try
            {
                var statusResult = ExecuteOnMainThread(() => UnityToolsMain.GetPlayModeStatus(args));

                // 解析状态结果，检查当前状态
                if (statusResult != null && !statusResult.IsError && statusResult.Content != null)
                {
                    var statusText = statusResult.Content.FirstOrDefault()?.Text ?? "";
                    McpLogger.LogDebug($"📊 播放模式状态检查结果: {statusText}");
                    
                    bool currentlyPlaying = statusText.Contains("Is Playing: True");
                    bool shouldSkip = isStartOperation ? currentlyPlaying : !currentlyPlaying;
                    
                    if (shouldSkip)
                    {
                        string message = isStartOperation ? "Play mode is already active" : "Play mode is not active";
                        return new McpToolResult
                        {
                            Content = new List<McpContent>
                            {
                                new McpContent { Type = "text", Text = message }
                            }
                        };
                    }
                }
                
                // 执行相应操作
                var operation = isStartOperation ? 
                    (Func<JObject, McpToolResult>)UnityToolsMain.StartPlayMode :
                UnityToolsMain.StopPlayMode;
                string operationName = isStartOperation ? "启动播放模式" : "停止播放模式";
                return ExecuteOnMainThreadAsync(() => operation(args), operationName);
            }
            //aborted异常处理
            // catch (ThreadAbortException ex) {
            //     //通过editorapp检测是否在切换场景，如果是切换场景打印info级别，如果是其他情况打印error
            //     if(IsPlayModeChanging()){
            //         UnityEngine.Debug.Log($"[McpServer] Operation {request.HttpMethod} {request.Url.AbsolutePath} 正在切换场景，忽略异常: {ex.Message}\n{ex.StackTrace}");
            //     }else{
            //         UnityEngine.Debug.LogError($"[McpServer] Operation {request.HttpMethod} {request.Url.AbsolutePath} 发生异常: {ex.Message}\n{ex.StackTrace}");
            //     }
            // }
            catch (Exception ex)
            {
                McpLogger.LogDebug($"🔧 播放模式状态检查失败: {ex.Message}");
                // 如果状态检查失败，仍然尝试执行操作
                var operation = isStartOperation ? 
                    (Func<JObject, McpToolResult>)UnityToolsMain.StartPlayMode :
                UnityToolsMain.StopPlayMode;
                string operationName = isStartOperation ? "启动播放模式" : "停止播放模式";
                return ExecuteOnMainThreadAsync(() => operation(args), operationName);
            }
        }
        
        private McpToolResult ExecutePlayModeStartWithStatusCheck(JObject args)
        {
            return ExecutePlayModeWithStatusCheck(args, true);
        }
        
        private McpToolResult ExecutePlayModeStopWithStatusCheck(JObject args)
        {
            return ExecutePlayModeWithStatusCheck(args, false);
        }
        
        private void TrackClientConnection(HttpListenerRequest request)
        {
            var clientKey = request.RemoteEndPoint?.ToString() ?? "unknown";
            var userAgent = request.UserAgent ?? "unknown";
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            
            lock (_clientsLock)
            {
                if (_connectedClients.TryGetValue(clientKey, out var clientInfo))
                {
                    clientInfo.LastSeen = DateTime.Now;
                    clientInfo.RequestCount++;
                    clientInfo.LastMethod = request.HttpMethod;
                    if (clientInfo.UserAgent != userAgent)
                    {
                        clientInfo.UserAgent = userAgent;
                    }
                    
                    // 记录客户端活动
                }
                else
                {
                    _connectedClients[clientKey] = new ClientInfo
                    {
                        RemoteEndPoint = clientKey,
                        UserAgent = userAgent,
                        LastSeen = DateTime.Now,
                        RequestCount = 1,
                        LastMethod = request.HttpMethod
                    };
                    
                    // 记录新客户端连接
                }
                
                // 清理超过5分钟未活动的客户端
                var expiredClients = _connectedClients
                    .Where(kvp => DateTime.Now - kvp.Value.LastSeen > TimeSpan.FromMinutes(5))
                    .Select(kvp => kvp.Key)
                    .ToList();
                    
                foreach (var expiredClient in expiredClients)
                {
                    _connectedClients.Remove(expiredClient);
                }
            }
        }
        
        private void UpdateClientLastMethod(string toolName)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            
            lock (_clientsLock)
            {
                foreach (var clientInfo in _connectedClients.Values)
                {
                    if (DateTime.Now - clientInfo.LastSeen < TimeSpan.FromSeconds(10)) // 最近10秒内活跃的客户端
                    {
                        clientInfo.LastMethod = $"tools/call:{toolName}";
                    }
                }
            }
        }
        
        public void ClearDisconnectedClients()
        {
            lock (_clientsLock)
            {
                var expiredClients = _connectedClients
                    .Where(kvp => DateTime.Now - kvp.Value.LastSeen > TimeSpan.FromMinutes(1))
                    .Select(kvp => kvp.Key)
                    .ToList();
                    
                foreach (var expiredClient in expiredClients)
                {
                    _connectedClients.Remove(expiredClient);
                }
            }
        }
    }
}