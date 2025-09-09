using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.MCP;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Unity.MCP.Editor
{
    /// <summary>
    /// Unity编辑器日志查询工具
    /// </summary>
    public static class UnityLogTools
    {
        /// <summary>
        /// 获取Unity编辑器Console日志
        /// </summary>
        /// <param name="parameters">查询参数JSON</param>
        /// <returns>操作结果</returns>
        public static string GetUnityLogs(string parameters)
        {
            try
            {
#if UNITY_EDITOR
                var paramObj = JObject.Parse(parameters);
                
                // 解析参数
                int maxCount = paramObj["maxCount"]?.ToObject<int>() ?? 100;
                string logLevel = paramObj["logLevel"]?.ToString() ?? "all"; // all, info, warning, error
                bool includeStackTrace = paramObj["includeStackTrace"]?.ToObject<bool>() ?? false;
                string searchText = paramObj["searchText"]?.ToString() ?? "";
                
                McpLogger.LogDebug($"GetUnityLogs调用参数: maxCount={maxCount}, logLevel={logLevel}, includeStackTrace={includeStackTrace}, searchText='{searchText}'");
                
                var logs = new List<object>();
                
                // 使用反射获取LogEntries类
                var logEntriesType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.LogEntries");
                if (logEntriesType == null)
                {
                    McpLogger.LogDebug("无法找到UnityEditor.LogEntries类型");
                    return "{\"success\": false, \"message\": \"无法访问Unity日志系统\"}";
                }
                
                // 尝试获取Console窗口类
                var consoleWindowType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.ConsoleWindow");
                McpLogger.LogDebug($"Console窗口类型: {consoleWindowType?.Name ?? "未找到"}");
                
                // 获取日志条目数量
                var getCountMethod = logEntriesType.GetMethod("GetCount", BindingFlags.Static | BindingFlags.Public);
                
                // 获取日志条目方法
                var getEntryInternalMethod = logEntriesType.GetMethod("GetEntryInternal", BindingFlags.Static | BindingFlags.Public);
                
                if (getCountMethod == null)
                {
                    McpLogger.LogDebug("无法找到GetCount方法");
                    return "{\"success\": false, \"message\": \"无法获取日志数量\"}";
                }
                
                // 强制刷新Console以确保日志计数正确
                EditorApplication.delayCall += () => { };
                
                int totalCount = (int)getCountMethod.Invoke(null, null);
                McpLogger.LogDebug($"Unity Console总日志数量: {totalCount}");
                
                // 再次检查日志数量（有时需要延迟获取）
                if (totalCount == 0)
                {
                    // 等待一帧后重新获取
                    EditorApplication.delayCall += () => {
                        int retryCount = (int)getCountMethod.Invoke(null, null);
                        McpLogger.LogDebug($"重试获取日志数量: {retryCount}");
                    };
                    
                    totalCount = (int)getCountMethod.Invoke(null, null);
                    McpLogger.LogDebug($"最终日志数量: {totalCount}");
                }
                
                // 如果仍然没有日志，返回调试信息
                if (totalCount == 0)
                {
                    var emptyResult = new
                    {
                        success = true,
                        totalCount = 0,
                        returnedCount = 0,
                        logs = new List<object>(),
                        timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                        message = "Unity Console中暂无日志",
                        debug = new {
                            logEntriesTypeFound = logEntriesType != null,
                            getCountMethodFound = getCountMethod != null,
                            getEntryInternalMethodFound = getEntryInternalMethod != null
                        }
                    };
                    return JsonConvert.SerializeObject(emptyResult, Formatting.Indented);
                }
                if (getEntryInternalMethod == null)
                {
                    McpLogger.LogDebug("无法找到GetEntryInternal方法");
                    return "{\"success\": false, \"message\": \"无法获取日志条目\"}";
                }
                
                // 限制获取的日志数量
                int startIndex = Math.Max(0, totalCount - maxCount);
                int endIndex = totalCount;
                McpLogger.LogDebug($"准备获取日志范围: {startIndex} - {endIndex}");
                
                for (int i = startIndex; i < endIndex; i++)
                {
                    try
                    {
                        // 尝试不同的方法获取日志条目
                        string message = "";
                        string file = "";
                        int line = 0;
                        int mode = 0;
                        int instanceID = 0;
                        
                        // 尝试多种方法获取日志内容
                        bool success = false;
                        
                        // 方法1: 使用简化的GetEntryInternal调用
                        try
                        {
                            // 尝试找到正确的GetEntryInternal方法
                            var methods = logEntriesType.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                                .Where(m => m.Name == "GetEntryInternal")
                                .ToArray();
                            
                            McpLogger.LogDebug($"找到 {methods.Length} 个GetEntryInternal方法");
                            
                            // 优先尝试形如 GetEntryInternal(int, <LogEntry|object|ref LogEntry>) 的双参数重载
                            try
                            {
                                var twoParamCandidates = methods
                                    .Select(m => new { m, ps = m.GetParameters() })
                                    .Where(x => x.ps.Length == 2 && x.ps[0].ParameterType == typeof(int))
                                    .ToArray();

                                foreach (var cand in twoParamCandidates)
                                {
                                    if (success) break;
                                    var method = cand.m;
                                    var methodParams = cand.ps;
                                    var secondType = methodParams[1].ParameterType;

                                    McpLogger.LogDebug($"尝试双参重载: {method.Name}({methodParams[0].ParameterType.Name}, {methodParams[1].ParameterType.Name})");

                                    try
                                    {
                                        object[] args2 = new object[2];
                                        args2[0] = i;

                                        // 准备第二个参数实例
                                        if (secondType.IsByRef)
                                        {
                                            var elementType = secondType.GetElementType();
                                            args2[1] = Activator.CreateInstance(elementType);
                                        }
                                        else if (secondType == typeof(object))
                                        {
                                            var logEntryType2 = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.LogEntry");
                                            args2[1] = logEntryType2 != null ? Activator.CreateInstance(logEntryType2) : new object();
                                        }
                                        else
                                        {
                                            args2[1] = Activator.CreateInstance(secondType);
                                        }

                                        McpLogger.LogDebug($"准备调用双参重载，参数值: [0:int={i}, 1:{(args2[1]?.GetType().Name ?? "null")}]");
                                        method.Invoke(null, args2);

                                        var entryObj = args2[1];
                                        if (entryObj != null)
                                        {
                                            var entryType = entryObj.GetType();
                                            var messageField = entryType.GetField("message", BindingFlags.Public | BindingFlags.Instance) ??
                                                               entryType.GetField("condition", BindingFlags.Public | BindingFlags.Instance);
                                            var fileField = entryType.GetField("file", BindingFlags.Public | BindingFlags.Instance);
                                            var lineField = entryType.GetField("line", BindingFlags.Public | BindingFlags.Instance);
                                            var modeField = entryType.GetField("mode", BindingFlags.Public | BindingFlags.Instance);
                                            var instanceIdField = entryType.GetField("instanceID", BindingFlags.Public | BindingFlags.Instance) ??
                                                                  entryType.GetField("instanceId", BindingFlags.Public | BindingFlags.Instance);

                                            if (messageField != null)
                                            {
                                                message = messageField.GetValue(entryObj)?.ToString() ?? "";
                                                if (fileField != null) file = fileField.GetValue(entryObj)?.ToString() ?? "";
                                                if (lineField != null) line = Convert.ToInt32(lineField.GetValue(entryObj) ?? 0);
                                                if (modeField != null) mode = Convert.ToInt32(modeField.GetValue(entryObj) ?? 0);
                                                if (instanceIdField != null) instanceID = Convert.ToInt32(instanceIdField.GetValue(entryObj) ?? 0);

                                                if (!string.IsNullOrEmpty(message))
                                                {
                                                    success = true;
                                                    McpLogger.LogDebug($"双参重载成功获取日志: {message}");
                                                }
                                            }
                                        }
                                    }
                                    catch (Exception ex2)
                                    {
                                        McpLogger.LogDebug($"双参重载调用失败: {ex2.Message}");
                                    }
                                }
                            }
                            catch (Exception preTryEx)
                            {
                                McpLogger.LogDebug($"双参重载预检失败: {preTryEx.Message}");
                            }
                            
                            foreach (var method in methods)
                            {
                                if (success) break; // 若已成功，跳过后续尝试
                                var methodParams = method.GetParameters();
                                McpLogger.LogDebug($"方法签名: {method.Name}({string.Join(", ", methodParams.Select(p => $"{p.ParameterType.Name} {p.Name}"))})");
                                McpLogger.LogDebug($"参数详情: {string.Join(", ", methodParams.Select(p => $"{p.ParameterType.FullName} {(p.ParameterType.IsByRef ? "(ref)" : "")}"))}");

                                // 仅尝试双参数重载，其它重载直接跳过以避免参数不匹配
                                if (methodParams.Length != 2)
                                {
                                    McpLogger.LogDebug($"跳过非双参重载 (参数数量: {methodParams.Length}) 以避免参数不匹配");
                                    continue;
                                }
                                
                                // 尝试使用不同的方法签名
                                if (methodParams.Length >= 1 && methodParams[0].ParameterType == typeof(int))
                                {
                                    try
                                    {
                                        McpLogger.LogDebug($"尝试调用方法，参数数量: {methodParams.Length}");
                                        // 根据参数数量创建对应的参数数组
                                        object[] args = new object[methodParams.Length];
                                        args[0] = i; // 第一个参数是索引
                                        
                                        // 其余参数初始化为null或默认值
                                        for (int j = 1; j < methodParams.Length; j++)
                                        {
                                            var paramType = methodParams[j].ParameterType;
                                            if (paramType.IsByRef)
                                            {
                                                // 对于引用参数，获取元素类型并创建默认值
                                                var elementType = paramType.GetElementType();
                                                if (elementType == typeof(string))
                                                {
                                                    args[j] = "";
                                                }
                                                else if (elementType == typeof(int))
                                                {
                                                    args[j] = 0;
                                                }
                                                else if (elementType.IsValueType)
                                                {
                                                    args[j] = Activator.CreateInstance(elementType);
                                                }
                                                else
                                                {
                                                    args[j] = null;
                                                }
                                            }
                                            else if (paramType == typeof(int))
                                            {
                                                args[j] = 0;
                                            }
                                            else if (paramType == typeof(string))
                                            {
                                                args[j] = "";
                                            }
                                            else if (paramType.IsValueType)
                                            {
                                                args[j] = Activator.CreateInstance(paramType);
                                            }
                                            else
                                            {
                                                args[j] = null;
                                            }
                                        }
                                        
                                        McpLogger.LogDebug($"准备调用方法，参数值: [{string.Join(", ", args.Select((arg, idx) => $"{idx}:{arg?.GetType()?.Name ?? "null"}={arg}"))}]");
                                        
                                        method.Invoke(null, args);
                                        
                                        McpLogger.LogDebug($"方法调用成功，返回参数: [{string.Join(", ", args.Select((arg, idx) => $"{idx}:{arg?.GetType()?.Name ?? "null"}={arg}"))}]");
                                        
                                        // 尝试从返回的参数中提取信息
                                        if (methodParams.Length >= 2 && args[1] != null)
                                        {
                                            message = args[1].ToString();
                                        }
                                        if (methodParams.Length >= 3 && args[2] != null)
                                        {
                                            file = args[2].ToString();
                                        }
                                        if (methodParams.Length >= 4 && args[3] != null)
                                        {
                                            line = Convert.ToInt32(args[3]);
                                        }
                                        if (methodParams.Length >= 5 && args[4] != null)
                                        {
                                            mode = Convert.ToInt32(args[4]);
                                        }
                                        if (methodParams.Length >= 6 && args[5] != null)
                                        {
                                            instanceID = Convert.ToInt32(args[5]);
                                        }
                                        
                                        if (!string.IsNullOrEmpty(message))
                                        {
                                            success = true;
                                            McpLogger.LogDebug($"成功获取日志: {message}");
                                            break;
                                        }
                                    }
                                    catch (Exception methodEx)
                                    {
                                        McpLogger.LogDebug($"方法调用失败 (参数数量: {methodParams.Length}): {methodEx.Message}");
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            McpLogger.LogDebug($"方法1失败: {ex.Message}");
                        }
                        
                        // 方法2: 如果方法1失败，尝试使用LogEntry结构体
                        if (!success)
                        {
                            try
                            {
                                var logEntryType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.LogEntry");
                                if (logEntryType != null)
                                {
                                    var getEntryMethod2 = logEntriesType.GetMethod("GetEntryInternal", 
                                        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                                        null,
                                        new Type[] { typeof(int), logEntryType },
                                        null);
                                    
                                    if (getEntryMethod2 != null)
                                    {
                                        var logEntry = Activator.CreateInstance(logEntryType);
                                        getEntryMethod2.Invoke(null, new object[] { i, logEntry });
                                        
                                        // 获取LogEntry的字段
                                        var messageField = logEntryType.GetField("message", BindingFlags.Public | BindingFlags.Instance);
                                        var fileField = logEntryType.GetField("file", BindingFlags.Public | BindingFlags.Instance);
                                        var lineField = logEntryType.GetField("line", BindingFlags.Public | BindingFlags.Instance);
                                        var modeField = logEntryType.GetField("mode", BindingFlags.Public | BindingFlags.Instance);
                                        
                                        if (messageField != null)
                                        {
                                            message = messageField.GetValue(logEntry)?.ToString() ?? "";
                                            if (fileField != null) file = fileField.GetValue(logEntry)?.ToString() ?? "";
                                            if (lineField != null) line = Convert.ToInt32(lineField.GetValue(logEntry) ?? 0);
                                            if (modeField != null) mode = Convert.ToInt32(modeField.GetValue(logEntry) ?? 0);
                                            
                                            if (!string.IsNullOrEmpty(message))
                                            {
                                                success = true;
                                            }
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                McpLogger.LogDebug($"方法2失败: {ex.Message}");
                            }
                        }
                        
                        McpLogger.LogDebug($"获取到日志条目 {i}: message='{message}', mode={mode}, file='{file}', line={line}");
                        
                        // 检查消息是否为空
                        if (string.IsNullOrEmpty(message))
                        {
                            McpLogger.LogDebug($"日志条目 {i} 的消息为空，跳过");
                            continue;
                        }
                        
                        // 解析日志级别
                        string level = "Info";
                        if ((mode & 2) != 0) level = "Warning";
                        if ((mode & 4) != 0) level = "Error";
                        
                        McpLogger.LogDebug($"日志条目 {i} 级别: {level}, 过滤条件: {logLevel}");
                        
                        // 过滤日志级别
                        if (logLevel != "all")
                        {
                            if (logLevel.ToLower() == "info" && level != "Info") {
                                McpLogger.LogDebug($"日志条目 {i} 被级别过滤器排除 (需要info，实际{level})");
                                continue;
                            }
                            if (logLevel.ToLower() == "warning" && level != "Warning") {
                                McpLogger.LogDebug($"日志条目 {i} 被级别过滤器排除 (需要warning，实际{level})");
                                continue;
                            }
                            if (logLevel.ToLower() == "error" && level != "Error") {
                                McpLogger.LogDebug($"日志条目 {i} 被级别过滤器排除 (需要error，实际{level})");
                                continue;
                            }
                        }
                        
                        // 搜索文本过滤
                        if (!string.IsNullOrEmpty(searchText) && 
                            !message.ToLower().Contains(searchText.ToLower()))
                        {
                            McpLogger.LogDebug($"日志条目 {i} 被搜索过滤器排除 (搜索'{searchText}'，消息'{message}')");
                            continue;
                        }
                        
                        McpLogger.LogDebug($"日志条目 {i} 通过所有过滤器，将添加到结果中");
                        
                        var logInfo = new
                        {
                            index = i,
                            message = message,
                            level = level,
                            file = file,
                            line = line,
                            timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                            stackTrace = (includeStackTrace && !string.IsNullOrEmpty(file)) ? $"at {file}:{line}" : ""
                        };
                        
                        logs.Add(logInfo);
                    }
                    catch (Exception ex)
                    {
                        McpLogger.LogDebug($"获取日志条目 {i} 时出错: {ex.Message}");
                        continue;
                    }
                }
                
                var result = new
                {
                    success = true,
                    totalCount = totalCount,
                    returnedCount = logs.Count,
                    logs = logs,
                    timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")
                };
                
                McpLogger.LogTool($"🔍 获取Unity编辑器日志成功，共 {logs.Count} 条日志");
                return JsonConvert.SerializeObject(result, Formatting.Indented);
#else
                return "{\"success\": false, \"message\": \"此功能仅在编辑器模式下可用\"}";
#endif
            }
            catch (Exception ex)
            {
                McpLogger.LogException(ex, "获取Unity编辑器日志时发生错误");
                return $"{{\"success\": false, \"message\": \"获取日志失败: {ex.Message}\"}}";
            }
        }
        
        /// <summary>
        /// 清空Unity编辑器Console日志
        /// </summary>
        /// <param name="parameters">参数JSON</param>
        /// <returns>操作结果</returns>
        public static string ClearUnityLogs(string parameters)
        {
            try
            {
#if UNITY_EDITOR
                // 使用反射获取LogEntries类
                var logEntriesType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.LogEntries");
                if (logEntriesType == null)
                {
                    return "{\"success\": false, \"message\": \"无法访问Unity日志系统\"}";
                }
                
                // 获取Clear方法
                var clearMethod = logEntriesType.GetMethod("Clear", BindingFlags.Static | BindingFlags.Public);
                if (clearMethod == null)
                {
                    return "{\"success\": false, \"message\": \"无法找到清空日志方法\"}";
                }
                
                // 调用Clear方法
                clearMethod.Invoke(null, null);
                
                McpLogger.LogTool("🧹 Unity编辑器Console日志已清空");
                return "{\"success\": true, \"message\": \"Unity编辑器Console日志已清空\"}";
#else
                return "{\"success\": false, \"message\": \"此功能仅在编辑器模式下可用\"}";
#endif
            }
            catch (Exception ex)
            {
                McpLogger.LogException(ex, "清空Unity编辑器日志时发生错误");
                return $"{{\"success\": false, \"message\": \"清空日志失败: {ex.Message}\"}}";
            }
        }
        
        /// <summary>
        /// 获取Unity编辑器日志统计信息
        /// </summary>
        /// <param name="parameters">参数JSON</param>
        /// <returns>操作结果</returns>
        public static string GetUnityLogStats(string parameters)
        {
            try
            {
#if UNITY_EDITOR
                // 使用反射获取LogEntries类
                var logEntriesType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.LogEntries");
                if (logEntriesType == null)
                {
                    return "{\"success\": false, \"message\": \"无法访问Unity日志系统\"}";
                }
                
                // 获取日志条目数量
                var getCountMethod = logEntriesType.GetMethod("GetCount", BindingFlags.Static | BindingFlags.Public);
                if (getCountMethod == null)
                {
                    return "{\"success\": false, \"message\": \"无法获取日志数量\"}";
                }
                
                int totalCount = (int)getCountMethod.Invoke(null, null);
                
                // 统计不同级别的日志数量
                int infoCount = 0;
                int warningCount = 0;
                int errorCount = 0;
                
                var getEntryInternalMethod = logEntriesType.GetMethod("GetEntryInternal", BindingFlags.Static | BindingFlags.Public);
                if (getEntryInternalMethod != null)
                {
                    for (int i = 0; i < totalCount; i++)
                    {
                        try
                        {
                            var parameters_array = new object[] { i, "", "", 0, 0, 0 };
                            getEntryInternalMethod.Invoke(null, parameters_array);
                            
                            int mode = (int)(parameters_array[4] ?? 0);
                            
                            if ((mode & 4) != 0) errorCount++;
                            else if ((mode & 2) != 0) warningCount++;
                            else infoCount++;
                        }
                        catch
                        {
                            continue;
                        }
                    }
                }
                
                var result = new
                {
                    success = true,
                    totalCount = totalCount,
                    infoCount = infoCount,
                    warningCount = warningCount,
                    errorCount = errorCount,
                    timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")
                };
                
                McpLogger.LogTool($"📊 Unity编辑器日志统计: 总计{totalCount}条 (信息:{infoCount}, 警告:{warningCount}, 错误:{errorCount})");
                return JsonConvert.SerializeObject(result, Formatting.Indented);
#else
                return "{\"success\": false, \"message\": \"此功能仅在编辑器模式下可用\"}";
#endif
            }
            catch (Exception ex)
            {
                McpLogger.LogException(ex, "获取Unity编辑器日志统计时发生错误");
                return $"{{\"success\": false, \"message\": \"获取日志统计失败: {ex.Message}\"}}";
            }
        }
    }
}