using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json.Linq;
#if UNITY_EDITOR
using UnityEditor;
using Unity.MCP;
#endif

namespace Unity.MCP.Tools.Editor
{
    public static class UnityMaterialTools
    {
        public static McpToolResult CreateMaterial(JObject arguments)
        {
            var name = arguments["name"]?.ToString();
            var shaderName = arguments["shader"]?.ToString() ?? "Standard";
            
            if (string.IsNullOrEmpty(name))
            {
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = "Material name is required" }
                    },
                    IsError = true
                };
            }
            
            try
            {
                var shader = Shader.Find(shaderName);
                if (shader == null)
                {
                    return new McpToolResult
                    {
                        Content = new List<McpContent>
                        {
                            new McpContent { Type = "text", Text = $"Shader not found: {shaderName}" }
                        },
                        IsError = true
                    };
                }
                
                var material = new Material(shader);
                material.name = name;
                
#if UNITY_EDITOR
                var path = $"Assets/Materials/{name}.mat";
                var directory = Path.GetDirectoryName(path);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                AssetDatabase.CreateAsset(material, path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Successfully created material: {name} with shader {shaderName} at {path}" }
                    }
                };
#else
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Successfully created material: {name} with shader {shaderName} (runtime only)" }
                    }
                };
#endif
            }
            catch (Exception ex)
            {
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Failed to create material: {ex.Message}" }
                    },
                    IsError = true
                };
            }
        }
        
        public static McpToolResult SetMaterialProperties(JObject arguments)
        {
            var materialPath = arguments["materialPath"]?.ToString();
            var properties = arguments["properties"] as JObject;
            
            if (string.IsNullOrEmpty(materialPath) || properties == null)
            {
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = "Material path and properties are required" }
                    },
                    IsError = true
                };
            }
            
            try
            {
#if UNITY_EDITOR
                var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (material == null)
                {
                    return new McpToolResult
                    {
                        Content = new List<McpContent>
                        {
                            new McpContent { Type = "text", Text = $"Material not found at path: {materialPath}" }
                        },
                        IsError = true
                    };
                }
                
                Undo.RecordObject(material, "Set Material Properties");
#else
                // Runtime fallback - try to find material by name
                var materialName = Path.GetFileNameWithoutExtension(materialPath);
                var materials = Resources.FindObjectsOfTypeAll<Material>();
                var material = materials.FirstOrDefault(m => m.name == materialName);
                
                if (material == null)
                {
                    return new McpToolResult
                    {
                        Content = new List<McpContent>
                        {
                            new McpContent { Type = "text", Text = $"Material not found: {materialName}" }
                        },
                        IsError = true
                    };
                }
#endif
                
                var setProperties = new List<string>();
                foreach (var prop in properties)
                {
                    try
                    {
                        var propertyName = prop.Key;
                        var value = prop.Value;
                        
                        if (material.HasProperty(propertyName))
                        {
                            var shader = material.shader;
                            var propertyIndex = -1;
                            for (int i = 0; i < shader.GetPropertyCount(); i++)
                            {
                                if (shader.GetPropertyName(i) == propertyName)
                                {
                                    propertyIndex = i;
                                    break;
                                }
                            }
                            
                            if (propertyIndex == -1) continue;
                            var propertyType = shader.GetPropertyType(propertyIndex);
                            
                            switch (propertyType)
                            {
                                case UnityEngine.Rendering.ShaderPropertyType.Color:
                                    var colorValue = value.ToObject<Color>();
                                    material.SetColor(propertyName, colorValue);
                                    setProperties.Add($"{propertyName} = {colorValue}");
                                    break;
                                    
                                case UnityEngine.Rendering.ShaderPropertyType.Float:
                                case UnityEngine.Rendering.ShaderPropertyType.Range:
                                    var floatValue = value.ToObject<float>();
                                    material.SetFloat(propertyName, floatValue);
                                    setProperties.Add($"{propertyName} = {floatValue}");
                                    break;
                                    
                                case UnityEngine.Rendering.ShaderPropertyType.Vector:
                                    var vectorValue = value.ToObject<Vector4>();
                                    material.SetVector(propertyName, vectorValue);
                                    setProperties.Add($"{propertyName} = {vectorValue}");
                                    break;
                                    
                                case UnityEngine.Rendering.ShaderPropertyType.Texture:
                                    var texturePath = value.ToString();
#if UNITY_EDITOR
                                    var texture = AssetDatabase.LoadAssetAtPath<Texture>(texturePath);
#else
                                    var texture = Resources.Load<Texture>(texturePath);
#endif
                                    if (texture != null)
                                    {
                                        material.SetTexture(propertyName, texture);
                                        setProperties.Add($"{propertyName} = {texture.name}");
                                    }
                                    else
                                    {
                                        setProperties.Add($"{propertyName}: texture not found at {texturePath}");
                                    }
                                    break;
                                    
                                default:
                                    setProperties.Add($"{propertyName}: unsupported property type {propertyType}");
                                    break;
                            }
                        }
                        else
                        {
                            setProperties.Add($"{propertyName}: property not found on material");
                        }
                    }
                    catch (Exception ex)
                    {
                        setProperties.Add($"{prop.Key}: failed to set - {ex.Message}");
                    }
                }
                
#if UNITY_EDITOR
                EditorUtility.SetDirty(material);
                AssetDatabase.SaveAssets();
#endif
                
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Set properties on material {material.name}:\n" + string.Join("\n", setProperties) }
                    }
                };
            }
            catch (Exception ex)
            {
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Failed to set material properties: {ex.Message}" }
                    },
                    IsError = true
                };
            }
        }
        
        public static McpToolResult AssignMaterial(JObject arguments)
        {
            var gameObjectName = arguments["gameObject"]?.ToString();
            var materialPath = arguments["materialPath"]?.ToString();
            var rendererIndex = arguments["rendererIndex"]?.ToObject<int>() ?? 0;
            
            if (string.IsNullOrEmpty(gameObjectName) || string.IsNullOrEmpty(materialPath))
            {
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = "GameObject name and material path are required" }
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
                
                var renderer = go.GetComponent<Renderer>();
                if (renderer == null)
                {
                    return new McpToolResult
                    {
                        Content = new List<McpContent>
                        {
                            new McpContent { Type = "text", Text = $"No Renderer component found on GameObject: {gameObjectName}" }
                        },
                        IsError = true
                    };
                }
                
#if UNITY_EDITOR
                var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
#else
                var materialName = Path.GetFileNameWithoutExtension(materialPath);
                var material = Resources.Load<Material>(materialName);
#endif
                
                if (material == null)
                {
                    return new McpToolResult
                    {
                        Content = new List<McpContent>
                        {
                            new McpContent { Type = "text", Text = $"Material not found at path: {materialPath}" }
                        },
                        IsError = true
                    };
                }
                
#if UNITY_EDITOR
                Undo.RecordObject(renderer, "Assign Material");
#endif
                
                var materials = renderer.sharedMaterials;
                if (rendererIndex >= 0 && rendererIndex < materials.Length)
                {
                    materials[rendererIndex] = material;
                    renderer.sharedMaterials = materials;
                }
                else
                {
                    renderer.sharedMaterial = material;
                }
                
#if UNITY_EDITOR
                EditorUtility.SetDirty(renderer);
#endif
                
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Successfully assigned material {material.name} to {gameObjectName} (index: {rendererIndex})" }
                    }
                };
            }
            catch (Exception ex)
            {
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Failed to assign material: {ex.Message}" }
                    },
                    IsError = true
                };
            }
        }
        
        public static McpToolResult SetRendererProperties(JObject arguments)
        {
            var gameObjectName = arguments["gameObject"]?.ToString();
            var properties = arguments["properties"] as JObject;
            
            if (string.IsNullOrEmpty(gameObjectName) || properties == null)
            {
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = "GameObject name and properties are required" }
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
                
                var renderer = go.GetComponent<Renderer>();
                if (renderer == null)
                {
                    return new McpToolResult
                    {
                        Content = new List<McpContent>
                        {
                            new McpContent { Type = "text", Text = $"No Renderer component found on GameObject: {gameObjectName}" }
                        },
                        IsError = true
                    };
                }
                
#if UNITY_EDITOR
                Undo.RecordObject(renderer, "Set Renderer Properties");
#endif
                
                var setProperties = new List<string>();
                foreach (var prop in properties)
                {
                    try
                    {
                        switch (prop.Key.ToLower())
                        {
                            case "enabled":
                                renderer.enabled = prop.Value.ToObject<bool>();
                                setProperties.Add($"enabled = {renderer.enabled}");
                                break;
                                
                            case "castshadows":
                                var shadowCasting = (UnityEngine.Rendering.ShadowCastingMode)prop.Value.ToObject<int>();
                                renderer.shadowCastingMode = shadowCasting;
                                setProperties.Add($"shadowCastingMode = {shadowCasting}");
                                break;
                                
                            case "receiveshadows":
                                renderer.receiveShadows = prop.Value.ToObject<bool>();
                                setProperties.Add($"receiveShadows = {renderer.receiveShadows}");
                                break;
                                
                            case "lightprobeusage":
                                var lightProbeUsage = (UnityEngine.Rendering.LightProbeUsage)prop.Value.ToObject<int>();
                                renderer.lightProbeUsage = lightProbeUsage;
                                setProperties.Add($"lightProbeUsage = {lightProbeUsage}");
                                break;
                                
                            case "sortingorder":
                                renderer.sortingOrder = prop.Value.ToObject<int>();
                                setProperties.Add($"sortingOrder = {renderer.sortingOrder}");
                                break;
                                
                            case "sortinglayername":
                                renderer.sortingLayerName = prop.Value.ToString();
                                setProperties.Add($"sortingLayerName = {renderer.sortingLayerName}");
                                break;
                                
                            default:
                                setProperties.Add($"{prop.Key}: unknown renderer property");
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        setProperties.Add($"{prop.Key}: failed to set - {ex.Message}");
                    }
                }
                
#if UNITY_EDITOR
                EditorUtility.SetDirty(renderer);
#endif
                
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Set renderer properties on {gameObjectName}:\n" + string.Join("\n", setProperties) }
                    }
                };
            }
            catch (Exception ex)
            {
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Failed to set renderer properties: {ex.Message}" }
                    },
                    IsError = true
                };
            }
        }
        
        /// <summary>
        /// 应用材质到游戏对象
        /// </summary>
        /// <param name="arguments">包含gameObject、materialPath等参数的JSON对象</param>
        /// <returns>操作结果</returns>
        public static McpToolResult ApplyMaterial(JObject arguments)
        {
            var gameObjectName = arguments["gameObject"]?.ToString();
            var materialPath = arguments["materialPath"]?.ToString();
            
            if (string.IsNullOrEmpty(gameObjectName) || string.IsNullOrEmpty(materialPath))
            {
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = "GameObject name and material path are required" }
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
                
                var renderer = go.GetComponent<Renderer>();
                if (renderer == null)
                {
                    return new McpToolResult
                    {
                        Content = new List<McpContent>
                        {
                            new McpContent { Type = "text", Text = $"No Renderer component found on GameObject: {gameObjectName}" }
                        },
                        IsError = true
                    };
                }
                
#if UNITY_EDITOR
                var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
#else
                var materialName = Path.GetFileNameWithoutExtension(materialPath);
                var materials = Resources.FindObjectsOfTypeAll<Material>();
                var material = materials.FirstOrDefault(m => m.name == materialName);
#endif
                
                if (material == null)
                {
                    return new McpToolResult
                    {
                        Content = new List<McpContent>
                        {
                            new McpContent { Type = "text", Text = $"Material not found at path: {materialPath}" }
                        },
                        IsError = true
                    };
                }
                
#if UNITY_EDITOR
                Undo.RecordObject(renderer, "Apply Material");
#endif
                
                renderer.material = material;
                
#if UNITY_EDITOR
                EditorUtility.SetDirty(renderer);
#endif
                
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Applied material {material.name} to {gameObjectName}" }
                    }
                };
            }
            catch (Exception ex)
            {
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Failed to apply material: {ex.Message}" }
                    },
                    IsError = true
                };
            }
        }
    }
}