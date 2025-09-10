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
    /// Unity几何体创建工具
    /// </summary>
    public static class UnityGeometryTools
    {
        /// <summary>
        /// 创建基础几何体
        /// </summary>
        public static McpToolResult CreateGeometry(JObject arguments)
        {
            try
            {
#if UNITY_EDITOR
                string geometryType = arguments.ContainsKey("geometryType") ? arguments["geometryType"].ToString() : "Cube";
                string objectName = arguments.ContainsKey("objectName") ? arguments["objectName"].ToString() : geometryType;
                string parentName = arguments.ContainsKey("parentName") ? arguments["parentName"].ToString() : "";
                
                // 位置参数
                Vector3 position = Vector3.zero;
                if (arguments.ContainsKey("position"))
                {
                    var posObj = arguments["position"] as JObject;
                    if (posObj != null)
                    {
                        position = new Vector3(
                            posObj.ContainsKey("x") ? (float)posObj["x"] : 0f,
                            posObj.ContainsKey("y") ? (float)posObj["y"] : 0f,
                            posObj.ContainsKey("z") ? (float)posObj["z"] : 0f
                        );
                    }
                }
                
                // 旋转参数
                Vector3 rotation = Vector3.zero;
                if (arguments.ContainsKey("rotation"))
                {
                    var rotObj = arguments["rotation"] as JObject;
                    if (rotObj != null)
                    {
                        rotation = new Vector3(
                            rotObj.ContainsKey("x") ? (float)rotObj["x"] : 0f,
                            rotObj.ContainsKey("y") ? (float)rotObj["y"] : 0f,
                            rotObj.ContainsKey("z") ? (float)rotObj["z"] : 0f
                        );
                    }
                }
                
                // 缩放参数
                Vector3 scale = Vector3.one;
                if (arguments.ContainsKey("scale"))
                {
                    var scaleObj = arguments["scale"] as JObject;
                    if (scaleObj != null)
                    {
                        scale = new Vector3(
                            scaleObj.ContainsKey("x") ? (float)scaleObj["x"] : 1f,
                            scaleObj.ContainsKey("y") ? (float)scaleObj["y"] : 1f,
                            scaleObj.ContainsKey("z") ? (float)scaleObj["z"] : 1f
                        );
                    }
                }

                GameObject gameObject = null;
                
                // 根据几何体类型创建对象
                switch (geometryType.ToLower())
                {
                    case "cube":
                        gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        break;
                    case "sphere":
                        gameObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        break;
                    case "cylinder":
                        gameObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                        break;
                    case "capsule":
                        gameObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                        break;
                    case "plane":
                        gameObject = GameObject.CreatePrimitive(PrimitiveType.Plane);
                        break;
                    case "quad":
                        gameObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
                        break;
                    default:
                        return new McpToolResult
                        {
                            Content = new List<McpContent>
                            {
                                new McpContent { Type = "text", Text = $"Error: Unsupported geometry type '{geometryType}'. Supported types: Cube, Sphere, Cylinder, Capsule, Plane, Quad" }
                            },
                            IsError = true
                        };
                }

                if (gameObject == null)
                {
                    return new McpToolResult
                    {
                        Content = new List<McpContent>
                        {
                            new McpContent { Type = "text", Text = $"Error: Failed to create {geometryType}" }
                        },
                        IsError = true
                    };
                }

                // 设置对象名称
                gameObject.name = objectName;
                
                // 设置变换
                gameObject.transform.position = position;
                gameObject.transform.rotation = Quaternion.Euler(rotation);
                gameObject.transform.localScale = scale;
                
                // 设置父对象
                if (!string.IsNullOrEmpty(parentName))
                {
                    GameObject parent = GameObject.Find(parentName);
                    if (parent != null)
                    {
                        gameObject.transform.SetParent(parent.transform);
                    }
                    else
                    {
                        return new McpToolResult
                        {
                            Content = new List<McpContent>
                            {
                                new McpContent { Type = "text", Text = $"Warning: Parent object '{parentName}' not found. {geometryType} '{objectName}' created without parent." }
                            }
                        };
                    }
                }
                
                // 注册撤销操作
                Undo.RegisterCreatedObjectUndo(gameObject, $"Create {geometryType}");
                
                // 选中新创建的对象
                Selection.activeGameObject = gameObject;
                
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"{geometryType} '{objectName}' created successfully at position {position}" }
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
                        new McpContent { Type = "text", Text = $"Error creating geometry: {e.Message}" }
                    },
                    IsError = true
                };
            }
        }
        
        /// <summary>
        /// 创建自定义网格几何体
        /// </summary>
        public static McpToolResult CreateCustomMesh(JObject arguments)
        {
            try
            {
#if UNITY_EDITOR
                string objectName = arguments.ContainsKey("objectName") ? arguments["objectName"].ToString() : "CustomMesh";
                string meshType = arguments.ContainsKey("meshType") ? arguments["meshType"].ToString() : "Triangle";
                
                GameObject gameObject = new GameObject(objectName);
                MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
                MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
                
                Mesh mesh = new Mesh();
                
                switch (meshType.ToLower())
                {
                    case "triangle":
                        CreateTriangleMesh(mesh);
                        break;
                    case "square":
                        CreateSquareMesh(mesh);
                        break;
                    default:
                        CreateTriangleMesh(mesh);
                        break;
                }
                
                meshFilter.mesh = mesh;
                
                // 设置默认材质
                Material defaultMaterial = Resources.GetBuiltinResource<Material>("Default-Material.mat");
                if (defaultMaterial != null)
                {
                    meshRenderer.material = defaultMaterial;
                }
                
                // 注册撤销操作
                Undo.RegisterCreatedObjectUndo(gameObject, $"Create Custom Mesh");
                
                // 选中新创建的对象
                Selection.activeGameObject = gameObject;
                
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Custom mesh '{objectName}' of type '{meshType}' created successfully" }
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
                        new McpContent { Type = "text", Text = $"Error creating custom mesh: {e.Message}" }
                    },
                    IsError = true
                };
            }
        }
        
#if UNITY_EDITOR
        /// <summary>
        /// 创建三角形网格
        /// </summary>
        private static void CreateTriangleMesh(Mesh mesh)
        {
            Vector3[] vertices = new Vector3[3]
            {
                new Vector3(0, 1, 0),
                new Vector3(-1, -1, 0),
                new Vector3(1, -1, 0)
            };
            
            int[] triangles = new int[3] { 0, 1, 2 };
            
            Vector2[] uv = new Vector2[3]
            {
                new Vector2(0.5f, 1),
                new Vector2(0, 0),
                new Vector2(1, 0)
            };
            
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uv;
            mesh.RecalculateNormals();
            mesh.name = "Triangle";
        }
        
        /// <summary>
        /// 创建正方形网格
        /// </summary>
        private static void CreateSquareMesh(Mesh mesh)
        {
            Vector3[] vertices = new Vector3[4]
            {
                new Vector3(-1, -1, 0),
                new Vector3(1, -1, 0),
                new Vector3(1, 1, 0),
                new Vector3(-1, 1, 0)
            };
            
            int[] triangles = new int[6] { 0, 2, 1, 0, 3, 2 };
            
            Vector2[] uv = new Vector2[4]
            {
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(1, 1),
                new Vector2(0, 1)
            };
            
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uv;
            mesh.RecalculateNormals();
            mesh.name = "Square";
        }
#endif
    }
}