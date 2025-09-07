using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json.Linq;
#if UNITY_EDITOR
using UnityEditor;
using Unity.MCP;
#endif

namespace Unity.MCP.Tools.Editor
{
    public static class UnityPhysicsTools
    {
        public static McpToolResult SetRigidbodyProperties(JObject arguments)
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
                
                var rigidbody = go.GetComponent<Rigidbody>();
                if (rigidbody == null)
                {
                    return new McpToolResult
                    {
                        Content = new List<McpContent>
                        {
                            new McpContent { Type = "text", Text = $"No Rigidbody component found on GameObject: {gameObjectName}" }
                        },
                        IsError = true
                    };
                }
                
#if UNITY_EDITOR
                Undo.RecordObject(rigidbody, "Set Rigidbody Properties");
#endif
                
                var setProperties = new List<string>();
                foreach (var prop in properties)
                {
                    try
                    {
                        switch (prop.Key.ToLower())
                        {
                            case "mass":
                                rigidbody.mass = prop.Value.ToObject<float>();
                                setProperties.Add($"mass = {rigidbody.mass}");
                                break;
                                
                            case "drag":
                                rigidbody.drag = prop.Value.ToObject<float>();
                                setProperties.Add($"drag = {rigidbody.drag}");
                                break;
                                
                            case "angulardrag":
                                rigidbody.angularDrag = prop.Value.ToObject<float>();
                                setProperties.Add($"angularDrag = {rigidbody.angularDrag}");
                                break;
                                
                            case "usegravity":
                                rigidbody.useGravity = prop.Value.ToObject<bool>();
                                setProperties.Add($"useGravity = {rigidbody.useGravity}");
                                break;
                                
                            case "iskinematic":
                                rigidbody.isKinematic = prop.Value.ToObject<bool>();
                                setProperties.Add($"isKinematic = {rigidbody.isKinematic}");
                                break;
                                
                            case "interpolate":
                                var interpolation = (RigidbodyInterpolation)prop.Value.ToObject<int>();
                                rigidbody.interpolation = interpolation;
                                setProperties.Add($"interpolation = {interpolation}");
                                break;
                                
                            case "collisiondetection":
                                var collisionDetection = (CollisionDetectionMode)prop.Value.ToObject<int>();
                                rigidbody.collisionDetectionMode = collisionDetection;
                                setProperties.Add($"collisionDetectionMode = {collisionDetection}");
                                break;
                                
                            case "constraints":
                                var constraints = (RigidbodyConstraints)prop.Value.ToObject<int>();
                                rigidbody.constraints = constraints;
                                setProperties.Add($"constraints = {constraints}");
                                break;
                                
                            case "velocity":
                                var velocity = prop.Value.ToObject<Vector3>();
                                rigidbody.velocity = velocity;
                                setProperties.Add($"velocity = {velocity}");
                                break;
                                
                            case "angularvelocity":
                                var angularVelocity = prop.Value.ToObject<Vector3>();
                                rigidbody.angularVelocity = angularVelocity;
                                setProperties.Add($"angularVelocity = {angularVelocity}");
                                break;
                                
                            default:
                                setProperties.Add($"{prop.Key}: unknown rigidbody property");
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        setProperties.Add($"{prop.Key}: failed to set - {ex.Message}");
                    }
                }
                
#if UNITY_EDITOR
                EditorUtility.SetDirty(rigidbody);
#endif
                
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Set rigidbody properties on {gameObjectName}:\n" + string.Join("\n", setProperties) }
                    }
                };
            }
            catch (Exception ex)
            {
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Failed to set rigidbody properties: {ex.Message}" }
                    },
                    IsError = true
                };
            }
        }
        
        public static McpToolResult AddForce(JObject arguments)
        {
            var gameObjectName = arguments["gameObject"]?.ToString();
            var force = arguments["force"]?.ToObject<Vector3>() ?? Vector3.zero;
            var forceMode = arguments["forceMode"]?.ToObject<int>() ?? 0;
            
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
                
                var rigidbody = go.GetComponent<Rigidbody>();
                if (rigidbody == null)
                {
                    return new McpToolResult
                    {
                        Content = new List<McpContent>
                        {
                            new McpContent { Type = "text", Text = $"No Rigidbody component found on GameObject: {gameObjectName}" }
                        },
                        IsError = true
                    };
                }
                
                var mode = (ForceMode)forceMode;
                rigidbody.AddForce(force, mode);
                
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Applied force {force} to {gameObjectName} with mode {mode}" }
                    }
                };
            }
            catch (Exception ex)
            {
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Failed to add force: {ex.Message}" }
                    },
                    IsError = true
                };
            }
        }
        
        public static McpToolResult SetColliderProperties(JObject arguments)
        {
            var gameObjectName = arguments["gameObject"]?.ToString();
            var colliderType = arguments["colliderType"]?.ToString();
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
                
                Collider collider = null;
                if (!string.IsNullOrEmpty(colliderType))
                {
                    switch (colliderType.ToLower())
                    {
                        case "box":
                        case "boxcollider":
                            collider = go.GetComponent<BoxCollider>();
                            break;
                        case "sphere":
                        case "spherecollider":
                            collider = go.GetComponent<SphereCollider>();
                            break;
                        case "capsule":
                        case "capsulecollider":
                            collider = go.GetComponent<CapsuleCollider>();
                            break;
                        case "mesh":
                        case "meshcollider":
                            collider = go.GetComponent<MeshCollider>();
                            break;
                        default:
                            collider = go.GetComponent<Collider>();
                            break;
                    }
                }
                else
                {
                    collider = go.GetComponent<Collider>();
                }
                
                if (collider == null)
                {
                    return new McpToolResult
                    {
                        Content = new List<McpContent>
                        {
                            new McpContent { Type = "text", Text = $"No Collider component found on GameObject: {gameObjectName}" }
                        },
                        IsError = true
                    };
                }
                
#if UNITY_EDITOR
                Undo.RecordObject(collider, "Set Collider Properties");
#endif
                
                var setProperties = new List<string>();
                foreach (var prop in properties)
                {
                    try
                    {
                        switch (prop.Key.ToLower())
                        {
                            case "enabled":
                                collider.enabled = prop.Value.ToObject<bool>();
                                setProperties.Add($"enabled = {collider.enabled}");
                                break;
                                
                            case "istrigger":
                                collider.isTrigger = prop.Value.ToObject<bool>();
                                setProperties.Add($"isTrigger = {collider.isTrigger}");
                                break;
                                
                            case "material":
                                var materialPath = prop.Value.ToString();
#if UNITY_EDITOR
                                var physicMaterial = AssetDatabase.LoadAssetAtPath<PhysicMaterial>(materialPath);
#else
                                var materialName = Path.GetFileNameWithoutExtension(materialPath);
                                var physicMaterial = Resources.Load<PhysicMaterial>(materialName);
#endif
                                if (physicMaterial != null)
                                {
                                    collider.material = physicMaterial;
                                    setProperties.Add($"material = {physicMaterial.name}");
                                }
                                else
                                {
                                    setProperties.Add($"material: not found at {materialPath}");
                                }
                                break;
                                
                            // BoxCollider specific properties
                            case "size":
                                if (collider is BoxCollider boxCollider)
                                {
                                    boxCollider.size = prop.Value.ToObject<Vector3>();
                                    setProperties.Add($"size = {boxCollider.size}");
                                }
                                else
                                {
                                    setProperties.Add($"size: only applicable to BoxCollider");
                                }
                                break;
                                
                            case "center":
                                if (collider is BoxCollider boxCol)
                                {
                                    boxCol.center = prop.Value.ToObject<Vector3>();
                                    setProperties.Add($"center = {boxCol.center}");
                                }
                                else if (collider is SphereCollider sphereCol)
                                {
                                    sphereCol.center = prop.Value.ToObject<Vector3>();
                                    setProperties.Add($"center = {sphereCol.center}");
                                }
                                else if (collider is CapsuleCollider capsuleCol)
                                {
                                    capsuleCol.center = prop.Value.ToObject<Vector3>();
                                    setProperties.Add($"center = {capsuleCol.center}");
                                }
                                else
                                {
                                    setProperties.Add($"center: not applicable to {collider.GetType().Name}");
                                }
                                break;
                                
                            // SphereCollider specific properties
                            case "radius":
                                if (collider is SphereCollider sphereCollider)
                                {
                                    sphereCollider.radius = prop.Value.ToObject<float>();
                                    setProperties.Add($"radius = {sphereCollider.radius}");
                                }
                                else if (collider is CapsuleCollider capsuleCollider)
                                {
                                    capsuleCollider.radius = prop.Value.ToObject<float>();
                                    setProperties.Add($"radius = {capsuleCollider.radius}");
                                }
                                else
                                {
                                    setProperties.Add($"radius: only applicable to SphereCollider or CapsuleCollider");
                                }
                                break;
                                
                            // CapsuleCollider specific properties
                            case "height":
                                if (collider is CapsuleCollider capsuleCol2)
                                {
                                    capsuleCol2.height = prop.Value.ToObject<float>();
                                    setProperties.Add($"height = {capsuleCol2.height}");
                                }
                                else
                                {
                                    setProperties.Add($"height: only applicable to CapsuleCollider");
                                }
                                break;
                                
                            case "direction":
                                if (collider is CapsuleCollider capsuleCol3)
                                {
                                    capsuleCol3.direction = prop.Value.ToObject<int>();
                                    setProperties.Add($"direction = {capsuleCol3.direction}");
                                }
                                else
                                {
                                    setProperties.Add($"direction: only applicable to CapsuleCollider");
                                }
                                break;
                                
                            // MeshCollider specific properties
                            case "convex":
                                if (collider is MeshCollider meshCollider)
                                {
                                    meshCollider.convex = prop.Value.ToObject<bool>();
                                    setProperties.Add($"convex = {meshCollider.convex}");
                                }
                                else
                                {
                                    setProperties.Add($"convex: only applicable to MeshCollider");
                                }
                                break;
                                
                            default:
                                setProperties.Add($"{prop.Key}: unknown collider property");
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        setProperties.Add($"{prop.Key}: failed to set - {ex.Message}");
                    }
                }
                
#if UNITY_EDITOR
                EditorUtility.SetDirty(collider);
#endif
                
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Set collider properties on {gameObjectName} ({collider.GetType().Name}):\n" + string.Join("\n", setProperties) }
                    }
                };
            }
            catch (Exception ex)
            {
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Failed to set collider properties: {ex.Message}" }
                    },
                    IsError = true
                };
            }
        }
        
        public static McpToolResult Raycast(JObject arguments)
        {
            var origin = arguments["origin"]?.ToObject<Vector3>() ?? Vector3.zero;
            var direction = arguments["direction"]?.ToObject<Vector3>() ?? Vector3.forward;
            var maxDistance = arguments["maxDistance"]?.ToObject<float>() ?? Mathf.Infinity;
            var layerMask = arguments["layerMask"]?.ToObject<int>() ?? -1;
            
            try
            {
                RaycastHit hit;
                bool hasHit = Physics.Raycast(origin, direction, out hit, maxDistance, layerMask);
                
                if (hasHit)
                {
                    var hitInfo = new
                    {
                        hit = true,
                        point = hit.point,
                        normal = hit.normal,
                        distance = hit.distance,
                        gameObject = hit.collider.gameObject.name,
                        collider = hit.collider.GetType().Name,
                        transform = new
                        {
                            position = hit.transform.position,
                            rotation = hit.transform.rotation.eulerAngles,
                            scale = hit.transform.localScale
                        }
                    };
                    
                    return new McpToolResult
                    {
                        Content = new List<McpContent>
                        {
                            new McpContent { Type = "text", Text = $"Raycast hit:\n" +
                                $"GameObject: {hitInfo.gameObject}\n" +
                                $"Hit Point: {hitInfo.point}\n" +
                                $"Normal: {hitInfo.normal}\n" +
                                $"Distance: {hitInfo.distance}\n" +
                                $"Collider: {hitInfo.collider}\n" +
                                $"Transform Position: {hitInfo.transform.position}" }
                        }
                    };
                }
                else
                {
                    return new McpToolResult
                    {
                        Content = new List<McpContent>
                        {
                            new McpContent { Type = "text", Text = $"Raycast from {origin} in direction {direction} (distance: {maxDistance}, layerMask: {layerMask}) - No hit" }
                        }
                    };
                }
            }
            catch (Exception ex)
            {
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Failed to perform raycast: {ex.Message}" }
                    },
                    IsError = true
                };
            }
        }
    }
}