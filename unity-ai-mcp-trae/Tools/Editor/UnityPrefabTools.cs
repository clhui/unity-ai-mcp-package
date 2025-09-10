using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json.Linq;
using Unity.MCP;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Unity.MCP.Tools.Editor
{
    /// <summary>
    /// Unity预制体管理工具
    /// </summary>
    public static class UnityPrefabTools
    {
        /// <summary>
        /// 将游戏对象保存为预制体
        /// </summary>
        public static McpToolResult CreatePrefab(JObject arguments)
        {
            try
            {
#if UNITY_EDITOR
                string gameObjectName = arguments.ContainsKey("gameObjectName") ? arguments["gameObjectName"].ToString() : "";
                string prefabName = arguments.ContainsKey("prefabName") ? arguments["prefabName"].ToString() : gameObjectName + "_Prefab";
                string savePath = arguments.ContainsKey("savePath") ? arguments["savePath"].ToString() : "Assets/Prefabs";
                bool overwrite = arguments.ContainsKey("overwrite") && (bool)arguments["overwrite"];

                if (string.IsNullOrEmpty(gameObjectName))
                {
                    return new McpToolResult
                    {
                        Content = new List<McpContent>
                        {
                            new McpContent { Type = "text", Text = "Error: gameObjectName is required" }
                        },
                        IsError = true
                    };
                }

                // 查找游戏对象
                GameObject gameObject = GameObject.Find(gameObjectName);
                if (gameObject == null)
                {
                    return new McpToolResult
                    {
                        Content = new List<McpContent>
                        {
                            new McpContent { Type = "text", Text = $"Error: GameObject '{gameObjectName}' not found" }
                        },
                        IsError = true
                    };
                }

                // 确保保存路径存在
                if (!Directory.Exists(savePath))
                {
                    Directory.CreateDirectory(savePath);
                    AssetDatabase.Refresh();
                }

                // 构建完整的预制体路径
                string prefabPath = Path.Combine(savePath, prefabName + ".prefab").Replace("\\", "/");

                // 检查文件是否已存在
                if (File.Exists(prefabPath) && !overwrite)
                {
                    return new McpToolResult
                    {
                        Content = new List<McpContent>
                        {
                            new McpContent { Type = "text", Text = $"Error: Prefab '{prefabPath}' already exists. Set overwrite to true to replace it." }
                        },
                        IsError = true
                    };
                }

                // 创建预制体
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(gameObject, prefabPath);
                
                if (prefab != null)
                {
                    AssetDatabase.Refresh();
                    return new McpToolResult
                    {
                        Content = new List<McpContent>
                        {
                            new McpContent { Type = "text", Text = $"Prefab '{prefabName}' created successfully at '{prefabPath}'" }
                        }
                    };
                }
                else
                {
                    return new McpToolResult
                    {
                        Content = new List<McpContent>
                        {
                            new McpContent { Type = "text", Text = $"Error: Failed to create prefab '{prefabName}'" }
                        },
                        IsError = true
                    };
                }
#else
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = "Error: This tool can only be used in Unity Editor" }
                    },
                    IsError = true
                };
#endif
            }
            catch (Exception e)
            {
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Error creating prefab: {e.Message}" }
                    },
                    IsError = true
                };
            }
        }

        /// <summary>
        /// 实例化预制体
        /// </summary>
        public static McpToolResult InstantiatePrefab(JObject arguments)
        {
            try
            {
#if UNITY_EDITOR
                string prefabPath = arguments.ContainsKey("prefabPath") ? arguments["prefabPath"].ToString() : "";
                string instanceName = arguments.ContainsKey("instanceName") ? arguments["instanceName"].ToString() : "";
                string parentName = arguments.ContainsKey("parentName") ? arguments["parentName"].ToString() : "";
                
                // 位置参数
                Vector3 position = Vector3.zero;
                if (arguments.ContainsKey("position"))
                {
                    var posObj = arguments["position"];
                    position = new Vector3(
                        posObj["x"]?.ToObject<float>() ?? 0f,
                        posObj["y"]?.ToObject<float>() ?? 0f,
                        posObj["z"]?.ToObject<float>() ?? 0f
                    );
                }

                // 旋转参数
                Vector3 rotation = Vector3.zero;
                if (arguments.ContainsKey("rotation"))
                {
                    var rotObj = arguments["rotation"];
                    rotation = new Vector3(
                        rotObj["x"]?.ToObject<float>() ?? 0f,
                        rotObj["y"]?.ToObject<float>() ?? 0f,
                        rotObj["z"]?.ToObject<float>() ?? 0f
                    );
                }

                // 缩放参数
                Vector3 scale = Vector3.one;
                if (arguments.ContainsKey("scale"))
                {
                    var scaleObj = arguments["scale"];
                    scale = new Vector3(
                        scaleObj["x"]?.ToObject<float>() ?? 1f,
                        scaleObj["y"]?.ToObject<float>() ?? 1f,
                        scaleObj["z"]?.ToObject<float>() ?? 1f
                    );
                }

                if (string.IsNullOrEmpty(prefabPath))
                {
                    return new McpToolResult
                    {
                        Content = new List<McpContent>
                        {
                            new McpContent { Type = "text", Text = "Error: prefabPath is required" }
                        },
                        IsError = true
                    };
                }

                // 加载预制体
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab == null)
                {
                    return new McpToolResult
                    {
                        Content = new List<McpContent>
                        {
                            new McpContent { Type = "text", Text = $"Error: Prefab not found at path '{prefabPath}'" }
                        },
                        IsError = true
                    };
                }

                // 查找父对象
                Transform parent = null;
                if (!string.IsNullOrEmpty(parentName))
                {
                    GameObject parentObject = GameObject.Find(parentName);
                    if (parentObject != null)
                    {
                        parent = parentObject.transform;
                    }
                }

                // 实例化预制体
                GameObject instance = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
                
                if (instance != null)
                {
                    // 设置变换属性
                    instance.transform.position = position;
                    instance.transform.rotation = Quaternion.Euler(rotation);
                    instance.transform.localScale = scale;

                    // 设置实例名称
                    if (!string.IsNullOrEmpty(instanceName))
                    {
                        instance.name = instanceName;
                    }

                    return new McpToolResult
                    {
                        Content = new List<McpContent>
                        {
                            new McpContent { Type = "text", Text = $"Prefab '{prefab.name}' instantiated successfully as '{instance.name}' at position {position}" }
                        }
                    };
                }
                else
                {
                    return new McpToolResult
                    {
                        Content = new List<McpContent>
                        {
                            new McpContent { Type = "text", Text = $"Error: Failed to instantiate prefab '{prefab.name}'" }
                        },
                        IsError = true
                    };
                }
#else
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = "Error: This tool can only be used in Unity Editor" }
                    },
                    IsError = true
                };
#endif
            }
            catch (Exception e)
            {
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Error instantiating prefab: {e.Message}" }
                    },
                    IsError = true
                };
            }
        }

        /// <summary>
        /// 列出项目中的所有预制体
        /// </summary>
        public static McpToolResult ListPrefabs(JObject arguments)
        {
            try
            {
#if UNITY_EDITOR
                string searchPath = arguments.ContainsKey("searchPath") ? arguments["searchPath"].ToString() : "Assets";
                bool includeSubfolders = !arguments.ContainsKey("includeSubfolders") || (bool)arguments["includeSubfolders"];

                // 搜索预制体文件
                string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { searchPath });
                
                List<string> prefabPaths = new List<string>();
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (!includeSubfolders && Path.GetDirectoryName(path).Replace("\\", "/") != searchPath.Replace("\\", "/"))
                    {
                        continue;
                    }
                    prefabPaths.Add(path);
                }

                string result = $"Found {prefabPaths.Count} prefab(s) in '{searchPath}':\n";
                foreach (string path in prefabPaths)
                {
                    string prefabName = Path.GetFileNameWithoutExtension(path);
                    result += $"- {prefabName} ({path})\n";
                }

                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = result }
                    }
                };
#else
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = "Error: This tool can only be used in Unity Editor" }
                    },
                    IsError = true
                };
#endif
            }
            catch (Exception e)
            {
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Error listing prefabs: {e.Message}" }
                    },
                    IsError = true
                };
            }
        }

        /// <summary>
        /// 获取预制体信息
        /// </summary>
        public static McpToolResult GetPrefabInfo(JObject arguments)
        {
            try
            {
#if UNITY_EDITOR
                string prefabPath = arguments.ContainsKey("prefabPath") ? arguments["prefabPath"].ToString() : "";

                if (string.IsNullOrEmpty(prefabPath))
                {
                    return new McpToolResult
                    {
                        Content = new List<McpContent>
                        {
                            new McpContent { Type = "text", Text = "Error: prefabPath is required" }
                        },
                        IsError = true
                    };
                }

                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab == null)
                {
                    return new McpToolResult
                    {
                        Content = new List<McpContent>
                        {
                            new McpContent { Type = "text", Text = $"Error: Prefab not found at path '{prefabPath}'" }
                        },
                        IsError = true
                    };
                }

                // 收集预制体信息
                string info = $"Prefab Information for '{prefab.name}':\n";
                info += $"Path: {prefabPath}\n";
                info += $"Active: {prefab.activeInHierarchy}\n";
                info += $"Tag: {prefab.tag}\n";
                info += $"Layer: {LayerMask.LayerToName(prefab.layer)}\n";
                
                // 变换信息
                Transform transform = prefab.transform;
                info += $"Position: {transform.position}\n";
                info += $"Rotation: {transform.rotation.eulerAngles}\n";
                info += $"Scale: {transform.localScale}\n";
                
                // 组件信息
                Component[] components = prefab.GetComponents<Component>();
                info += $"Components ({components.Length}):\n";
                foreach (Component component in components)
                {
                    if (component != null)
                    {
                        info += $"- {component.GetType().Name}\n";
                    }
                }
                
                // 子对象信息
                int childCount = transform.childCount;
                info += $"Child Objects: {childCount}\n";
                if (childCount > 0)
                {
                    for (int i = 0; i < childCount; i++)
                    {
                        Transform child = transform.GetChild(i);
                        info += $"- {child.name}\n";
                    }
                }

                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = info }
                    }
                };
#else
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = "Error: This tool can only be used in Unity Editor" }
                    },
                    IsError = true
                };
#endif
            }
            catch (Exception e)
            {
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Error getting prefab info: {e.Message}" }
                    },
                    IsError = true
                };
            }
        }

        /// <summary>
        /// 删除预制体文件
        /// </summary>
        public static McpToolResult DeletePrefab(JObject arguments)
        {
            try
            {
#if UNITY_EDITOR
                string prefabPath = arguments.ContainsKey("prefabPath") ? arguments["prefabPath"].ToString() : "";
                bool confirmDelete = arguments.ContainsKey("confirmDelete") && (bool)arguments["confirmDelete"];

                if (string.IsNullOrEmpty(prefabPath))
                {
                    return new McpToolResult
                    {
                        Content = new List<McpContent>
                        {
                            new McpContent { Type = "text", Text = "Error: prefabPath is required" }
                        },
                        IsError = true
                    };
                }

                // 检查预制体文件是否存在
                if (!File.Exists(prefabPath))
                {
                    return new McpToolResult
                    {
                        Content = new List<McpContent>
                        {
                            new McpContent { Type = "text", Text = $"Error: Prefab file not found at path '{prefabPath}'" }
                        },
                        IsError = true
                    };
                }

                // 验证是否为预制体文件
                if (!prefabPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                {
                    return new McpToolResult
                    {
                        Content = new List<McpContent>
                        {
                            new McpContent { Type = "text", Text = $"Error: '{prefabPath}' is not a prefab file (.prefab)" }
                        },
                        IsError = true
                    };
                }

                // 安全检查：需要确认删除
                if (!confirmDelete)
                {
                    return new McpToolResult
                    {
                        Content = new List<McpContent>
                        {
                            new McpContent { Type = "text", Text = $"Warning: This will permanently delete the prefab '{prefabPath}'. Set 'confirmDelete' to true to proceed." }
                        },
                        IsError = true
                    };
                }

                // 获取预制体名称用于日志
                string prefabName = Path.GetFileNameWithoutExtension(prefabPath);

                // 删除预制体文件
                bool deleteSuccess = AssetDatabase.DeleteAsset(prefabPath);
                
                if (deleteSuccess)
                {
                    AssetDatabase.Refresh();
                    return new McpToolResult
                    {
                        Content = new List<McpContent>
                        {
                            new McpContent { Type = "text", Text = $"Prefab '{prefabName}' deleted successfully from '{prefabPath}'" }
                        }
                    };
                }
                else
                {
                    return new McpToolResult
                    {
                        Content = new List<McpContent>
                        {
                            new McpContent { Type = "text", Text = $"Error: Failed to delete prefab '{prefabName}' at '{prefabPath}'. The file may be in use or protected." }
                        },
                        IsError = true
                    };
                }
#else
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = "Error: This tool can only be used in Unity Editor" }
                    },
                    IsError = true
                };
#endif
            }
            catch (Exception e)
            {
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Error deleting prefab: {e.Message}" }
                    },
                    IsError = true
                };
            }
        }
    }
}