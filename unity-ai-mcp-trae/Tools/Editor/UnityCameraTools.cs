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
    /// Unity相机属性设置工具
    /// </summary>
    public static class UnityCameraTools
    {
        /// <summary>
        /// 设置相机属性
        /// </summary>
        public static McpToolResult SetCameraProperties(JObject arguments)
        {
            try
            {
#if UNITY_EDITOR
                string cameraName = arguments.ContainsKey("cameraName") ? arguments["cameraName"].ToString() : "Main Camera";
                
                Camera targetCamera = null;
                
                // 查找指定相机
                if (cameraName == "Main Camera")
                {
                    targetCamera = Camera.main;
                }
                
                if (targetCamera == null)
                {
                    GameObject cameraObj = GameObject.Find(cameraName);
                    if (cameraObj != null)
                    {
                        targetCamera = cameraObj.GetComponent<Camera>();
                    }
                }
                
                if (targetCamera == null)
                {
                    return new McpToolResult
                    {
                        Content = new List<McpContent>
                        {
                            new McpContent { Type = "text", Text = $"Error: Camera '{cameraName}' not found" }
                        },
                        IsError = true
                    };
                }
                
                Undo.RecordObject(targetCamera, "Set Camera Properties");
                
                // 设置视野角度 (Field of View)
                if (arguments.ContainsKey("fieldOfView"))
                {
                    targetCamera.fieldOfView = (float)arguments["fieldOfView"];
                }
                
                // 设置近裁剪面
                if (arguments.ContainsKey("nearClipPlane"))
                {
                    targetCamera.nearClipPlane = (float)arguments["nearClipPlane"];
                }
                
                // 设置远裁剪面
                if (arguments.ContainsKey("farClipPlane"))
                {
                    targetCamera.farClipPlane = (float)arguments["farClipPlane"];
                }
                
                // 设置投影模式
                if (arguments.ContainsKey("projectionMode"))
                {
                    string projectionStr = arguments["projectionMode"].ToString().ToLower();
                    if (projectionStr == "perspective")
                    {
                        targetCamera.orthographic = false;
                    }
                    else if (projectionStr == "orthographic")
                    {
                        targetCamera.orthographic = true;
                    }
                }
                
                // 设置正交大小（仅在正交模式下有效）
                if (arguments.ContainsKey("orthographicSize"))
                {
                    targetCamera.orthographicSize = (float)arguments["orthographicSize"];
                }
                
                // 设置相机位置
                if (arguments.ContainsKey("position"))
                {
                    var posObj = arguments["position"] as JObject;
                    if (posObj != null)
                    {
                        Vector3 position = new Vector3(
                            posObj.ContainsKey("x") ? (float)posObj["x"] : targetCamera.transform.position.x,
                            posObj.ContainsKey("y") ? (float)posObj["y"] : targetCamera.transform.position.y,
                            posObj.ContainsKey("z") ? (float)posObj["z"] : targetCamera.transform.position.z
                        );
                        Undo.RecordObject(targetCamera.transform, "Set Camera Position");
                        targetCamera.transform.position = position;
                    }
                }
                
                // 设置相机旋转
                if (arguments.ContainsKey("rotation"))
                {
                    var rotObj = arguments["rotation"] as JObject;
                    if (rotObj != null)
                    {
                        Vector3 rotation = new Vector3(
                            rotObj.ContainsKey("x") ? (float)rotObj["x"] : targetCamera.transform.eulerAngles.x,
                            rotObj.ContainsKey("y") ? (float)rotObj["y"] : targetCamera.transform.eulerAngles.y,
                            rotObj.ContainsKey("z") ? (float)rotObj["z"] : targetCamera.transform.eulerAngles.z
                        );
                        Undo.RecordObject(targetCamera.transform, "Set Camera Rotation");
                        targetCamera.transform.rotation = Quaternion.Euler(rotation);
                    }
                }
                
                // 设置清除标志
                if (arguments.ContainsKey("clearFlags"))
                {
                    string clearFlagsStr = arguments["clearFlags"].ToString().ToLower();
                    switch (clearFlagsStr)
                    {
                        case "skybox":
                            targetCamera.clearFlags = CameraClearFlags.Skybox;
                            break;
                        case "solidcolor":
                            targetCamera.clearFlags = CameraClearFlags.SolidColor;
                            break;
                        case "depth":
                            targetCamera.clearFlags = CameraClearFlags.Depth;
                            break;
                        case "nothing":
                            targetCamera.clearFlags = CameraClearFlags.Nothing;
                            break;
                    }
                }
                
                // 设置背景色
                if (arguments.ContainsKey("backgroundColor"))
                {
                    var colorObj = arguments["backgroundColor"] as JObject;
                    if (colorObj != null)
                    {
                        Color backgroundColor = new Color(
                            colorObj.ContainsKey("r") ? (float)colorObj["r"] : 0f,
                            colorObj.ContainsKey("g") ? (float)colorObj["g"] : 0f,
                            colorObj.ContainsKey("b") ? (float)colorObj["b"] : 0f,
                            colorObj.ContainsKey("a") ? (float)colorObj["a"] : 1f
                        );
                        targetCamera.backgroundColor = backgroundColor;
                    }
                }
                
                // 设置渲染深度
                if (arguments.ContainsKey("depth"))
                {
                    targetCamera.depth = (float)arguments["depth"];
                }
                
                // 设置渲染路径
                if (arguments.ContainsKey("renderingPath"))
                {
                    string renderingPathStr = arguments["renderingPath"].ToString().ToLower();
                    switch (renderingPathStr)
                    {
                        case "forward":
                            targetCamera.renderingPath = RenderingPath.Forward;
                            break;
                        case "deferred":
                            targetCamera.renderingPath = RenderingPath.DeferredShading;
                            break;
                        case "legacy":
                            targetCamera.renderingPath = RenderingPath.VertexLit;
                            break;
                        case "use player settings":
                            targetCamera.renderingPath = RenderingPath.UsePlayerSettings;
                            break;
                    }
                }
                
                // 设置视口矩形
                if (arguments.ContainsKey("viewportRect"))
                {
                    var rectObj = arguments["viewportRect"] as JObject;
                    if (rectObj != null)
                    {
                        Rect viewportRect = new Rect(
                            rectObj.ContainsKey("x") ? (float)rectObj["x"] : 0f,
                            rectObj.ContainsKey("y") ? (float)rectObj["y"] : 0f,
                            rectObj.ContainsKey("width") ? (float)rectObj["width"] : 1f,
                            rectObj.ContainsKey("height") ? (float)rectObj["height"] : 1f
                        );
                        targetCamera.rect = viewportRect;
                    }
                }
                
                EditorUtility.SetDirty(targetCamera);
                
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Camera '{cameraName}' properties updated successfully" }
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
                        new McpContent { Type = "text", Text = $"Error setting camera properties: {e.Message}" }
                    },
                    IsError = true
                };
            }
        }
        
        /// <summary>
        /// 获取相机属性
        /// </summary>
        public static McpToolResult GetCameraProperties(JObject arguments)
        {
            try
            {
#if UNITY_EDITOR
                string cameraName = arguments.ContainsKey("cameraName") ? arguments["cameraName"].ToString() : "Main Camera";
                
                Camera targetCamera = null;
                
                // 查找指定相机
                if (cameraName == "Main Camera")
                {
                    targetCamera = Camera.main;
                }
                
                if (targetCamera == null)
                {
                    GameObject cameraObj = GameObject.Find(cameraName);
                    if (cameraObj != null)
                    {
                        targetCamera = cameraObj.GetComponent<Camera>();
                    }
                }
                
                if (targetCamera == null)
                {
                    return new McpToolResult
                    {
                        Content = new List<McpContent>
                        {
                            new McpContent { Type = "text", Text = $"Error: Camera '{cameraName}' not found" }
                        },
                        IsError = true
                    };
                }
                
                var cameraInfo = new JObject
                {
                    ["name"] = targetCamera.name,
                    ["fieldOfView"] = targetCamera.fieldOfView,
                    ["nearClipPlane"] = targetCamera.nearClipPlane,
                    ["farClipPlane"] = targetCamera.farClipPlane,
                    ["orthographic"] = targetCamera.orthographic,
                    ["orthographicSize"] = targetCamera.orthographicSize,
                    ["position"] = new JObject
                    {
                        ["x"] = targetCamera.transform.position.x,
                        ["y"] = targetCamera.transform.position.y,
                        ["z"] = targetCamera.transform.position.z
                    },
                    ["rotation"] = new JObject
                    {
                        ["x"] = targetCamera.transform.eulerAngles.x,
                        ["y"] = targetCamera.transform.eulerAngles.y,
                        ["z"] = targetCamera.transform.eulerAngles.z
                    },
                    ["clearFlags"] = targetCamera.clearFlags.ToString(),
                    ["backgroundColor"] = new JObject
                    {
                        ["r"] = targetCamera.backgroundColor.r,
                        ["g"] = targetCamera.backgroundColor.g,
                        ["b"] = targetCamera.backgroundColor.b,
                        ["a"] = targetCamera.backgroundColor.a
                    },
                    ["depth"] = targetCamera.depth,
                    ["renderingPath"] = targetCamera.renderingPath.ToString(),
                    ["viewportRect"] = new JObject
                    {
                        ["x"] = targetCamera.rect.x,
                        ["y"] = targetCamera.rect.y,
                        ["width"] = targetCamera.rect.width,
                        ["height"] = targetCamera.rect.height
                    }
                };
                
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Camera properties:\n{cameraInfo.ToString()}" }
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
                        new McpContent { Type = "text", Text = $"Error getting camera properties: {e.Message}" }
                    },
                    IsError = true
                };
            }
        }
        
        /// <summary>
        /// 创建新相机
        /// </summary>
        public static McpToolResult CreateCamera(JObject arguments)
        {
            try
            {
#if UNITY_EDITOR
                string cameraName = arguments.ContainsKey("cameraName") ? arguments["cameraName"].ToString() : "New Camera";
                
                // 创建相机游戏对象
                GameObject cameraObj = new GameObject(cameraName);
                Camera camera = cameraObj.AddComponent<Camera>();
                
                // 设置初始属性
                if (arguments.ContainsKey("position"))
                {
                    var posObj = arguments["position"] as JObject;
                    if (posObj != null)
                    {
                        Vector3 position = new Vector3(
                            posObj.ContainsKey("x") ? (float)posObj["x"] : 0f,
                            posObj.ContainsKey("y") ? (float)posObj["y"] : 1f,
                            posObj.ContainsKey("z") ? (float)posObj["z"] : -10f
                        );
                        cameraObj.transform.position = position;
                    }
                }
                else
                {
                    cameraObj.transform.position = new Vector3(0, 1, -10);
                }
                
                // 注册撤销操作
                Undo.RegisterCreatedObjectUndo(cameraObj, "Create Camera");
                
                // 选中新创建的相机
                Selection.activeGameObject = cameraObj;
                
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Camera '{cameraName}' created successfully" }
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
                        new McpContent { Type = "text", Text = $"Error creating camera: {e.Message}" }
                    },
                    IsError = true
                };
            }
        }
    }
}