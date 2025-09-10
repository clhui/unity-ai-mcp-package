using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json.Linq;
using Unity.MCP;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Unity.MCP.Tools.Editor
{
    /// <summary>
    /// Unity场景环境设置工具
    /// </summary>
    public static class UnityEnvironmentTools
    {
        /// <summary>
        /// 设置场景背景色
        /// </summary>
        public static McpToolResult SetBackgroundColor(JObject arguments)
        {
            try
            {
#if UNITY_EDITOR
                // 获取颜色参数
                Color backgroundColor = Color.black;
                if (arguments.ContainsKey("color"))
                {
                    var colorObj = arguments["color"] as JObject;
                    if (colorObj != null)
                    {
                        backgroundColor = new Color(
                            colorObj.ContainsKey("r") ? (float)colorObj["r"] : 0f,
                            colorObj.ContainsKey("g") ? (float)colorObj["g"] : 0f,
                            colorObj.ContainsKey("b") ? (float)colorObj["b"] : 0f,
                            colorObj.ContainsKey("a") ? (float)colorObj["a"] : 1f
                        );
                    }
                }
                else if (arguments.ContainsKey("colorHex"))
                {
                    string hexColor = arguments["colorHex"].ToString();
                    if (ColorUtility.TryParseHtmlString(hexColor, out Color parsedColor))
                    {
                        backgroundColor = parsedColor;
                    }
                }
                
                // 设置相机背景色
                Camera mainCamera = Camera.main;
                if (mainCamera == null)
                {
                    mainCamera = UnityEngine.Object.FindObjectOfType<Camera>();
                }
                
                if (mainCamera != null)
                {
                    Undo.RecordObject(mainCamera, "Set Background Color");
                    mainCamera.backgroundColor = backgroundColor;
                    mainCamera.clearFlags = CameraClearFlags.SolidColor;
                    EditorUtility.SetDirty(mainCamera);
                }
                
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Background color set to {backgroundColor}" }
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
                        new McpContent { Type = "text", Text = $"Error setting background color: {e.Message}" }
                    },
                    IsError = true
                };
            }
        }
        
        /// <summary>
        /// 设置天空盒
        /// </summary>
        public static McpToolResult SetSkybox(JObject arguments)
        {
            try
            {
#if UNITY_EDITOR
                string skyboxPath = arguments.ContainsKey("skyboxPath") ? arguments["skyboxPath"].ToString() : "";
                
                Material skyboxMaterial = null;
                
                if (!string.IsNullOrEmpty(skyboxPath))
                {
                    skyboxMaterial = AssetDatabase.LoadAssetAtPath<Material>(skyboxPath);
                    if (skyboxMaterial == null)
                    {
                        return new McpToolResult
                        {
                            Content = new List<McpContent>
                            {
                                new McpContent { Type = "text", Text = $"Error: Skybox material not found at path '{skyboxPath}'" }
                            },
                            IsError = true
                        };
                    }
                }
                else
                {
                    // 使用默认天空盒
                    skyboxMaterial = Resources.GetBuiltinResource<Material>("Default-Skybox.mat");
                }
                
                // 设置渲染设置中的天空盒
                RenderSettings.skybox = skyboxMaterial;
                
                // 设置相机清除标志为天空盒
                Camera mainCamera = Camera.main;
                if (mainCamera == null)
                {
                    mainCamera = UnityEngine.Object.FindObjectOfType<Camera>();
                }
                
                if (mainCamera != null)
                {
                    Undo.RecordObject(mainCamera, "Set Skybox");
                    mainCamera.clearFlags = CameraClearFlags.Skybox;
                    EditorUtility.SetDirty(mainCamera);
                }
                
                string message = string.IsNullOrEmpty(skyboxPath) ? 
                    "Default skybox applied" : 
                    $"Skybox set to material at '{skyboxPath}'";
                
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = message }
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
                        new McpContent { Type = "text", Text = $"Error setting skybox: {e.Message}" }
                    },
                    IsError = true
                };
            }
        }
        
        /// <summary>
        /// 设置雾效
        /// </summary>
        public static McpToolResult SetFog(JObject arguments)
        {
            try
            {
#if UNITY_EDITOR
                bool enableFog = arguments.ContainsKey("enabled") ? (bool)arguments["enabled"] : true;
                
                RenderSettings.fog = enableFog;
                
                if (enableFog)
                {
                    // 雾效模式
                    string fogModeStr = arguments.ContainsKey("fogMode") ? arguments["fogMode"].ToString() : "Linear";
                    FogMode fogMode = FogMode.Linear;
                    
                    switch (fogModeStr.ToLower())
                    {
                        case "linear":
                            fogMode = FogMode.Linear;
                            break;
                        case "exponential":
                            fogMode = FogMode.Exponential;
                            break;
                        case "exponentialsquared":
                            fogMode = FogMode.ExponentialSquared;
                            break;
                    }
                    
                    RenderSettings.fogMode = fogMode;
                    
                    // 雾效颜色
                    if (arguments.ContainsKey("fogColor"))
                    {
                        var colorObj = arguments["fogColor"] as JObject;
                        if (colorObj != null)
                        {
                            Color fogColor = new Color(
                                colorObj.ContainsKey("r") ? (float)colorObj["r"] : 0.5f,
                                colorObj.ContainsKey("g") ? (float)colorObj["g"] : 0.5f,
                                colorObj.ContainsKey("b") ? (float)colorObj["b"] : 0.5f,
                                colorObj.ContainsKey("a") ? (float)colorObj["a"] : 1f
                            );
                            RenderSettings.fogColor = fogColor;
                        }
                    }
                    
                    // 雾效参数
                    if (fogMode == FogMode.Linear)
                    {
                        RenderSettings.fogStartDistance = arguments.ContainsKey("fogStartDistance") ? (float)arguments["fogStartDistance"] : 0f;
                        RenderSettings.fogEndDistance = arguments.ContainsKey("fogEndDistance") ? (float)arguments["fogEndDistance"] : 300f;
                    }
                    else
                    {
                        RenderSettings.fogDensity = arguments.ContainsKey("fogDensity") ? (float)arguments["fogDensity"] : 0.01f;
                    }
                }
                
                string message = enableFog ? "Fog enabled" : "Fog disabled";
                
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = message }
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
                        new McpContent { Type = "text", Text = $"Error setting fog: {e.Message}" }
                    },
                    IsError = true
                };
            }
        }
        
        /// <summary>
        /// 设置环境光照
        /// </summary>
        public static McpToolResult SetAmbientLight(JObject arguments)
        {
            try
            {
#if UNITY_EDITOR
                // 环境光模式
                string ambientModeStr = arguments.ContainsKey("ambientMode") ? arguments["ambientMode"].ToString() : "Trilight";
                UnityEngine.Rendering.AmbientMode ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
                
                switch (ambientModeStr.ToLower())
                {
                    case "skybox":
                        ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
                        break;
                    case "trilight":
                        ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
                        break;
                    case "flat":
                        ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
                        break;
                }
                
                RenderSettings.ambientMode = ambientMode;
                
                // 根据模式设置相应参数
                if (ambientMode == UnityEngine.Rendering.AmbientMode.Trilight)
                {
                    if (arguments.ContainsKey("skyColor"))
                    {
                        var colorObj = arguments["skyColor"] as JObject;
                        if (colorObj != null)
                        {
                            RenderSettings.ambientSkyColor = new Color(
                                (float)colorObj["r"], (float)colorObj["g"], (float)colorObj["b"], 1f
                            );
                        }
                    }
                    
                    if (arguments.ContainsKey("equatorColor"))
                    {
                        var colorObj = arguments["equatorColor"] as JObject;
                        if (colorObj != null)
                        {
                            RenderSettings.ambientEquatorColor = new Color(
                                (float)colorObj["r"], (float)colorObj["g"], (float)colorObj["b"], 1f
                            );
                        }
                    }
                    
                    if (arguments.ContainsKey("groundColor"))
                    {
                        var colorObj = arguments["groundColor"] as JObject;
                        if (colorObj != null)
                        {
                            RenderSettings.ambientGroundColor = new Color(
                                (float)colorObj["r"], (float)colorObj["g"], (float)colorObj["b"], 1f
                            );
                        }
                    }
                }
                else if (ambientMode == UnityEngine.Rendering.AmbientMode.Flat)
                {
                    if (arguments.ContainsKey("ambientColor"))
                    {
                        var colorObj = arguments["ambientColor"] as JObject;
                        if (colorObj != null)
                        {
                            RenderSettings.ambientLight = new Color(
                                (float)colorObj["r"], (float)colorObj["g"], (float)colorObj["b"], 1f
                            );
                        }
                    }
                }
                
                // 环境光强度
                if (arguments.ContainsKey("ambientIntensity"))
                {
                    RenderSettings.ambientIntensity = (float)arguments["ambientIntensity"];
                }
                
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Ambient light set to {ambientModeStr} mode" }
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
                        new McpContent { Type = "text", Text = $"Error setting ambient light: {e.Message}" }
                    },
                    IsError = true
                };
            }
        }
    }
}