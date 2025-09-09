using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json.Linq;
using Unity.MCP;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace Unity.MCP.Tools.Editor
{
    /// <summary>
    /// Unity编辑器工具类
    /// </summary>
    public static class UnityEditorTools
    {
        /// <summary>
        /// 强制刷新Unity编辑器界面
        /// </summary>
        /// <param name="arguments">参数</param>
        /// <returns>操作结果</returns>
        public static McpToolResult RefreshEditor(JObject arguments)
        {
#if UNITY_EDITOR
            try
            {
                // 获取刷新选项
                bool refreshAssets = arguments["refreshAssets"]?.ToObject<bool>() ?? true;
                bool refreshSceneView = arguments["refreshSceneView"]?.ToObject<bool>() ?? true;
                bool refreshHierarchy = arguments["refreshHierarchy"]?.ToObject<bool>() ?? true;
                bool refreshInspector = arguments["refreshInspector"]?.ToObject<bool>() ?? true;
                bool refreshProject = arguments["refreshProject"]?.ToObject<bool>() ?? true;
                bool refreshConsole = arguments["refreshConsole"]?.ToObject<bool>() ?? false;
                
                var refreshedComponents = new List<string>();
                
                // 刷新资源数据库
                if (refreshAssets)
                {
                    AssetDatabase.Refresh();
                    refreshedComponents.Add("资源数据库");
                }
                
                // 刷新场景视图
                if (refreshSceneView)
                {
                    SceneView.RepaintAll();
                    refreshedComponents.Add("场景视图");
                }
                
                // 刷新层级视图
                if (refreshHierarchy)
                {
                    EditorApplication.RepaintHierarchyWindow();
                    refreshedComponents.Add("层级视图");
                }
                
                // 刷新检视器
                if (refreshInspector)
                {
                    EditorApplication.RepaintProjectWindow();
                    refreshedComponents.Add("检视器");
                }
                
                // 刷新项目窗口
                if (refreshProject)
                {
                    EditorApplication.RepaintProjectWindow();
                    refreshedComponents.Add("项目窗口");
                }
                
                // 刷新控制台（清空日志）
                if (refreshConsole)
                {
                    var logEntries = System.Type.GetType("UnityEditor.LogEntries,UnityEditor.dll");
                    if (logEntries != null)
                    {
                        var clearMethod = logEntries.GetMethod("Clear", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                        clearMethod?.Invoke(null, null);
                        refreshedComponents.Add("控制台");
                    }
                }
                
                // 强制重绘所有编辑器窗口
                EditorApplication.delayCall += () =>
                {
                    UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
                };
                
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent 
                        { 
                            Type = "text", 
                            Text = $"Unity编辑器界面已刷新\n已刷新组件: {string.Join(", ", refreshedComponents)}\n刷新时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}"
                        }
                    }
                };
            }
            catch (Exception ex)
            {
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"刷新编辑器界面失败: {ex.Message}" }
                    },
                    IsError = true
                };
            }
#else
            return new McpToolResult
            {
                Content = new List<McpContent>
                {
                    new McpContent { Type = "text", Text = "编辑器刷新功能仅在Unity编辑器中可用" }
                },
                IsError = true
            };
#endif
        }
        
        /// <summary>
        /// 获取编辑器状态信息
        /// </summary>
        /// <param name="arguments">参数</param>
        /// <returns>编辑器状态信息</returns>
        public static McpToolResult GetEditorStatus(JObject arguments)
        {
#if UNITY_EDITOR
            try
            {
                var statusInfo = new
                {
                    IsPlaying = EditorApplication.isPlaying,
                    IsPaused = EditorApplication.isPaused,
                    IsCompiling = EditorApplication.isCompiling,
                    IsUpdating = EditorApplication.isUpdating,
                    CurrentScene = EditorSceneManager.GetActiveScene().name,
                    ScenePath = EditorSceneManager.GetActiveScene().path,
                    UnityVersion = Application.unityVersion,
                    Platform = Application.platform.ToString(),
                    EditorSkin = EditorGUIUtility.isProSkin ? "Dark" : "Light"
                };
                
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent 
                        { 
                            Type = "text", 
                            Text = $"Unity编辑器状态:\n" +
                                   $"- 播放状态: {(statusInfo.IsPlaying ? "播放中" : "已停止")}\n" +
                                   $"- 暂停状态: {(statusInfo.IsPaused ? "已暂停" : "未暂停")}\n" +
                                   $"- 编译状态: {(statusInfo.IsCompiling ? "编译中" : "未编译")}\n" +
                                   $"- 更新状态: {(statusInfo.IsUpdating ? "更新中" : "未更新")}\n" +
                                   $"- 当前场景: {statusInfo.CurrentScene}\n" +
                                   $"- 场景路径: {statusInfo.ScenePath}\n" +
                                   $"- Unity版本: {statusInfo.UnityVersion}\n" +
                                   $"- 平台: {statusInfo.Platform}\n" +
                                   $"- 编辑器主题: {statusInfo.EditorSkin}"
                        }
                    }
                };
            }
            catch (Exception ex)
            {
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"获取编辑器状态失败: {ex.Message}" }
                    },
                    IsError = true
                };
            }
#else
            return new McpToolResult
            {
                Content = new List<McpContent>
                {
                    new McpContent { Type = "text", Text = "编辑器状态查询仅在Unity编辑器中可用" }
                },
                IsError = true
            };
#endif
        }
    }
}