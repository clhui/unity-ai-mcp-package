using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.MCP;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace Unity.MCP.Editor
{
    public static class UnityTools
    {
        // 场景变化事件委托
        public static event System.Action<string, string> OnSceneChanged;
        
        // 当前场景信息缓存
        private static string _currentScenePath = "";
        private static bool _isListenerInitialized = false;
        
        private class SceneInfo
        {
            public string path;
            public string name;
            public bool enabled;
            public bool isLoaded;
        }
        
        // 初始化场景变化监听器
        public static void InitializeSceneListener()
        {
#if UNITY_EDITOR
            if (_isListenerInitialized) return;
            
            // 监听场景打开事件
            // EditorSceneManager.sceneOpened += OnSceneOpened;
            // EditorSceneManager.activeSceneChangedInEditMode += OnActiveSceneChanged;
            
            // 获取当前活动场景
            var activeScene = SceneManager.GetActiveScene();
            if (activeScene.IsValid())
            {
                _currentScenePath = activeScene.path;
            }
            
            _isListenerInitialized = true;
#endif
        }
        
#if UNITY_EDITOR
        private static void OnSceneOpened(UnityEngine.SceneManagement.Scene scene, UnityEditor.SceneManagement.OpenSceneMode mode)
        {
            HandleSceneChange(scene.path, scene.name);
        }
        
        private static void OnActiveSceneChanged(UnityEngine.SceneManagement.Scene previousScene, UnityEngine.SceneManagement.Scene newScene)
        {
            if (newScene.IsValid())
            {
                HandleSceneChange(newScene.path, newScene.name);
            }
        }
        
        private static void HandleSceneChange(string scenePath, string sceneName)
        {
            if (_currentScenePath != scenePath)
            {
                var previousPath = _currentScenePath;
                _currentScenePath = scenePath;
                
                // 场景变化已检测
                
                // 触发场景变化事件
                OnSceneChanged?.Invoke(previousPath, scenePath);
                
                // 自动读取新场景信息
                ReadNewSceneInfo(sceneName, scenePath);
            }
        }
        
        private static void ReadNewSceneInfo(string sceneName, string scenePath)
        {
            try
            {
                var scene = SceneManager.GetSceneByPath(scenePath);
                if (scene.IsValid() && scene.isLoaded)
                {
                    // 获取场景中的根对象
                    var rootObjects = scene.GetRootGameObjects();
                    foreach (var obj in rootObjects)
                    {
                        // 获取组件信息
                        var components = obj.GetComponents<Component>();
                        foreach (var comp in components)
                        {
                            if (comp != null)
                            {
                                // 组件信息已获取
                            }
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                // 静默处理场景信息读取错误
            }
        }
#endif
        
        // 场景管理方法已移动到UnitySceneTools.cs
        // 游戏对象管理方法已移动到UnityGameObjectTools.cs
        // 组件管理方法已移动到UnityComponentTools.cs
        // 播放模式管理方法已移动到UnityPlayModeTools.cs
        // 材质和渲染系统方法已移动到UnityMaterialTools.cs
        // 物理系统方法已移动到UnityPhysicsTools.cs
        // 音频系统方法已移动到UnityAudioTools.cs
        // 光照系统方法已移动到UnityLightingTools.cs
        // 脚本管理方法已移动到UnityScriptTools.cs
        // UI系统方法已移动到UnityUITools.cs
        // 动画系统方法已移动到UnityAnimationTools.cs
        
        public static McpToolResult GetThreadStackInfo(JObject arguments)
        {
            try
            {
                var stackInfo = new System.Text.StringBuilder();
                stackInfo.AppendLine("Unity线程栈信息和死锁检测:");
                stackInfo.AppendLine(new string('=', 50));
                
                // 获取当前进程的所有线程
                var process = System.Diagnostics.Process.GetCurrentProcess();
                var threads = process.Threads;
                
                stackInfo.AppendLine($"总线程数量: {threads.Count}");
                stackInfo.AppendLine();
                
                // 分析每个线程的状态
                var waitingThreads = new List<System.Diagnostics.ProcessThread>();
                var runningThreads = new List<System.Diagnostics.ProcessThread>();
                var standbyThreads = new List<System.Diagnostics.ProcessThread>();
                var terminatedThreads = new List<System.Diagnostics.ProcessThread>();
                var otherThreads = new List<System.Diagnostics.ProcessThread>();
                
                foreach (System.Diagnostics.ProcessThread thread in threads)
                {
                    try
                    {
                        switch (thread.ThreadState)
                        {
                            case System.Diagnostics.ThreadState.Wait:
                                waitingThreads.Add(thread);
                                break;
                            case System.Diagnostics.ThreadState.Running:
                                runningThreads.Add(thread);
                                break;
                            case System.Diagnostics.ThreadState.Standby:
                                standbyThreads.Add(thread);
                                break;
                            case System.Diagnostics.ThreadState.Terminated:
                                terminatedThreads.Add(thread);
                                break;
                            default:
                                otherThreads.Add(thread);
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        // 某些线程可能无法访问，跳过
                        stackInfo.AppendLine($"无法访问线程 {thread.Id}: {ex.Message}");
                    }
                }
                
                // 输出线程状态统计
                stackInfo.AppendLine("线程状态统计:");
                stackInfo.AppendLine($"- 等待中: {waitingThreads.Count}");
                stackInfo.AppendLine($"- 运行中: {runningThreads.Count}");
                stackInfo.AppendLine($"- 待机: {standbyThreads.Count}");
                stackInfo.AppendLine($"- 已终止: {terminatedThreads.Count}");
                stackInfo.AppendLine($"- 其他状态: {otherThreads.Count}");
                stackInfo.AppendLine();
                
                // 检测潜在的死锁情况
                if (waitingThreads.Count > runningThreads.Count * 2)
                {
                    stackInfo.AppendLine("⚠️ 警告: 等待线程数量异常高，可能存在死锁!");
                    stackInfo.AppendLine();
                }
                
                // 详细分析等待线程
                if (waitingThreads.Count > 0)
                {
                    stackInfo.AppendLine("等待线程详情:");
                    foreach (var thread in waitingThreads.Take(10)) // 只显示前10个
                    {
                        try
                        {
                            stackInfo.AppendLine($"  线程 {thread.Id}:");
                            stackInfo.AppendLine($"    - 等待原因: {thread.WaitReason}");
                            stackInfo.AppendLine($"    - 优先级: {thread.PriorityLevel}");
                            stackInfo.AppendLine($"    - 开始时间: {thread.StartTime}");
                            stackInfo.AppendLine($"    - 总处理器时间: {thread.TotalProcessorTime}");
                            stackInfo.AppendLine();
                        }
                        catch (Exception ex)
                        {
                            stackInfo.AppendLine($"    - 无法获取详细信息: {ex.Message}");
                        }
                    }
                    
                    if (waitingThreads.Count > 10)
                    {
                        stackInfo.AppendLine($"  ... 还有 {waitingThreads.Count - 10} 个等待线程");
                    }
                }
                
                // Unity主线程信息
                stackInfo.AppendLine("Unity主线程信息:");
                stackInfo.AppendLine($"- 当前线程ID: {System.Threading.Thread.CurrentThread.ManagedThreadId}");
                stackInfo.AppendLine($"- 是否为主线程: {System.Threading.Thread.CurrentThread.IsBackground == false}");
                stackInfo.AppendLine($"- 线程状态: {System.Threading.Thread.CurrentThread.ThreadState}");
                
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent
                        {
                            Type = "text",
                            Text = stackInfo.ToString()
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
                        new McpContent { Type = "text", Text = $"获取线程栈信息失败: {ex.Message}" }
                    },
                    IsError = true
                };
            }
        }
    }
}