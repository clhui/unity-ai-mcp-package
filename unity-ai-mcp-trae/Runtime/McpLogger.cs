using System;
using UnityEngine;

namespace Unity.MCP
{
    /// <summary>
    /// MCP服务器统一日志工具类
    /// 支持不同日志级别和debug模式控制
    /// </summary>
    public static class McpLogger
    {
        /// <summary>
        /// 日志级别枚举
        /// </summary>
        public enum LogLevel
        {
            /// <summary>工具调用相关日志（常规模式显示）</summary>
            Tool = 0,
            /// <summary>内部调试日志（仅debug模式显示）</summary>
            Debug = 1,
            /// <summary>警告日志（总是显示）</summary>
            Warning = 2,
            /// <summary>错误日志（总是显示）</summary>
            Error = 3
        }

        /// <summary>
        /// 是否启用debug模式
        /// </summary>
        public static bool IsDebugEnabled { get; set; } = false;

        /// <summary>
        /// 记录工具调用日志（常规模式显示）
        /// </summary>
        /// <param name="message">日志消息</param>
        /// <param name="context">上下文对象</param>
        public static void LogTool(string message, UnityEngine.Object context = null)
        {
            Log(LogLevel.Tool, message, context);
        }

        /// <summary>
        /// 记录内部调试日志（仅debug模式显示）
        /// </summary>
        /// <param name="message">日志消息</param>
        /// <param name="context">上下文对象</param>
        public static void LogDebug(string message, UnityEngine.Object context = null)
        {
            Log(LogLevel.Debug, message, context);
        }

        /// <summary>
        /// 记录警告日志（总是显示）
        /// </summary>
        /// <param name="message">日志消息</param>
        /// <param name="context">上下文对象</param>
        public static void LogWarning(string message, UnityEngine.Object context = null)
        {
            Log(LogLevel.Warning, message, context);
        }

        /// <summary>
        /// 记录错误日志（总是显示）
        /// </summary>
        /// <param name="message">日志消息</param>
        /// <param name="context">上下文对象</param>
        public static void LogError(string message, UnityEngine.Object context = null)
        {
            Log(LogLevel.Error, message, context);
        }

        /// <summary>
        /// 记录异常日志（总是显示）
        /// </summary>
        /// <param name="exception">异常对象</param>
        /// <param name="message">附加消息</param>
        /// <param name="context">上下文对象</param>
        public static void LogException(Exception exception, string message = null, UnityEngine.Object context = null)
        {
            var fullMessage = string.IsNullOrEmpty(message) 
                ? $"[McpServer] Exception: {exception.Message}\n{exception.StackTrace}"
                : $"[McpServer] {message}: {exception.Message}\n{exception.StackTrace}";
            
            UnityEngine.Debug.LogError(fullMessage, context);
        }

        /// <summary>
        /// 核心日志记录方法
        /// </summary>
        /// <param name="level">日志级别</param>
        /// <param name="message">日志消息</param>
        /// <param name="context">上下文对象</param>
        private static void Log(LogLevel level, string message, UnityEngine.Object context = null)
        {
            // 检查是否应该显示此日志
            if (!ShouldLog(level))
                return;

            // 添加统一的前缀
            var prefixedMessage = $"[McpServer] {message}";

            // 根据日志级别选择合适的Unity日志方法
            switch (level)
            {
                case LogLevel.Tool:
                case LogLevel.Debug:
                    UnityEngine.Debug.Log(prefixedMessage, context);
                    break;
                case LogLevel.Warning:
                    UnityEngine.Debug.LogWarning(prefixedMessage, context);
                    break;
                case LogLevel.Error:
                    UnityEngine.Debug.LogError(prefixedMessage, context);
                    break;
            }
        }

        /// <summary>
        /// 检查是否应该记录指定级别的日志
        /// </summary>
        /// <param name="level">日志级别</param>
        /// <returns>是否应该记录</returns>
        private static bool ShouldLog(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Tool:
                    return true; // 工具调用日志总是显示
                case LogLevel.Debug:
                    return IsDebugEnabled; // 调试日志仅在debug模式下显示
                case LogLevel.Warning:
                case LogLevel.Error:
                    return true; // 警告和错误总是显示
                default:
                    return true;
            }
        }

        /// <summary>
        /// 格式化工具调用开始日志
        /// </summary>
        /// <param name="toolName">工具名称</param>
        /// <param name="arguments">工具参数</param>
        /// <returns>格式化的日志消息</returns>
        public static string FormatToolStart(string toolName, object arguments = null)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            if (arguments != null)
            {
                return $"🔧 [{timestamp}] 调用工具: {toolName}, 参数: {arguments}";
            }
            return $"🔧 [{timestamp}] 调用工具: {toolName}";
        }

        /// <summary>
        /// 格式化工具调用结果日志
        /// </summary>
        /// <param name="toolName">工具名称</param>
        /// <param name="success">是否成功</param>
        /// <param name="result">结果信息</param>
        /// <returns>格式化的日志消息</returns>
        public static string FormatToolResult(string toolName, bool success, string result = null)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            var icon = success ? "✅" : "❌";
            var status = success ? "成功" : "失败";
            
            if (!string.IsNullOrEmpty(result))
            {
                return $"{icon} [{timestamp}] 工具 {toolName} {status}: {result}";
            }
            return $"{icon} [{timestamp}] 工具 {toolName} {status}";
        }

        /// <summary>
        /// 格式化线程调度日志
        /// </summary>
        /// <param name="operation">操作描述</param>
        /// <param name="threadId">线程ID</param>
        /// <param name="additionalInfo">附加信息</param>
        /// <returns>格式化的日志消息</returns>
        public static string FormatThreadOperation(string operation, int threadId, string additionalInfo = null)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            if (!string.IsNullOrEmpty(additionalInfo))
            {
                return $"🔄 [{timestamp}] {operation} - 线程ID: {threadId}, {additionalInfo}";
            }
            return $"🔄 [{timestamp}] {operation} - 线程ID: {threadId}";
        }
    }
}