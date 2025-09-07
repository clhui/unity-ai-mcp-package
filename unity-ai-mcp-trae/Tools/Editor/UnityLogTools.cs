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
                
                var logs = new List<object>();
                
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
                
                // 获取日志条目
                var getEntryInternalMethod = logEntriesType.GetMethod("GetEntryInternal", BindingFlags.Static | BindingFlags.Public);
                if (getEntryInternalMethod == null)
                {
                    return "{\"success\": false, \"message\": \"无法获取日志条目\"}";
                }
                
                // 限制获取的日志数量
                int startIndex = Math.Max(0, totalCount - maxCount);
                int endIndex = totalCount;
                
                for (int i = startIndex; i < endIndex; i++)
                {
                    try
                    {
                        // 创建LogEntry对象来接收数据
                        var logEntry = new
                        {
                            message = "",
                            file = "",
                            line = 0,
                            mode = 0,
                            instanceID = 0
                        };
                        
                        // 调用GetEntryInternal方法
                        var parameters_array = new object[] { i, logEntry.message, logEntry.file, logEntry.line, logEntry.mode, logEntry.instanceID };
                        getEntryInternalMethod.Invoke(null, parameters_array);
                        
                        string message = parameters_array[1]?.ToString() ?? "";
                        string file = parameters_array[2]?.ToString() ?? "";
                        int line = (int)(parameters_array[3] ?? 0);
                        int mode = (int)(parameters_array[4] ?? 0);
                        int instanceID = (int)(parameters_array[5] ?? 0);
                        
                        // 解析日志级别
                        string level = "Info";
                        if ((mode & 2) != 0) level = "Warning";
                        if ((mode & 4) != 0) level = "Error";
                        
                        // 过滤日志级别
                        if (logLevel != "all")
                        {
                            if (logLevel == "info" && level != "Info") continue;
                            if (logLevel == "warning" && level != "Warning") continue;
                            if (logLevel == "error" && level != "Error") continue;
                        }
                        
                        // 搜索文本过滤
                        if (!string.IsNullOrEmpty(searchText) && 
                            !message.ToLower().Contains(searchText.ToLower()))
                        {
                            continue;
                        }
                        
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