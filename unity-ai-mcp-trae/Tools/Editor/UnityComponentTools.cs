using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Newtonsoft.Json.Linq;
using Unity.MCP;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Unity.MCP.Editor
{
    /// <summary>
    /// Unity组件管理工具
    /// </summary>
    public static class UnityComponentTools
    {
        public static McpToolResult AddComponent(JObject arguments)
        {
            var gameObjectName = arguments["gameObject"]?.ToString();
            var componentType = arguments["component"]?.ToString();
            var maxRetries = arguments["maxRetries"]?.ToObject<int>() ?? 3;
            var retryDelay = arguments["retryDelay"]?.ToObject<int>() ?? 100;
            
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
            
            var errors = new List<string>();
            
            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    // 查找游戏对象，支持多种查找方式
                    GameObject go = FindGameObjectWithFallback(gameObjectName);
                    
                    if (go == null)
                    {
                        var error = $"GameObject not found: {gameObjectName} (attempt {attempt + 1}/{maxRetries})";
                        errors.Add(error);
                        
                        if (attempt < maxRetries - 1)
                        {
                            System.Threading.Thread.Sleep(retryDelay);
                            continue;
                        }
                        
                        return new McpToolResult
                        {
                            Content = new List<McpContent>
                            {
                                new McpContent { Type = "text", Text = $"GameObject not found after {maxRetries} attempts: {gameObjectName}\nErrors: {string.Join("; ", errors)}" }
                            },
                            IsError = true
                        };
                    }
                    
                    // 检查组件是否已存在
                    Type componentTypeObj = ResolveComponentType(componentType);
                    if (componentTypeObj == null)
                    {
                        return new McpToolResult
                        {
                            Content = new List<McpContent>
                            {
                                new McpContent { Type = "text", Text = $"Component type not found: {componentType}. Available types include: Transform, Rigidbody, Collider, MeshRenderer, etc." }
                            },
                            IsError = true
                        };
                    }
                    
                    // 检查是否已有该组件
                    if (go.GetComponent(componentTypeObj) != null)
                    {
                        return new McpToolResult
                        {
                            Content = new List<McpContent>
                            {
                                new McpContent { Type = "text", Text = $"Component {componentType} already exists on {gameObjectName}" }
                            },
                            IsError = false
                        };
                    }
                    
                    // 添加组件
                    var component = go.AddComponent(componentTypeObj);
                    
#if UNITY_EDITOR
                    Undo.RegisterCreatedObjectUndo(component, "Add Component");
                    EditorUtility.SetDirty(go);
#endif
                    
                    return new McpToolResult
                    {
                        Content = new List<McpContent>
                        {
                            new McpContent { Type = "text", Text = $"Successfully added component {componentType} to {gameObjectName} (attempt {attempt + 1})" }
                        }
                    };
                }
                catch (Exception ex)
                {
                    var error = $"Attempt {attempt + 1}: {ex.Message}";
                    errors.Add(error);
                    
                    if (attempt < maxRetries - 1)
                    {
                        System.Threading.Thread.Sleep(retryDelay);
                        continue;
                    }
                    
                    return new McpToolResult
                    {
                        Content = new List<McpContent>
                        {
                            new McpContent { Type = "text", Text = $"Failed to add component after {maxRetries} attempts: {componentType}\nErrors: {string.Join("; ", errors)}" }
                        },
                        IsError = true
                    };
                }
            }
            
            return new McpToolResult
            {
                Content = new List<McpContent>
                {
                    new McpContent { Type = "text", Text = $"Unexpected error: Failed to add component {componentType} to {gameObjectName}" }
                },
                IsError = true
            };
        }
        
        /// <summary>
        /// 使用多种方式查找游戏对象
        /// </summary>
        private static GameObject FindGameObjectWithFallback(string name)
        {
            // 1. 直接按名称查找
            var go = GameObject.Find(name);
            if (go != null) return go;
            
            // 2. 查找包含非活动对象
            var allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
            go = allObjects.FirstOrDefault(obj => obj.name == name && obj.scene.isLoaded);
            if (go != null) return go;
            
            // 3. 模糊匹配
            go = allObjects.FirstOrDefault(obj => obj.name.Contains(name) && obj.scene.isLoaded);
            if (go != null) return go;
            
            return null;
        }
        
        /// <summary>
        /// 解析组件类型，支持多种命名方式
        /// </summary>
        private static Type ResolveComponentType(string componentType)
        {
            // 常见组件类型映射
            var typeMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "rigidbody", "Rigidbody" },
                { "collider", "BoxCollider" },
                { "boxcollider", "BoxCollider" },
                { "spherecollider", "SphereCollider" },
                { "capsulecollider", "CapsuleCollider" },
                { "meshcollider", "MeshCollider" },
                { "renderer", "MeshRenderer" },
                { "meshrenderer", "MeshRenderer" },
                { "light", "Light" },
                { "camera", "Camera" },
                { "audiosource", "AudioSource" },
                { "animator", "Animator" },
                { "canvas", "Canvas" },
                { "button", "Button" },
                { "text", "Text" },
                { "image", "Image" }
            };
            
            // 使用映射表查找
            if (typeMap.TryGetValue(componentType, out string mappedType))
            {
                componentType = mappedType;
            }
            
            // 1. 首先尝试直接按类型名查找（支持用户自定义脚本）
            var allAssemblies = System.AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in allAssemblies)
            {
                try
                {
                    var type = assembly.GetType(componentType);
                    if (type != null && typeof(Component).IsAssignableFrom(type))
                    {
                        return type;
                    }
                    
                    // 也尝试在程序集中搜索所有类型
                    var types = assembly.GetTypes();
                    foreach (var t in types)
                    {
                        if (t.Name.Equals(componentType, StringComparison.OrdinalIgnoreCase) && 
                            typeof(Component).IsAssignableFrom(t))
                        {
                            return t;
                        }
                    }
                }
                catch (Exception)
                {
                    // 忽略无法访问的程序集
                    continue;
                }
            }
            
            // 2. 尝试Unity内置命名空间
            var namespaces = new[]
            {
                "UnityEngine",
                "UnityEngine.UI",
                "UnityEngine.Audio",
                "UnityEngine.Rendering"
            };
            
            var unityAssemblies = new[]
            {
                "UnityEngine",
                "UnityEngine.CoreModule",
                "UnityEngine.UIModule",
                "UnityEngine.AudioModule",
                "UnityEngine.PhysicsModule"
            };
            
            foreach (var ns in namespaces)
            {
                foreach (var assembly in unityAssemblies)
                {
                    var fullTypeName = $"{ns}.{componentType}, {assembly}";
                    var type = Type.GetType(fullTypeName);
                    if (type != null && typeof(Component).IsAssignableFrom(type))
                    {
                        return type;
                    }
                }
            }
            
            return null;
        }
        
        public static McpToolResult GetComponentProperties(JObject arguments)
        {
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
                
                var properties = new List<string>();
                var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
                var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                
                foreach (var field in fields)
                {
                    try
                    {
                        var value = field.GetValue(component);
                        properties.Add($"{field.Name}: {value} ({field.FieldType.Name})");
                    }
                    catch
                    {
                        properties.Add($"{field.Name}: <unable to read> ({field.FieldType.Name})");
                    }
                }
                
                foreach (var prop in props)
                {
                    if (prop.CanRead && prop.GetIndexParameters().Length == 0)
                    {
                        try
                        {
                            var value = prop.GetValue(component);
                            properties.Add($"{prop.Name}: {value} ({prop.PropertyType.Name})");
                        }
                        catch
                        {
                            properties.Add($"{prop.Name}: <unable to read> ({prop.PropertyType.Name})");
                        }
                    }
                }
                
                var info = $"Component Properties: {componentType} on {gameObjectName}\n" +
                          string.Join("\n", properties);
                
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
                        new McpContent { Type = "text", Text = $"Failed to get component properties: {ex.Message}" }
                    },
                    IsError = true
                };
            }
        }
        
        public static McpToolResult SetComponentProperties(JObject arguments)
        {
            var gameObjectName = arguments["gameObject"]?.ToString();
            var componentType = arguments["component"]?.ToString();
            var properties = arguments["properties"] as JObject;
            
            if (string.IsNullOrEmpty(gameObjectName) || string.IsNullOrEmpty(componentType) || properties == null)
            {
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = "GameObject name, component type, and properties are required" }
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
                Undo.RecordObject(component, "Set Component Properties");
#endif
                
                var setProperties = new List<string>();
                foreach (var prop in properties)
                {
                    try
                    {
                        var field = type.GetField(prop.Key, BindingFlags.Public | BindingFlags.Instance);
                        var property = type.GetProperty(prop.Key, BindingFlags.Public | BindingFlags.Instance);
                        
                        if (field != null)
                        {
                            var value = Convert.ChangeType(prop.Value.ToObject<object>(), field.FieldType);
                            field.SetValue(component, value);
                            setProperties.Add($"{prop.Key} = {value}");
                        }
                        else if (property != null && property.CanWrite)
                        {
                            var value = Convert.ChangeType(prop.Value.ToObject<object>(), property.PropertyType);
                            property.SetValue(component, value);
                            setProperties.Add($"{prop.Key} = {value}");
                        }
                        else
                        {
                            setProperties.Add($"{prop.Key}: property not found or not writable");
                        }
                    }
                    catch (Exception ex)
                    {
                        setProperties.Add($"{prop.Key}: failed to set - {ex.Message}");
                    }
                }
                
#if UNITY_EDITOR
                EditorUtility.SetDirty(component);
#endif
                
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Set properties on {componentType} of {gameObjectName}:\n" + string.Join("\n", setProperties) }
                    }
                };
            }
            catch (Exception ex)
            {
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Failed to set component properties: {ex.Message}" }
                    },
                    IsError = true
                };
            }
        }
        
        public static McpToolResult ListComponents(JObject arguments)
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
                
                var components = go.GetComponents<Component>();
                var componentInfo = components.Select(c => new
                {
                    name = c.GetType().Name,
                    fullName = c.GetType().FullName,
                    enabled = c is Behaviour behaviour ? behaviour.enabled : true,
                    instanceId = c.GetInstanceID()
                }).ToList();
                
                var info = $"Components on {gameObjectName} ({componentInfo.Count} total):\n" +
                          string.Join("\n", componentInfo.Select(c => $"- {c.name} (ID: {c.instanceId}, Enabled: {c.enabled})"));
                
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
                        new McpContent { Type = "text", Text = $"Failed to list components: {ex.Message}" }
                    },
                    IsError = true
                };
            }
        }
        
        /// <summary>
        /// 获取游戏对象的所有组件信息
        /// </summary>
        /// <param name="arguments">包含gameObject参数的JSON对象</param>
        /// <returns>操作结果</returns>
        public static McpToolResult GetAllComponents(JObject arguments)
        {
            return ListComponents(arguments);
        }
        
        /// <summary>
        /// 从游戏对象移除组件
        /// </summary>
        /// <param name="arguments">包含gameObject和component参数的JSON对象</param>
        /// <returns>操作结果</returns>
        public static McpToolResult RemoveComponent(JObject arguments)
        {
            try
            {
                if (arguments == null)
                {
                    return new McpToolResult
                    {
                        Content = new List<McpContent>
                        {
                            new McpContent { Type = "text", Text = "Arguments cannot be null" }
                        },
                        IsError = true
                    };
                }

                string gameObjectName = arguments["gameObject"]?.ToString();
                string componentType = arguments["component"]?.ToString();

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

                if (string.IsNullOrEmpty(componentType))
                {
                    return new McpToolResult
                    {
                        Content = new List<McpContent>
                        {
                            new McpContent { Type = "text", Text = "Component type is required" }
                        },
                        IsError = true
                    };
                }

                GameObject go = GameObject.Find(gameObjectName);
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

                // 查找组件类型
                Type type = Type.GetType(componentType);
                if (type == null)
                {
                    // 尝试在UnityEngine命名空间中查找
                    type = Type.GetType($"UnityEngine.{componentType}");
                }
                if (type == null)
                {
                    // 尝试在当前程序集中查找
                    var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
                    foreach (var assembly in assemblies)
                    {
                        type = assembly.GetType(componentType);
                        if (type != null) break;
                    }
                }

                if (type == null || !typeof(Component).IsAssignableFrom(type))
                {
                    return new McpToolResult
                    {
                        Content = new List<McpContent>
                        {
                            new McpContent { Type = "text", Text = $"Invalid component type: {componentType}" }
                        },
                        IsError = true
                    };
                }

                // 查找并移除组件
                Component component = go.GetComponent(type);
                if (component == null)
                {
                    return new McpToolResult
                    {
                        Content = new List<McpContent>
                        {
                            new McpContent { Type = "text", Text = $"Component {componentType} not found on {gameObjectName}" }
                        },
                        IsError = true
                    };
                }

#if UNITY_EDITOR
                // 在编辑器中注册撤销操作
                Undo.DestroyObjectImmediate(component);
#else
                // 在运行时销毁组件
                UnityEngine.Object.DestroyImmediate(component);
#endif

                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Successfully removed {componentType} from {gameObjectName}" }
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
    }
}