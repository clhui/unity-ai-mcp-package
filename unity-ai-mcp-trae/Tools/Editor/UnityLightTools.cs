using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Newtonsoft.Json.Linq;
using Unity.MCP;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Unity.MCP.Tools.Editor
{
    /// <summary>
    /// Unity光照工具类
    /// </summary>
    public static class UnityLightTools
    {
        /// <summary>
        /// 创建光源
        /// </summary>
        /// <param name="arguments">包含光源参数的JSON对象</param>
        /// <returns>操作结果</returns>
        public static McpToolResult CreateLight(JObject arguments)
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

                string lightName = arguments["name"]?.ToString() ?? "New Light";
                string lightTypeStr = arguments["type"]?.ToString() ?? "Directional";
                
                // 解析光源类型
                if (!Enum.TryParse<LightType>(lightTypeStr, true, out LightType lightType))
                {
                    return new McpToolResult
                    {
                        Content = new List<McpContent>
                        {
                            new McpContent { Type = "text", Text = $"Invalid light type: {lightTypeStr}. Valid types: Directional, Point, Spot, Area" }
                        },
                        IsError = true
                    };
                }

                // 创建游戏对象
                GameObject lightGO = new GameObject(lightName);
                
#if UNITY_EDITOR
                Undo.RegisterCreatedObjectUndo(lightGO, "Create Light");
#endif

                // 添加Light组件
                Light light = lightGO.AddComponent<Light>();
                light.type = lightType;

                // 设置位置
                if (arguments["position"] != null)
                {
                    var posArray = arguments["position"] as JArray;
                    if (posArray != null && posArray.Count >= 3)
                    {
                        Vector3 position = new Vector3(
                            posArray[0].ToObject<float>(),
                            posArray[1].ToObject<float>(),
                            posArray[2].ToObject<float>()
                        );
                        lightGO.transform.position = position;
                    }
                }

                // 设置旋转
                if (arguments["rotation"] != null)
                {
                    var rotArray = arguments["rotation"] as JArray;
                    if (rotArray != null && rotArray.Count >= 3)
                    {
                        Vector3 rotation = new Vector3(
                            rotArray[0].ToObject<float>(),
                            rotArray[1].ToObject<float>(),
                            rotArray[2].ToObject<float>()
                        );
                        lightGO.transform.rotation = Quaternion.Euler(rotation);
                    }
                }

                // 设置基本属性
                if (arguments["color"] != null)
                {
                    var colorArray = arguments["color"] as JArray;
                    if (colorArray != null && colorArray.Count >= 3)
                    {
                        Color color = new Color(
                            colorArray[0].ToObject<float>(),
                            colorArray[1].ToObject<float>(),
                            colorArray[2].ToObject<float>(),
                            colorArray.Count > 3 ? colorArray[3].ToObject<float>() : 1f
                        );
                        light.color = color;
                    }
                }

                if (arguments["intensity"] != null)
                {
                    light.intensity = arguments["intensity"].ToObject<float>();
                }

                if (arguments["range"] != null && (lightType == LightType.Point || lightType == LightType.Spot))
                {
                    light.range = arguments["range"].ToObject<float>();
                }

                if (arguments["spotAngle"] != null && lightType == LightType.Spot)
                {
                    light.spotAngle = arguments["spotAngle"].ToObject<float>();
                }

#if UNITY_EDITOR
                Selection.activeGameObject = lightGO;
                EditorUtility.SetDirty(lightGO);
#endif

                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Created {lightType} light '{lightName}' at position {lightGO.transform.position}" }
                    }
                };
            }
            catch (Exception ex)
            {
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Failed to create light: {ex.Message}" }
                    },
                    IsError = true
                };
            }
        }

        /// <summary>
        /// 设置光源属性
        /// </summary>
        /// <param name="arguments">包含gameObject和properties参数的JSON对象</param>
        /// <returns>操作结果</returns>
        public static McpToolResult SetLightProperties(JObject arguments)
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

                Light light = go.GetComponent<Light>();
                if (light == null)
                {
                    return new McpToolResult
                    {
                        Content = new List<McpContent>
                        {
                            new McpContent { Type = "text", Text = $"No Light component found on GameObject: {gameObjectName}" }
                        },
                        IsError = true
                    };
                }

                var properties = arguments["properties"] as JObject;
                if (properties == null)
                {
                    return new McpToolResult
                    {
                        Content = new List<McpContent>
                        {
                            new McpContent { Type = "text", Text = "Properties parameter is required" }
                        },
                        IsError = true
                    };
                }

#if UNITY_EDITOR
                Undo.RecordObject(light, "Set Light Properties");
#endif

                var setProperties = new List<string>();

                // 设置各种光源属性
                foreach (var prop in properties)
                {
                    try
                    {
                        switch (prop.Key.ToLower())
                        {
                            case "type":
                                if (Enum.TryParse<LightType>(prop.Value.ToString(), true, out var lightType))
                                {
                                    light.type = lightType;
                                    setProperties.Add($"type = {light.type}");
                                }
                                else
                                {
                                    setProperties.Add($"type: invalid value {prop.Value}");
                                }
                                break;
                            case "color":
                                var colorArray = prop.Value as JArray;
                                if (colorArray != null && colorArray.Count >= 3)
                                {
                                    Color color = new Color(
                                        colorArray[0].ToObject<float>(),
                                        colorArray[1].ToObject<float>(),
                                        colorArray[2].ToObject<float>(),
                                        colorArray.Count > 3 ? colorArray[3].ToObject<float>() : 1f
                                    );
                                    light.color = color;
                                    setProperties.Add($"color = {light.color}");
                                }
                                else
                                {
                                    setProperties.Add($"color: invalid format, expected [r,g,b] or [r,g,b,a]");
                                }
                                break;
                            case "intensity":
                                light.intensity = Mathf.Max(0f, prop.Value.ToObject<float>());
                                setProperties.Add($"intensity = {light.intensity}");
                                break;
                            case "range":
                                if (light.type == LightType.Point || light.type == LightType.Spot)
                                {
                                    light.range = Mathf.Max(0f, prop.Value.ToObject<float>());
                                    setProperties.Add($"range = {light.range}");
                                }
                                else
                                {
                                    setProperties.Add($"range: not applicable for {light.type} light");
                                }
                                break;
                            case "spotAngle":
                                if (light.type == LightType.Spot)
                                {
                                    light.spotAngle = Mathf.Clamp(prop.Value.ToObject<float>(), 1f, 179f);
                                    setProperties.Add($"spotAngle = {light.spotAngle}");
                                }
                                else
                                {
                                    setProperties.Add($"spotAngle: not applicable for {light.type} light");
                                }
                                break;
                            case "innerSpotAngle":
                                if (light.type == LightType.Spot)
                                {
                                    light.innerSpotAngle = Mathf.Clamp(prop.Value.ToObject<float>(), 0f, light.spotAngle);
                                    setProperties.Add($"innerSpotAngle = {light.innerSpotAngle}");
                                }
                                else
                                {
                                    setProperties.Add($"innerSpotAngle: not applicable for {light.type} light");
                                }
                                break;
                            case "shadows":
                                if (Enum.TryParse<LightShadows>(prop.Value.ToString(), true, out var shadowType))
                                {
                                    light.shadows = shadowType;
                                    setProperties.Add($"shadows = {light.shadows}");
                                }
                                else
                                {
                                    setProperties.Add($"shadows: invalid value {prop.Value}");
                                }
                                break;
                            case "shadowStrength":
                                light.shadowStrength = Mathf.Clamp01(prop.Value.ToObject<float>());
                                setProperties.Add($"shadowStrength = {light.shadowStrength}");
                                break;
                            case "shadowResolution":
                                if (Enum.TryParse<LightShadowResolution>(prop.Value.ToString(), true, out var shadowRes))
                                {
                                    light.shadowResolution = shadowRes;
                                    setProperties.Add($"shadowResolution = {light.shadowResolution}");
                                }
                                else
                                {
                                    setProperties.Add($"shadowResolution: invalid value {prop.Value}");
                                }
                                break;
                            case "cullingMask":
                                light.cullingMask = prop.Value.ToObject<int>();
                                setProperties.Add($"cullingMask = {light.cullingMask}");
                                break;
                            case "renderMode":
                                if (Enum.TryParse<LightRenderMode>(prop.Value.ToString(), true, out var renderMode))
                                {
                                    light.renderMode = renderMode;
                                    setProperties.Add($"renderMode = {light.renderMode}");
                                }
                                else
                                {
                                    setProperties.Add($"renderMode: invalid value {prop.Value}");
                                }
                                break;
                            case "enabled":
                                light.enabled = prop.Value.ToObject<bool>();
                                setProperties.Add($"enabled = {light.enabled}");
                                break;
                            default:
                                setProperties.Add($"{prop.Key}: unknown property");
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        setProperties.Add($"{prop.Key}: failed to set - {ex.Message}");
                    }
                }

#if UNITY_EDITOR
                EditorUtility.SetDirty(light);
#endif

                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Set light properties on {gameObjectName}:\n" + string.Join("\n", setProperties) }
                    }
                };
            }
            catch (Exception ex)
            {
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Failed to set light properties: {ex.Message}" }
                    },
                    IsError = true
                };
            }
        }
    }
}