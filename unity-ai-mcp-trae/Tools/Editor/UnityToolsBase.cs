using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;
using Newtonsoft.Json.Linq;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace Unity.MCP.Editor
{
    /// <summary>
    /// Unity MCP工具基础类，包含共用的数据结构和基础功能
    /// </summary>
    public static class UnityToolsBase
    {
        // 场景变化事件委托
        public static event System.Action<string, string> OnSceneChanged;
        
        // 当前场景信息缓存
        private static string _currentScenePath = "";
        private static bool _isListenerInitialized = false;
        
        public class SceneInfo
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
    }
}