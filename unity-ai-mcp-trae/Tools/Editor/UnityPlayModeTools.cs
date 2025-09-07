using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Unity.MCP;
using Unity.MCP.Editor;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Unity.MCP.Tools.Editor
{
    /// <summary>
    /// Unity播放模式管理工具类
    /// </summary>
    public static class UnityPlayModeTools
    {
        /// <summary>
        /// 获取播放模式状态
        /// </summary>
        /// <param name="arguments">参数</param>
        /// <returns>播放模式状态信息</returns>
        public static McpToolResult GetPlayModeStatus(JObject arguments)
        {
#if UNITY_EDITOR
            var isPlaying = EditorApplication.isPlaying;
            var isPaused = EditorApplication.isPaused;
            var isCompiling = EditorApplication.isCompiling;
            
            return new McpToolResult
            {
                Content = new List<McpContent>
                {
                    new McpContent 
                    { 
                        Type = "text", 
                        Text = $"Play Mode Status:\n" +
                               $"- Is Playing: {isPlaying}\n" +
                               $"- Is Paused: {isPaused}\n" +
                               $"- Is Compiling: {isCompiling}\n" +
                               $"- Current State: {(isPlaying ? (isPaused ? "Paused" : "Playing") : "Stopped")}"
                    }
                }
            };
#else
            return new McpToolResult
            {
                Content = new List<McpContent>
                {
                    new McpContent { Type = "text", Text = "Play mode status is only available in Unity Editor" }
                },
                IsError = true
            };
#endif
        }

        /// <summary>
        /// 启动播放模式
        /// </summary>
        /// <param name="arguments">参数</param>
        /// <returns>操作结果</returns>
        public static McpToolResult StartPlayMode(JObject arguments)
        {
#if UNITY_EDITOR
            if (EditorApplication.isPlaying)
            {
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = "Play mode is already active" }
                    }
                };
            }

            try
            {
                // 直接设置播放模式，Unity会自动处理线程安全
                EditorApplication.isPlaying = true;

                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = "Play mode started successfully" }
                    }
                };
            }
            catch (System.Exception ex)
            {
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Failed to start play mode: {ex.Message}" }
                    },
                    IsError = true
                };
            }
#else
            return new McpToolResult
            {
                Content = new List<McpContent>
                {
                    new McpContent { Type = "text", Text = "Play mode control is only available in Unity Editor" }
                },
                IsError = true
            };
#endif
        }

        /// <summary>
        /// 停止播放模式
        /// </summary>
        /// <param name="arguments">参数</param>
        /// <returns>操作结果</returns>
        public static McpToolResult StopPlayMode(JObject arguments)
        {
            //打印日志
            McpLogger.LogTool("StopPlayMode 停止播放模式");
#if UNITY_EDITOR
            if (!EditorApplication.isPlaying)
            {
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = "Play mode is not active" }
                    }
                };
            }

            try
            {
                // 直接设置播放模式，Unity会自动处理线程安全
                EditorApplication.isPlaying = false;

                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = "Play mode stopped successfully" }
                    }
                };
            }
            catch (System.Exception ex)
            {
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Failed to stop play mode: {ex.Message}" }
                    },
                    IsError = true
                };
            }
#else
            return new McpToolResult
            {
                Content = new List<McpContent>
                {
                    new McpContent { Type = "text", Text = "Play mode control is only available in Unity Editor" }
                },
                IsError = true
            };
#endif
        }
    }
}