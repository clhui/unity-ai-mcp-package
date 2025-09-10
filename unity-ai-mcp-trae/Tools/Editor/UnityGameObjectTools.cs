using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json.Linq;
using Unity.MCP;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Unity.MCP.Editor
{
    /// <summary>
    /// Unity GameObject管理工具
    /// </summary>
    public static class UnityGameObjectTools
    {
        /// <summary>
        /// 检查是否处于Play模式，如果是则返回警告信息
        /// </summary>
        /// <returns>如果处于Play模式返回错误结果，否则返回null</returns>
        private static McpToolResult CheckPlayModeForEditing()
        {
#if UNITY_EDITOR
            if (EditorApplication.isPlaying)
            {
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = "⚠️ 无法在Play模式下编辑场景！请先停止Play模式再进行场景编辑操作。\n提示：点击Unity编辑器中的停止按钮或使用play_mode_stop工具停止Play模式。" }
                    },
                    IsError = true
                };
            }
#endif
            return null;
        }

        public static McpToolResult CreateGameObject(JObject arguments)
        {
            // 检查Play模式
            var playModeCheck = CheckPlayModeForEditing();
            if (playModeCheck != null) return playModeCheck;
            
            var name = arguments["name"]?.ToString() ?? "GameObject";
            var parentPath = arguments["parent"]?.ToString();
            
            McpLogger.LogTool($"开始创建GameObject: {name}" + (!string.IsNullOrEmpty(parentPath) ? $", 父对象: {parentPath}" : ""));
            
            try
            {
                var go = new GameObject(name);
                McpLogger.LogTool($"成功创建GameObject: {name} (ID: {go.GetInstanceID()})");
                
                if (!string.IsNullOrEmpty(parentPath))
                {
                    var parent = GameObject.Find(parentPath);
                    if (parent != null)
                    {
                        go.transform.SetParent(parent.transform);
                        McpLogger.LogTool($"成功设置父对象: {name} -> {parentPath}");
                    }
                    else
                    {
                        McpLogger.LogTool($"警告: 未找到父对象 '{parentPath}'，GameObject '{name}' 将创建在根级别");
                    }
                }
                
#if UNITY_EDITOR
                Undo.RegisterCreatedObjectUndo(go, "Create GameObject");
                Selection.activeGameObject = go;
#endif
                
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Created GameObject: {name} (ID: {go.GetInstanceID()})" }
                    }
                };
            }
            catch (Exception ex)
            {
                McpLogger.LogTool($"创建GameObject失败: {name}, 错误: {ex.Message}");
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Failed to create GameObject: {ex.Message}" }
                    },
                    IsError = true
                };
            }
        }
        
        public static McpToolResult AddComponent(JObject arguments)
        {
            // 检查Play模式
            var playModeCheck = CheckPlayModeForEditing();
            if (playModeCheck != null) return playModeCheck;
            
            var gameObjectName = arguments["gameObject"]?.ToString();
            var componentType = arguments["component"]?.ToString();
            
            if (string.IsNullOrEmpty(gameObjectName) || string.IsNullOrEmpty(componentType))
            {
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = "GameObject name and component type are required" }
                    },
                    IsError = true
                };
            }
            
            try
            {
                var go = GameObject.Find(gameObjectName);
                if (go == null)
                {
                    return new McpToolResult
                    {
                        Content = new List<McpContent>
                        {
                            new McpContent { Type = "text", Text = $"GameObject not found: {gameObjectName}" }
                        },
                        IsError = true
                    };
                }
                
                // 尝试多种方式查找组件类型
                Type type = null;
                
                // 1. 首先尝试直接查找（适用于完全限定名）
                type = Type.GetType(componentType);
                
                // 2. 尝试在UnityEngine命名空间中查找
                if (type == null)
                {
                    type = Type.GetType($"UnityEngine.{componentType}, UnityEngine") ?? 
                           Type.GetType($"UnityEngine.{componentType}, UnityEngine.CoreModule");
                }
                
                // 3. 在所有已加载的程序集中查找
                if (type == null)
                {
                    foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                    {
                        type = assembly.GetType(componentType);
                        if (type != null) break;
                        
                        // 尝试在各种常见命名空间中查找
                        var namespaces = new[] { "UnityEngine", "UnityEngine.UI", "UnityEngine.EventSystems", "" };
                        foreach (var ns in namespaces)
                        {
                            var fullName = string.IsNullOrEmpty(ns) ? componentType : $"{ns}.{componentType}";
                            type = assembly.GetType(fullName);
                            if (type != null) break;
                        }
                        if (type != null) break;
                    }
                }
                
                // 4. 最后尝试模糊匹配（不区分大小写）
                if (type == null)
                {
                    foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                    {
                        var types = assembly.GetTypes().Where(t => 
                            typeof(Component).IsAssignableFrom(t) && 
                            t.Name.Equals(componentType, StringComparison.OrdinalIgnoreCase));
                        
                        type = types.FirstOrDefault();
                        if (type != null) break;
                    }
                }
                
                if (type == null)
                {
                    // 获取可用的组件类型列表
                    var availableTypes = new List<string>();
                    foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                    {
                        try
                        {
                            var componentTypes = assembly.GetTypes()
                                .Where(t => typeof(Component).IsAssignableFrom(t) && !t.IsAbstract)
                                .Select(t => t.Name)
                                .Take(20); // 限制数量避免输出过长
                            availableTypes.AddRange(componentTypes);
                        }
                        catch
                        {
                            // 忽略无法访问的程序集
                        }
                    }
                    
                    var suggestion = availableTypes.Count > 0 ? 
                        $"\n\n💡 可用的组件类型示例: {string.Join(", ", availableTypes.Take(10))}" +
                        (availableTypes.Count > 10 ? "..." : "") :
                        "";
                    
                    return new McpToolResult
                    {
                        Content = new List<McpContent>
                        {
                            new McpContent { Type = "text", Text = $"❌ 未找到组件类型: {componentType}\n\n请确保：\n1. 组件类名拼写正确\n2. 脚本已编译成功\n3. 组件继承自MonoBehaviour或Component{suggestion}" }
                        },
                        IsError = true
                    };
                }
                
                // 验证类型是否为有效的组件类型
                if (!typeof(Component).IsAssignableFrom(type))
                {
                    return new McpToolResult
                    {
                        Content = new List<McpContent>
                        {
                            new McpContent { Type = "text", Text = $"❌ {componentType} 不是有效的组件类型，必须继承自Component或MonoBehaviour" }
                        },
                        IsError = true
                    };
                }
                
                var component = go.AddComponent(type);
                
#if UNITY_EDITOR
                Undo.RegisterCreatedObjectUndo(component, "Add Component");
#endif
                
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"✅ 成功添加组件\n\n🎯 游戏对象: {gameObjectName}\n🔧 组件类型: {type.Name}\n📦 完整类型: {type.FullName}\n\n组件已添加并可在Inspector中查看和配置。" }
                    }
                };
            }
            catch (Exception ex)
            {
                var errorMessage = $"❌ 添加组件失败\n\n🎯 游戏对象: {gameObjectName}\n🔧 组件类型: {componentType}\n\n错误详情: {ex.Message}";
                
                // 添加常见错误的解决建议
                if (ex.Message.Contains("already has a component"))
                {
                    errorMessage += "\n\n💡 解决建议: 该游戏对象已经包含此组件，请先移除现有组件或检查是否真的需要添加。";
                }
                else if (ex.Message.Contains("abstract"))
                {
                    errorMessage += "\n\n💡 解决建议: 无法添加抽象类组件，请使用具体的实现类。";
                }
                else if (ex.Message.Contains("interface"))
                {
                    errorMessage += "\n\n💡 解决建议: 无法添加接口类型，请使用实现该接口的具体类。";
                }
                
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = errorMessage }
                    },
                    IsError = true
                };
            }
        }
        
        public static McpToolResult FindGameObject(JObject arguments)
        {
            var name = arguments["name"]?.ToString();
            var tag = arguments["tag"]?.ToString();
            var includeInactive = arguments["includeInactive"]?.ToObject<bool>() ?? false;
            
            try
            {
                GameObject[] foundObjects = null;
                
                if (!string.IsNullOrEmpty(name))
                {
                    var go = GameObject.Find(name);
                    foundObjects = go != null ? new GameObject[] { go } : new GameObject[0];
                }
                else if (!string.IsNullOrEmpty(tag))
                {
                    foundObjects = GameObject.FindGameObjectsWithTag(tag);
                }
                
                if (!includeInactive)
                {
                    foundObjects = foundObjects?.Where(go => go.activeInHierarchy).ToArray();
                }
                
                var results = foundObjects?.Select(go => new
                {
                    name = go.name,
                    instanceId = go.GetInstanceID(),
                    tag = go.tag,
                    layer = go.layer,
                    active = go.activeInHierarchy,
                    position = go.transform.position,
                    parent = go.transform.parent?.name ?? "None"
                }).ToList();
                
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent 
                        { 
                            Type = "text", 
                            Text = $"Found {results?.Count ?? 0} GameObject(s):\n" + 
                                   string.Join("\n", results?.Select(r => $"- {r.name} (ID: {r.instanceId}, Tag: {r.tag}, Active: {r.active}, Parent: {r.parent})") ?? new string[0])
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
                        new McpContent { Type = "text", Text = $"Failed to find GameObject: {ex.Message}" }
                    },
                    IsError = true
                };
            }
        }
        
        public static McpToolResult DeleteGameObject(JObject arguments)
        {
            // 检查Play模式
            var playModeCheck = CheckPlayModeForEditing();
            if (playModeCheck != null) return playModeCheck;
            
            var name = arguments["name"]?.ToString();
            
            if (string.IsNullOrEmpty(name))
            {
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = "GameObject name is required" }
                    },
                    IsError = true
                };
            }
            
            try
            {
                var go = GameObject.Find(name);
                if (go == null)
                {
                    return new McpToolResult
                    {
                        Content = new List<McpContent>
                        {
                            new McpContent { Type = "text", Text = $"GameObject not found: {name}" }
                        },
                        IsError = true
                    };
                }
                
#if UNITY_EDITOR
                Undo.DestroyObjectImmediate(go);
#else
                UnityEngine.Object.DestroyImmediate(go);
#endif
                
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Successfully deleted GameObject: {name}" }
                    }
                };
            }
            catch (Exception ex)
            {
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Failed to delete GameObject: {ex.Message}" }
                    },
                    IsError = true
                };
            }
        }
        
        public static McpToolResult DuplicateGameObject(JObject arguments)
        {
            // 检查Play模式
            var playModeCheck = CheckPlayModeForEditing();
            if (playModeCheck != null) return playModeCheck;
            
            var name = arguments["name"]?.ToString();
            var newName = arguments["newName"]?.ToString();
            
            if (string.IsNullOrEmpty(name))
            {
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = "GameObject name is required" }
                    },
                    IsError = true
                };
            }
            
            try
            {
                var go = GameObject.Find(name);
                if (go == null)
                {
                    return new McpToolResult
                    {
                        Content = new List<McpContent>
                        {
                            new McpContent { Type = "text", Text = $"GameObject not found: {name}" }
                        },
                        IsError = true
                    };
                }
                
                var duplicate = UnityEngine.Object.Instantiate(go);
                if (!string.IsNullOrEmpty(newName))
                {
                    duplicate.name = newName;
                }
                else
                {
                    duplicate.name = go.name + " (Clone)";
                }
                
#if UNITY_EDITOR
                Undo.RegisterCreatedObjectUndo(duplicate, "Duplicate GameObject");
                Selection.activeGameObject = duplicate;
#endif
                
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Successfully duplicated GameObject: {name} -> {duplicate.name} (ID: {duplicate.GetInstanceID()})" }
                    }
                };
            }
            catch (Exception ex)
            {
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Failed to duplicate GameObject: {ex.Message}" }
                    },
                    IsError = true
                };
            }
        }
        
        public static McpToolResult SetParent(JObject arguments)
        {
            // 检查Play模式
            var playModeCheck = CheckPlayModeForEditing();
            if (playModeCheck != null) return playModeCheck;
            
            var childName = arguments["child"]?.ToString();
            var parentName = arguments["parent"]?.ToString();
            var worldPositionStays = arguments["worldPositionStays"]?.ToObject<bool>() ?? true;
            
            if (string.IsNullOrEmpty(childName))
            {
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = "Child GameObject name is required" }
                    },
                    IsError = true
                };
            }
            
            try
            {
                var child = GameObject.Find(childName);
                if (child == null)
                {
                    return new McpToolResult
                    {
                        Content = new List<McpContent>
                        {
                            new McpContent { Type = "text", Text = $"Child GameObject not found: {childName}" }
                        },
                        IsError = true
                    };
                }
                
                Transform parentTransform = null;
                if (!string.IsNullOrEmpty(parentName))
                {
                    var parent = GameObject.Find(parentName);
                    if (parent == null)
                    {
                        return new McpToolResult
                        {
                            Content = new List<McpContent>
                            {
                                new McpContent { Type = "text", Text = $"Parent GameObject not found: {parentName}" }
                            },
                            IsError = true
                        };
                    }
                    parentTransform = parent.transform;
                }
                
#if UNITY_EDITOR
                Undo.SetTransformParent(child.transform, parentTransform, "Set Parent");
#else
                child.transform.SetParent(parentTransform, worldPositionStays);
#endif
                
                var parentInfo = parentTransform != null ? parentTransform.name : "Root";
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Successfully set parent: {childName} -> {parentInfo}" }
                    }
                };
            }
            catch (Exception ex)
            {
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Failed to set parent: {ex.Message}" }
                    },
                    IsError = true
                };
            }
        }
        
        public static McpToolResult GetGameObjectInfo(JObject arguments)
        {
            var name = arguments["name"]?.ToString();
            
            if (string.IsNullOrEmpty(name))
            {
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = "GameObject name is required" }
                    },
                    IsError = true
                };
            }
            
            try
            {
                var go = GameObject.Find(name);
                if (go == null)
                {
                    return new McpToolResult
                    {
                        Content = new List<McpContent>
                        {
                            new McpContent { Type = "text", Text = $"GameObject not found: {name}" }
                        },
                        IsError = true
                    };
                }
                
                // 获取详细级别参数，默认为详细信息
                var detailLevel = arguments["detailLevel"]?.ToString()?.ToLower() ?? "detailed";
                bool isDetailed = detailLevel == "detailed" || detailLevel == "detail";
                
                var components = go.GetComponents<Component>().Select(c => c.GetType().Name).ToList();
                var children = new List<string>();
                for (int i = 0; i < go.transform.childCount; i++)
                {
                    children.Add(go.transform.GetChild(i).name);
                }
                
                string info;
                if (isDetailed)
                {
                    // 详细信息模式
                    info = $"GameObject详细信息: {name}\n" +
                          $"📋 基本信息:\n" +
                          $"  - 实例ID: {go.GetInstanceID()}\n" +
                          $"  - 标签: {go.tag}\n" +
                          $"  - 层级: {go.layer} ({LayerMask.LayerToName(go.layer)})\n" +
                          $"  - 活动状态: {go.activeInHierarchy}\n" +
                          $"  - 父对象: {(go.transform.parent?.name ?? "无")}\n\n" +
                          $"📍 变换信息:\n" +
                          $"  - 位置: {go.transform.position}\n" +
                          $"  - 旋转: {go.transform.eulerAngles}\n" +
                          $"  - 缩放: {go.transform.localScale}\n\n" +
                          $"👥 子对象 ({go.transform.childCount}个):\n";
                    
                    if (children.Count > 0)
                    {
                        // 显示前10个子对象
                        for (int i = 0; i < Math.Min(children.Count, 10); i++)
                        {
                            info += $"  - {children[i]}\n";
                        }
                        if (children.Count > 10)
                        {
                            info += $"  - ... 还有 {children.Count - 10} 个子对象\n";
                        }
                    }
                    else
                    {
                        info += "  - 无子对象\n";
                    }
                    
                    info += $"\n🔧 组件 ({components.Count}个):\n";
                    foreach (var comp in components)
                    {
                        info += $"  - {comp}\n";
                    }
                }
                else
                {
                    // 简单信息模式
                    var status = go.activeInHierarchy ? "✓" : "✗";
                    var parentInfo = go.transform.parent?.name ?? "根级别";
                    
                    info = $"{status} {name}\n" +
                          $"位置: {go.transform.position}\n" +
                          $"父对象: {parentInfo}\n" +
                          $"组件数: {components.Count}, 子对象数: {children.Count}";
                }
                
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = info }
                    }
                };
            }
            catch (Exception ex)
            {
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Failed to get GameObject info: {ex.Message}" }
                    },
                    IsError = true
                };
            }
        }
        
        public static McpToolResult RemoveComponent(JObject arguments)
        {
            // 检查Play模式
            var playModeCheck = CheckPlayModeForEditing();
            if (playModeCheck != null) return playModeCheck;
            
            var gameObjectName = arguments["gameObject"]?.ToString();
            var componentType = arguments["component"]?.ToString();
            
            if (string.IsNullOrEmpty(gameObjectName) || string.IsNullOrEmpty(componentType))
            {
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = "GameObject name and component type are required" }
                    },
                    IsError = true
                };
            }
            
            try
            {
                var go = GameObject.Find(gameObjectName);
                if (go == null)
                {
                    return new McpToolResult
                    {
                        Content = new List<McpContent>
                        {
                            new McpContent { Type = "text", Text = $"GameObject not found: {gameObjectName}" }
                        },
                        IsError = true
                    };
                }
                
                var type = Type.GetType(componentType) ?? Type.GetType($"UnityEngine.{componentType}, UnityEngine");
                if (type == null)
                {
                    return new McpToolResult
                    {
                        Content = new List<McpContent>
                        {
                            new McpContent { Type = "text", Text = $"Component type not found: {componentType}" }
                        },
                        IsError = true
                    };
                }
                
                var component = go.GetComponent(type);
                if (component == null)
                {
                    return new McpToolResult
                    {
                        Content = new List<McpContent>
                        {
                            new McpContent { Type = "text", Text = $"Component {componentType} not found on GameObject {gameObjectName}" }
                        },
                        IsError = true
                    };
                }
                
#if UNITY_EDITOR
                Undo.DestroyObjectImmediate(component);
#else
                UnityEngine.Object.DestroyImmediate(component);
#endif
                
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Successfully removed component {componentType} from {gameObjectName}" }
                    }
                };
            }
            catch (Exception ex)
            {
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Failed to remove component: {ex.Message}" }
                    },
                    IsError = true
                };
            }
        }
        
        public static McpToolResult SetTransform(JObject arguments)
        {
            var gameObjectName = arguments["gameObject"]?.ToString();
            
            if (string.IsNullOrEmpty(gameObjectName))
            {
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = "GameObject name is required" }
                    },
                    IsError = true
                };
            }
            
            try
            {
                var go = GameObject.Find(gameObjectName);
                if (go == null)
                {
                    return new McpToolResult
                    {
                        Content = new List<McpContent>
                        {
                            new McpContent { Type = "text", Text = $"GameObject not found: {gameObjectName}" }
                        },
                        IsError = true
                    };
                }
                
#if UNITY_EDITOR
                Undo.RecordObject(go.transform, "Set Transform");
#endif
                
                var changes = new List<string>();
                
                if (arguments["position"] != null)
                {
                    var pos = arguments["position"];
                    var position = new Vector3(
                        pos["x"]?.ToObject<float>() ?? go.transform.position.x,
                        pos["y"]?.ToObject<float>() ?? go.transform.position.y,
                        pos["z"]?.ToObject<float>() ?? go.transform.position.z
                    );
                    go.transform.position = position;
                    changes.Add($"position to {position}");
                }
                
                if (arguments["rotation"] != null)
                {
                    var rot = arguments["rotation"];
                    var rotation = new Vector3(
                        rot["x"]?.ToObject<float>() ?? go.transform.eulerAngles.x,
                        rot["y"]?.ToObject<float>() ?? go.transform.eulerAngles.y,
                        rot["z"]?.ToObject<float>() ?? go.transform.eulerAngles.z
                    );
                    go.transform.eulerAngles = rotation;
                    changes.Add($"rotation to {rotation}");
                }
                
                if (arguments["scale"] != null)
                {
                    var scl = arguments["scale"];
                    var scale = new Vector3(
                        scl["x"]?.ToObject<float>() ?? go.transform.localScale.x,
                        scl["y"]?.ToObject<float>() ?? go.transform.localScale.y,
                        scl["z"]?.ToObject<float>() ?? go.transform.localScale.z
                    );
                    go.transform.localScale = scale;
                    changes.Add($"scale to {scale}");
                }
                
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Updated {gameObjectName}: {string.Join(", ", changes)}" }
                    }
                };
            }
            catch (Exception ex)
            {
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Failed to set transform: {ex.Message}" }
                    },
                    IsError = true
                };
            }
        }
    }
}