using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.MCP;
using Unity.MCP.Editor;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Unity.MCP.Editor
{
    public static class UnityGameLogicTools
    {
        #region Game Logic Management
        
        /// <summary>
        /// 创建游戏管理器
        /// </summary>
        /// <param name="parameters">游戏管理器参数JSON</param>
        /// <returns>操作结果</returns>
        public static string CreateGameManager(string parameters)
        {
            try
            {
#if UNITY_EDITOR
                var paramObj = JObject.Parse(parameters);
                
                // 解析参数
                string managerName = paramObj["name"]?.ToString() ?? "GameManager";
                string managerType = paramObj["type"]?.ToString() ?? "singleton";
                bool persistent = paramObj["persistent"]?.ToObject<bool>() ?? true;
                bool autoInitialize = paramObj["autoInitialize"]?.ToObject<bool>() ?? true;
                
                // 创建游戏管理器脚本内容
                string scriptContent = GenerateGameManagerScript(managerName, managerType, persistent, autoInitialize, paramObj);
                
                // 保存脚本文件
                string scriptPath = $"Assets/Scripts/{managerName}.cs";
                
                // 确保Scripts目录存在
                string scriptsDir = Path.GetDirectoryName(scriptPath);
                if (!Directory.Exists(scriptsDir))
                {
                    Directory.CreateDirectory(scriptsDir);
                }
                
                File.WriteAllText(scriptPath, scriptContent);
                AssetDatabase.Refresh();
                
                // 创建游戏管理器GameObject
                GameObject managerObject = new GameObject(managerName);
                
                // 等待脚本编译完成后添加组件
                EditorApplication.delayCall += () =>
                {
                    try
                    {
                        // 查找脚本类型
                        var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
                        System.Type managerScriptType = null;
                        
                        foreach (var assembly in assemblies)
                        {
                            managerScriptType = assembly.GetType(managerName);
                            if (managerScriptType != null) break;
                        }
                        
                        if (managerScriptType != null)
                        {
                            managerObject.AddComponent(managerScriptType);
                            
                            // 如果是持久化对象，设置DontDestroyOnLoad
                            if (persistent)
                            {
                                UnityEngine.Object.DontDestroyOnLoad(managerObject);
                            }
                            
                            // 记录撤销操作
                            Undo.RegisterCreatedObjectUndo(managerObject, "Create Game Manager");
                            
                            // 选中新创建的游戏管理器
                            Selection.activeGameObject = managerObject;
                            
                            McpLogger.LogTool($"游戏管理器创建成功: {managerName}");
                        }
                    }
                    catch (Exception ex)
                    {
                        McpLogger.LogException(ex, "添加游戏管理器组件时发生错误");
                    }
                };
                
                return $"{{\"success\": true, \"message\": \"游戏管理器 '{managerName}' 创建成功\", \"scriptPath\": \"{scriptPath}\", \"gameObjectId\": \"{managerObject.GetInstanceID()}\"}}";
#else
                return "{\"success\": false, \"message\": \"此功能仅在编辑器模式下可用\"}";
#endif
            }
            catch (Exception ex)
            {
                McpLogger.LogException(ex, "创建游戏管理器时发生错误");
                return $"{{\"success\": false, \"message\": \"创建游戏管理器失败: {ex.Message}\"}}";
            }
        }
        
        /// <summary>
        /// 设置游戏状态
        /// </summary>
        /// <param name="parameters">游戏状态参数JSON</param>
        /// <returns>操作结果</returns>
        public static string SetGameState(string parameters)
        {
            try
            {
                var paramObj = JObject.Parse(parameters);
                
                // 解析参数
                string managerName = paramObj["managerName"]?.ToString() ?? "GameManager";
                string stateName = paramObj["state"]?.ToString();
                var stateData = paramObj["data"];
                
                if (string.IsNullOrEmpty(stateName))
                {
                    return "{\"success\": false, \"message\": \"状态名称不能为空\"}";
                }
                
                // 查找游戏管理器
                GameObject managerObject = GameObject.Find(managerName);
                if (managerObject == null)
                {
                    return $"{{\"success\": false, \"message\": \"未找到游戏管理器: {managerName}\"}}";
                }
                
                // 获取游戏管理器组件
                MonoBehaviour managerComponent = managerObject.GetComponent<MonoBehaviour>();
                if (managerComponent == null)
                {
                    return "{\"success\": false, \"message\": \"游戏管理器组件未找到\"}";
                }
                
                // 使用反射调用SetState方法
                var managerType = managerComponent.GetType();
                var setStateMethod = managerType.GetMethod("SetState");
                
                if (setStateMethod != null)
                {
                    try
                    {
                        if (stateData != null)
                        {
                            setStateMethod.Invoke(managerComponent, new object[] { stateName, stateData.ToString() });
                        }
                        else
                        {
                            setStateMethod.Invoke(managerComponent, new object[] { stateName });
                        }
                    }
                    catch (Exception ex)
                    {
                        return $"{{\"success\": false, \"message\": \"调用SetState方法失败: {ex.Message}\"}}";
                    }
                }
                else
                {
                    // 尝试直接设置字段或属性
                    var stateField = managerType.GetField("currentState");
                    var stateProperty = managerType.GetProperty("CurrentState");
                    
                    if (stateField != null)
                    {
                        stateField.SetValue(managerComponent, stateName);
                    }
                    else if (stateProperty != null && stateProperty.CanWrite)
                    {
                        stateProperty.SetValue(managerComponent, stateName);
                    }
                    else
                    {
                        return "{\"success\": false, \"message\": \"游戏管理器不支持状态设置\"}";
                    }
                }
                
                McpLogger.LogTool($"游戏状态设置成功: {stateName}");
                return $"{{\"success\": true, \"message\": \"游戏状态 '{stateName}' 设置成功\"}}";
            }
            catch (Exception ex)
            {
                McpLogger.LogException(ex, "设置游戏状态时发生错误");
                return $"{{\"success\": false, \"message\": \"设置游戏状态失败: {ex.Message}\"}}";
            }
        }
        
        /// <summary>
        /// 保存游戏数据
        /// </summary>
        /// <param name="parameters">保存参数JSON</param>
        /// <returns>操作结果</returns>
        public static string SaveGameData(string parameters)
        {
            try
            {
                var paramObj = JObject.Parse(parameters);
                
                // 解析参数
                string saveSlot = paramObj["saveSlot"]?.ToString() ?? "default";
                string saveFormat = paramObj["format"]?.ToString() ?? "json";
                var gameData = paramObj["data"];
                string customPath = paramObj["customPath"]?.ToString();
                bool encrypt = paramObj["encrypt"]?.ToObject<bool>() ?? false;
                
                if (gameData == null)
                {
                    return "{\"success\": false, \"message\": \"游戏数据不能为空\"}";
                }
                
                // 确定保存路径
                string savePath;
                if (!string.IsNullOrEmpty(customPath))
                {
                    savePath = customPath;
                }
                else
                {
                    string saveDir = Path.Combine(Application.persistentDataPath, "SaveData");
                    if (!Directory.Exists(saveDir))
                    {
                        Directory.CreateDirectory(saveDir);
                    }
                    
                    string extension = saveFormat.ToLower() == "binary" ? ".dat" : ".json";
                    savePath = Path.Combine(saveDir, $"save_{saveSlot}{extension}");
                }
                
                // 准备保存数据
                GameSaveData saveData = new GameSaveData
                {
                    saveSlot = saveSlot,
                    timestamp = DateTime.Now,
                    gameData = gameData.ToString(),
                    version = Application.version
                };
                
                // 保存数据
                if (saveFormat.ToLower() == "binary")
                {
                    SaveBinaryData(savePath, saveData, encrypt);
                }
                else
                {
                    SaveJsonData(savePath, saveData, encrypt);
                }
                
                McpLogger.LogTool($"游戏数据保存成功: {saveSlot}");
                return $"{{\"success\": true, \"message\": \"游戏数据保存成功\", \"savePath\": \"{savePath}\", \"saveSlot\": \"{saveSlot}\"}}";
            }
            catch (Exception ex)
            {
                McpLogger.LogException(ex, "保存游戏数据时发生错误");
                return $"{{\"success\": false, \"message\": \"保存游戏数据失败: {ex.Message}\"}}";
            }
        }
        
        /// <summary>
        /// 加载游戏数据
        /// </summary>
        /// <param name="parameters">加载参数JSON</param>
        /// <returns>操作结果</returns>
        public static string LoadGameData(string parameters)
        {
            try
            {
                var paramObj = JObject.Parse(parameters);
                
                // 解析参数
                string saveSlot = paramObj["saveSlot"]?.ToString() ?? "default";
                string saveFormat = paramObj["format"]?.ToString() ?? "json";
                string customPath = paramObj["customPath"]?.ToString();
                bool decrypt = paramObj["decrypt"]?.ToObject<bool>() ?? false;
                
                // 确定加载路径
                string loadPath;
                if (!string.IsNullOrEmpty(customPath))
                {
                    loadPath = customPath;
                }
                else
                {
                    string saveDir = Path.Combine(Application.persistentDataPath, "SaveData");
                    string extension = saveFormat.ToLower() == "binary" ? ".dat" : ".json";
                    loadPath = Path.Combine(saveDir, $"save_{saveSlot}{extension}");
                }
                
                // 检查文件是否存在
                if (!File.Exists(loadPath))
                {
                    return $"{{\"success\": false, \"message\": \"保存文件不存在: {loadPath}\"}}";
                }
                
                // 加载数据
                GameSaveData saveData;
                if (saveFormat.ToLower() == "binary")
                {
                    saveData = LoadBinaryData(loadPath, decrypt);
                }
                else
                {
                    saveData = LoadJsonData(loadPath, decrypt);
                }
                
                if (saveData == null)
                {
                    return "{\"success\": false, \"message\": \"加载游戏数据失败\"}";
                }
                
                McpLogger.LogTool($"游戏数据加载成功: {saveSlot}");
                
                // 返回加载的数据
                var result = new
                {
                    success = true,
                    message = "游戏数据加载成功",
                    saveSlot = saveData.saveSlot,
                    timestamp = saveData.timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                    version = saveData.version,
                    data = JObject.Parse(saveData.gameData)
                };
                
                return JsonConvert.SerializeObject(result);
            }
            catch (Exception ex)
            {
                McpLogger.LogException(ex, "加载游戏数据时发生错误");
                return $"{{\"success\": false, \"message\": \"加载游戏数据失败: {ex.Message}\"}}";
            }
        }
        
        /// <summary>
        /// 获取游戏状态信息
        /// </summary>
        /// <param name="parameters">查询参数JSON</param>
        /// <returns>操作结果</returns>
        public static string GetGameState(string parameters)
        {
            try
            {
                var paramObj = JObject.Parse(parameters);
                
                // 解析参数
                string managerName = paramObj["managerName"]?.ToString() ?? "GameManager";
                
                // 查找游戏管理器
                GameObject managerObject = GameObject.Find(managerName);
                if (managerObject == null)
                {
                    return $"{{\"success\": false, \"message\": \"未找到游戏管理器: {managerName}\"}}";
                }
                
                // 获取游戏管理器组件
                MonoBehaviour managerComponent = managerObject.GetComponent<MonoBehaviour>();
                if (managerComponent == null)
                {
                    return "{\"success\": false, \"message\": \"游戏管理器组件未找到\"}";
                }
                
                // 获取状态信息
                var managerType = managerComponent.GetType();
                string currentState = "Unknown";
                
                // 尝试获取状态
                var getStateMethod = managerType.GetMethod("GetState");
                if (getStateMethod != null)
                {
                    try
                    {
                        var state = getStateMethod.Invoke(managerComponent, null);
                        currentState = state?.ToString() ?? "Unknown";
                    }
                    catch { }
                }
                else
                {
                    var stateField = managerType.GetField("currentState");
                    var stateProperty = managerType.GetProperty("CurrentState");
                    
                    if (stateField != null)
                    {
                        currentState = stateField.GetValue(managerComponent)?.ToString() ?? "Unknown";
                    }
                    else if (stateProperty != null && stateProperty.CanRead)
                    {
                        currentState = stateProperty.GetValue(managerComponent)?.ToString() ?? "Unknown";
                    }
                }
                
                // 获取其他信息
                var gameStateInfo = new
                {
                    success = true,
                    managerName = managerName,
                    currentState = currentState,
                    isActive = managerObject.activeInHierarchy,
                    isPersistent = managerObject.scene.name == "DontDestroyOnLoad",
                    componentType = managerType.Name
                };
                
                return JsonConvert.SerializeObject(gameStateInfo);
            }
            catch (Exception ex)
            {
                McpLogger.LogException(ex, "获取游戏状态时发生错误");
                return $"{{\"success\": false, \"message\": \"获取游戏状态失败: {ex.Message}\"}}";
            }
        }
        
        #endregion
        
        #region Private Helper Methods
        
        private static string GenerateGameManagerScript(string managerName, string managerType, bool persistent, bool autoInitialize, JObject paramObj)
        {
            // 从模板文件读取脚本内容
            string templatePath = Path.Combine(Application.dataPath, "Tools", "Editor", "Templates", "GameManagerTemplate.txt");
            
            if (!File.Exists(templatePath))
            {
                // 如果在Assets目录找不到，尝试在当前脚本目录查找
                string currentDir = Path.GetDirectoryName(typeof(UnityGameLogicTools).Assembly.Location);
                templatePath = Path.Combine(currentDir, "Templates", "GameManagerTemplate.txt");
            }
            
            string scriptContent;
            if (File.Exists(templatePath))
            {
                scriptContent = File.ReadAllText(templatePath);
            }
            else
            {
                // 使用内置模板
                scriptContent = GetDefaultGameManagerTemplate();
            }
            
            // 替换模板中的占位符
            scriptContent = scriptContent.Replace("{MANAGER_NAME}", managerName);
            
            // 处理单例实例
            string singletonInstance = managerType == "singleton" ? 
                $"public static {managerName} Instance {{ get; private set; }}" : "";
            scriptContent = scriptContent.Replace("{SINGLETON_INSTANCE}", singletonInstance);
            
            // 处理单例实现
            string singletonImplementation = "";
            if (managerType == "singleton")
            {
                singletonImplementation = "        // 单例模式实现\n" +
                    "        if (Instance == null)\n" +
                    "        {\n" +
                    "            Instance = this;\n" +
                    (persistent ? "            DontDestroyOnLoad(gameObject);\n" : "") +
                    "        }\n" +
                    "        else\n" +
                    "        {\n" +
                    "            Destroy(gameObject);\n" +
                    "            return;\n" +
                    "        }";
            }
            scriptContent = scriptContent.Replace("{SINGLETON_IMPLEMENTATION}", singletonImplementation);
            
            // 处理自动初始化
            string autoInit = autoInitialize ? "        Initialize();" : "";
            scriptContent = scriptContent.Replace("{AUTO_INITIALIZE}", autoInit);
            
            return scriptContent;
        }
        
        private static void SaveJsonData(string path, GameSaveData saveData, bool encrypt)
        {
            string jsonData = JsonConvert.SerializeObject(saveData, Formatting.Indented);
            
            if (encrypt)
            {
                jsonData = SimpleEncrypt(jsonData);
            }
            
            File.WriteAllText(path, jsonData);
        }
        
        private static void SaveBinaryData(string path, GameSaveData saveData, bool encrypt)
        {
            string jsonData = JsonConvert.SerializeObject(saveData);
            
            if (encrypt)
            {
                jsonData = SimpleEncrypt(jsonData);
            }
            
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(jsonData);
            File.WriteAllBytes(path, bytes);
        }
        
        private static GameSaveData LoadJsonData(string path, bool decrypt)
        {
            string jsonData = File.ReadAllText(path);
            
            if (decrypt)
            {
                jsonData = SimpleDecrypt(jsonData);
            }
            
            return JsonConvert.DeserializeObject<GameSaveData>(jsonData);
        }
        
        private static GameSaveData LoadBinaryData(string path, bool decrypt)
        {
            byte[] bytes = File.ReadAllBytes(path);
            string jsonData = System.Text.Encoding.UTF8.GetString(bytes);
            
            if (decrypt)
            {
                jsonData = SimpleDecrypt(jsonData);
            }
            
            return JsonConvert.DeserializeObject<GameSaveData>(jsonData);
        }
        
        private static string SimpleEncrypt(string text)
        {
            // 简单的XOR加密
            byte[] data = System.Text.Encoding.UTF8.GetBytes(text);
            byte key = 0x5A; // 简单密钥
            
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = (byte)(data[i] ^ key);
            }
            
            return Convert.ToBase64String(data);
        }
        
        private static string SimpleDecrypt(string encryptedText)
        {
            // 简单的XOR解密
            byte[] data = Convert.FromBase64String(encryptedText);
            byte key = 0x5A; // 简单密钥
            
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = (byte)(data[i] ^ key);
            }
            
            return System.Text.Encoding.UTF8.GetString(data);
        }
        
        private static string GetDefaultGameManagerTemplate()
        {
            return @"using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class {MANAGER_NAME} : MonoBehaviour
{
    // 单例实例
{SINGLETON_INSTANCE}

    // 游戏状态
    [SerializeField]
    private string currentState = ""Initialized"";
    
    // 游戏数据
    [SerializeField]
    private Dictionary<string, object> gameData = new Dictionary<string, object>();
    
    // 状态变化事件
    public event System.Action<string, string> OnStateChanged;
    
    // 游戏数据变化事件
    public event System.Action<string, object> OnDataChanged;
    
    public string CurrentState => currentState;
    
    private void Awake()
    {
{SINGLETON_IMPLEMENTATION}
        
{AUTO_INITIALIZE}
    }
    
    private void Initialize()
    {
        Debug.Log($""{MANAGER_NAME} 初始化完成"");
        
        // 初始化游戏数据
        InitializeGameData();
    }
    
    private void InitializeGameData()
    {
        // 设置默认游戏数据
        SetData(""playerLevel"", 1);
        SetData(""playerScore"", 0);
        SetData(""gameStartTime"", DateTime.Now.ToString());
    }
    
    /// <summary>
    /// 设置游戏状态
    /// </summary>
    /// <param name=""newState"">新状态</param>
    /// <param name=""stateData"">状态数据</param>
    public void SetState(string newState, string stateData = null)
    {
        if (string.IsNullOrEmpty(newState))
        {
            return;
        }
        
        string previousState = currentState;
        currentState = newState;
        
        // 解析状态数据
        if (!string.IsNullOrEmpty(stateData))
        {
            try
            {
                var data = JObject.Parse(stateData);
                foreach (var kvp in data)
                {
                    SetData(kvp.Key, kvp.Value.ToObject<object>());
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($""解析状态数据失败: {ex.Message}"");
            }
        }
        
        // 触发状态变化事件
        OnStateChanged?.Invoke(previousState, currentState);
        
        // 处理状态变化
        HandleStateChange(previousState, currentState);
        
        Debug.Log($""游戏状态变化: {previousState} -> {currentState}"");
    }
    
    /// <summary>
    /// 获取当前游戏状态
    /// </summary>
    /// <returns>当前状态</returns>
    public string GetState()
    {
        return currentState;
    }
    
    /// <summary>
    /// 设置游戏数据
    /// </summary>
    /// <param name=""key"">数据键</param>
    /// <param name=""value"">数据值</param>
    public void SetData(string key, object value)
    {
        if (string.IsNullOrEmpty(key))
        {
            Debug.LogError(""数据键不能为空"");
            return;
        }
        
        gameData[key] = value;
        OnDataChanged?.Invoke(key, value);
        
        Debug.Log($""游戏数据更新: {key} = {value}"");
    }
    
    /// <summary>
    /// 获取游戏数据
    /// </summary>
    /// <param name=""key"">数据键</param>
    /// <returns>数据值</returns>
    public object GetData(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            Debug.LogError(""数据键不能为空"");
            return null;
        }
        
        return gameData.ContainsKey(key) ? gameData[key] : null;
    }
    
    /// <summary>
    /// 获取所有游戏数据
    /// </summary>
    /// <returns>所有数据的字典</returns>
    public Dictionary<string, object> GetAllData()
    {
        return new Dictionary<string, object>(gameData);
    }
    
    /// <summary>
    /// 处理状态变化
    /// </summary>
    /// <param name=""previousState"">之前的状态</param>
    /// <param name=""newState"">新状态</param>
    private void HandleStateChange(string previousState, string newState)
    {
        switch (newState)
        {
            case ""GameStart"":
                OnGameStart();
                break;
            case ""GamePause"":
                OnGamePause();
                break;
            case ""GameOver"":
                OnGameOver();
                break;
            case ""ReturnToMenu"":
                OnReturnToMenu();
                break;
            default:
                break;
        }
    }
    
    private void OnGameStart()
    {
        Debug.Log(""游戏开始"");
        SetData(""gameStartTime"", DateTime.Now.ToString());
    }
    
    private void OnGamePause()
    {
        Debug.Log(""游戏暂停"");
        Time.timeScale = 0f;
    }
    
    private void OnGameOver()
    {
        Debug.Log(""游戏结束"");
        SetData(""gameEndTime"", DateTime.Now.ToString());
        Time.timeScale = 1f;
    }
    
    private void OnReturnToMenu()
    {
        Debug.Log(""返回菜单"");
        Time.timeScale = 1f;
    }
    
    // 保存游戏数据到PlayerPrefs
    public void SaveToPlayerPrefs()
    {
        try
        {
            string jsonData = JsonConvert.SerializeObject(gameData);
            PlayerPrefs.SetString($""{MANAGER_NAME}_GameData"", jsonData);
            PlayerPrefs.SetString($""{MANAGER_NAME}_CurrentState"", currentState);
            PlayerPrefs.Save();
            Debug.Log(""游戏数据已保存到PlayerPrefs"");
        }
        catch (Exception ex)
        {
            Debug.LogError($""保存游戏数据失败: {ex.Message}"");
        }
    }
    
    // 从PlayerPrefs加载游戏数据
    public void LoadFromPlayerPrefs()
    {
        try
        {
            if (PlayerPrefs.HasKey($""{MANAGER_NAME}_GameData""))
            {
                string jsonData = PlayerPrefs.GetString($""{MANAGER_NAME}_GameData"");
                gameData = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonData) ?? new Dictionary<string, object>();
            }
            
            if (PlayerPrefs.HasKey($""{MANAGER_NAME}_CurrentState""))
            {
                currentState = PlayerPrefs.GetString($""{MANAGER_NAME}_CurrentState"");
            }
            
            Debug.Log(""游戏数据已从PlayerPrefs加载"");
        }
        catch (Exception ex)
        {
            Debug.LogError($""加载游戏数据失败: {ex.Message}"");
            InitializeGameData();
        }
    }
}";
        }
        
        #endregion
    }
    
    #region Helper Classes
    
    [System.Serializable]
    public class GameSaveData
    {
        public string saveSlot;
        public DateTime timestamp;
        public string gameData;
        public string version;
    }
    
    #endregion
}