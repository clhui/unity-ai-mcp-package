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
        public static McpToolResult CreateGameObject(JObject arguments)
        {
            var name = arguments["name"]?.ToString() ?? "GameObject";
            var parentPath = arguments["parent"]?.ToString();
            
            try
            {
                var go = new GameObject(name);
                
                if (!string.IsNullOrEmpty(parentPath))
                {
                    var parent = GameObject.Find(parentPath);
                    if (parent != null)
                    {
                        go.transform.SetParent(parent.transform);
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
                
                var type = Type.GetType($"UnityEngine.{componentType}, UnityEngine") ?? 
                          Type.GetType($"UnityEngine.{componentType}, UnityEngine.CoreModule");
                
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
                
                var component = go.AddComponent(type);
                
#if UNITY_EDITOR
                Undo.RegisterCreatedObjectUndo(component, "Add Component");
#endif
                
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Added component {componentType} to {gameObjectName}" }
                    }
                };
            }
            catch (Exception ex)
            {
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Failed to add component: {ex.Message}" }
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
                
                var components = go.GetComponents<Component>().Select(c => c.GetType().Name).ToList();
                var children = new List<string>();
                for (int i = 0; i < go.transform.childCount; i++)
                {
                    children.Add(go.transform.GetChild(i).name);
                }
                
                var info = $"GameObject Information: {name}\n" +
                          $"Instance ID: {go.GetInstanceID()}\n" +
                          $"Tag: {go.tag}\n" +
                          $"Layer: {go.layer} ({LayerMask.LayerToName(go.layer)})\n" +
                          $"Active: {go.activeInHierarchy}\n" +
                          $"Position: {go.transform.position}\n" +
                          $"Rotation: {go.transform.eulerAngles}\n" +
                          $"Scale: {go.transform.localScale}\n" +
                          $"Parent: {(go.transform.parent?.name ?? "None")}\n" +
                          $"Children Count: {go.transform.childCount}\n" +
                          $"Children: [{string.Join(", ", children)}]\n" +
                          $"Components Count: {components.Count}\n" +
                          $"Components: [{string.Join(", ", components)}]";
                
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