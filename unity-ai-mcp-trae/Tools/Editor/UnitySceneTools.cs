using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Newtonsoft.Json.Linq;
using Unity.MCP;
using Unity.MCP.Editor;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace Unity.MCP.Tools.Editor
{
    /// <summary>
    /// Unity场景管理工具
    /// </summary>
    public static class UnitySceneTools
    {
        public static McpToolResult ListScenes(JObject arguments)
        {
#if UNITY_EDITOR
            var scenes = new List<UnityToolsBase.SceneInfo>();
            
            // 获取构建设置中的场景
            var buildScenes = EditorBuildSettings.scenes;
            
            foreach (var scene in buildScenes)
            {
                var sceneInfo = new UnityToolsBase.SceneInfo
                {
                    path = scene.path,
                    name = System.IO.Path.GetFileNameWithoutExtension(scene.path),
                    enabled = scene.enabled,
                    isLoaded = false
                };
                scenes.Add(sceneInfo);
            }
            
            // 获取当前加载的场景
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var loadedScene = SceneManager.GetSceneAt(i);
                var existingScene = scenes.FirstOrDefault(s => s.path == loadedScene.path);
                if (existingScene != null)
                {
                    existingScene.isLoaded = true;
                }
            }
            
            return new McpToolResult
            {
                Content = new List<McpContent>
                {
                    new McpContent
                    {
                        Type = "text",
                        Text = $"Found {scenes.Count} scenes:\n" + 
                               string.Join("\n", scenes.Select(s => $"- {s.name} ({s.path}) [Enabled: {s.enabled}, Loaded: {s.isLoaded}]"))
                    }
                }
            };
#else
            return new McpToolResult
            {
                Content = new List<McpContent>
                {
                    new McpContent { Type = "text", Text = "Scene listing is only available in Unity Editor" }
                },
                IsError = true
            };
#endif
        }
        
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
        
        public static McpToolResult GetCurrentSceneInfo(JObject arguments)
        {
#if UNITY_EDITOR
            try
            {
                var activeScene = SceneManager.GetActiveScene();
                if (!activeScene.IsValid())
                {
                    return new McpToolResult
                    {
                        Content = new List<McpContent>
                        {
                            new McpContent { Type = "text", Text = "没有活动场景" }
                        },
                        IsError = true
                    };
                }
                
                var sceneInfo = new System.Text.StringBuilder();
                sceneInfo.AppendLine($"当前场景信息:");
                sceneInfo.AppendLine($"- 场景名称: {activeScene.name}");
                sceneInfo.AppendLine($"- 场景路径: {activeScene.path}");
                sceneInfo.AppendLine($"- 构建索引: {activeScene.buildIndex}");
                sceneInfo.AppendLine($"- 是否已加载: {activeScene.isLoaded}");
                sceneInfo.AppendLine($"- 是否脏数据: {activeScene.isDirty}");
                sceneInfo.AppendLine($"- 根对象数量: {activeScene.rootCount}");
                sceneInfo.AppendLine();
                
                // 获取场景中的根对象详细信息
                var rootObjects = activeScene.GetRootGameObjects();
                sceneInfo.AppendLine($"根GameObject列表 ({rootObjects.Length}个):");
                
                foreach (var obj in rootObjects)
                {
                    sceneInfo.AppendLine($"  📦 {obj.name}");
                    sceneInfo.AppendLine($"    - 活动状态: {obj.activeInHierarchy}");
                    sceneInfo.AppendLine($"    - 标签: {obj.tag}");
                    sceneInfo.AppendLine($"    - 层级: {obj.layer}");
                    
                    // 获取组件信息
                    var components = obj.GetComponents<Component>();
                    sceneInfo.AppendLine($"    - 组件 ({components.Length}个):");
                    foreach (var comp in components)
                    {
                        if (comp != null)
                        {
                            sceneInfo.AppendLine($"      🔧 {comp.GetType().Name}");
                        }
                    }
                    
                    // 获取子对象数量
                    var childCount = obj.transform.childCount;
                    if (childCount > 0)
                    {
                        sceneInfo.AppendLine($"    - 子对象数量: {childCount}");
                    }
                    
                    sceneInfo.AppendLine();
                }
                
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent
                        {
                            Type = "text",
                            Text = sceneInfo.ToString()
                        }
                    }
                };
            }
            catch (System.Exception ex)
            {
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"获取场景信息时发生错误: {ex.Message}" }
                    },
                    IsError = true
                };
            }
#else
            return new McpToolResult
            {
                Content = new List<McpContent>
                {
                    new McpContent { Type = "text", Text = "Scene info is only available in Unity Editor" }
                },
                IsError = true
            };
#endif
        }
        
        public static McpToolResult OpenScene(JObject arguments)
        {
#if UNITY_EDITOR
            var scenePath = arguments["path"]?.ToString();
            if (string.IsNullOrEmpty(scenePath))
            {
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = "Scene path is required" }
                    },
                    IsError = true
                };
            }
            
            try
            {
                EditorSceneManager.OpenScene(scenePath);
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Successfully opened scene: {scenePath}" }
                    }
                };
            }
            catch (Exception ex)
            {
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Failed to open scene: {ex.Message}" }
                    },
                    IsError = true
                };
            }
#else
            return new McpToolResult
            {
                Content = new List<McpContent>
                {
                    new McpContent { Type = "text", Text = "Scene opening is only available in Unity Editor" }
                },
                IsError = true
            };
#endif
        }

        public static McpToolResult LoadScene(JObject arguments)
        {
#if UNITY_EDITOR
            var sceneName = arguments["name"]?.ToString();
            var scenePath = arguments["path"]?.ToString();
            
            // 优先使用场景名称，如果没有则从路径提取
            string targetSceneName;
            if (!string.IsNullOrEmpty(sceneName))
            {
                targetSceneName = sceneName;
            }
            else if (!string.IsNullOrEmpty(scenePath))
            {
                targetSceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            }
            else
            {
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = "Scene name or path is required" }
                    },
                    IsError = true
                };
            }
            
            try
            {
                // 使用SceneManager.LoadScene，适用于播放模式
                SceneManager.LoadScene(targetSceneName);
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Successfully loaded scene: {targetSceneName}" }
                    }
                };
            }
            catch (Exception ex)
            {
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Failed to load scene: {ex.Message}" }
                    },
                    IsError = true
                };
            }
#else
            return new McpToolResult
            {
                Content = new List<McpContent>
                {
                    new McpContent { Type = "text", Text = "Scene loading is only available in Unity Editor" }
                },
                IsError = true
            };
#endif
        }

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

        public static McpToolResult StopPlayMode(JObject arguments)
        {
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