using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json.Linq;
using Unity.MCP;
using Unity.MCP.Editor;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Unity.MCP.Editor
{
    public static class UnityTerrainTools
    {
        #region Terrain System
        
        /// <summary>
        /// 创建地形
        /// </summary>
        /// <param name="parameters">地形参数JSON</param>
        /// <returns>操作结果</returns>
        public static string CreateTerrain(string parameters)
        {
            try
            {
#if UNITY_EDITOR
                var paramObj = JObject.Parse(parameters);
                
                // 解析参数
                string name = paramObj["name"]?.ToString() ?? "New Terrain";
                int width = paramObj["width"]?.ToObject<int>() ?? 1000;
                int height = paramObj["height"]?.ToObject<int>() ?? 1000;
                int heightmapResolution = paramObj["heightmapResolution"]?.ToObject<int>() ?? 513;
                float terrainHeight = paramObj["terrainHeight"]?.ToObject<float>() ?? 600f;
                Vector3 position = Vector3.zero;
                
                if (paramObj["position"] != null)
                {
                    var posObj = paramObj["position"];
                    position = new Vector3(
                        posObj["x"]?.ToObject<float>() ?? 0f,
                        posObj["y"]?.ToObject<float>() ?? 0f,
                        posObj["z"]?.ToObject<float>() ?? 0f
                    );
                }
                
                // 创建地形数据
                TerrainData terrainData = new TerrainData();
                terrainData.heightmapResolution = heightmapResolution;
                terrainData.size = new Vector3(width, terrainHeight, height);
                
                // 创建地形GameObject
                GameObject terrainObject = Terrain.CreateTerrainGameObject(terrainData);
                terrainObject.name = name;
                terrainObject.transform.position = position;
                
                // 保存地形数据资产
                string assetPath = $"Assets/TerrainData_{name}.asset";
                AssetDatabase.CreateAsset(terrainData, assetPath);
                AssetDatabase.SaveAssets();
                
                // 记录撤销操作
                Undo.RegisterCreatedObjectUndo(terrainObject, "Create Terrain");
                
                // 选中新创建的地形
                Selection.activeGameObject = terrainObject;
                
                McpLogger.LogTool($"地形创建成功: {name}");
                return $"{{\"success\": true, \"message\": \"地形 '{name}' 创建成功\", \"terrainId\": \"{terrainObject.GetInstanceID()}\", \"assetPath\": \"{assetPath}\"}}";
#else
                return "{\"success\": false, \"message\": \"此功能仅在编辑器模式下可用\"}";
#endif
            }
            catch (Exception ex)
            {
                McpLogger.LogException(ex, "创建地形时发生错误");
                return $"{{\"success\": false, \"message\": \"创建地形失败: {ex.Message}\"}}";
            }
        }
        
        /// <summary>
        /// 修改地形高度
        /// </summary>
        /// <param name="parameters">修改参数JSON</param>
        /// <returns>操作结果</returns>
        public static string ModifyTerrain(string parameters)
        {
            try
            {
#if UNITY_EDITOR
                var paramObj = JObject.Parse(parameters);
                
                // 解析参数
                string terrainName = paramObj["terrainName"]?.ToString();
                int terrainId = paramObj["terrainId"]?.ToObject<int>() ?? 0;
                string operation = paramObj["operation"]?.ToString() ?? "raise";
                float strength = paramObj["strength"]?.ToObject<float>() ?? 0.1f;
                float brushSize = paramObj["brushSize"]?.ToObject<float>() ?? 10f;
                Vector2 position = Vector2.zero;
                
                if (paramObj["position"] != null)
                {
                    var posObj = paramObj["position"];
                    position = new Vector2(
                        posObj["x"]?.ToObject<float>() ?? 0f,
                        posObj["z"]?.ToObject<float>() ?? 0f
                    );
                }
                
                // 查找地形
                Terrain terrain = null;
                if (terrainId != 0)
                {
                    var obj = EditorUtility.InstanceIDToObject(terrainId) as GameObject;
                    terrain = obj?.GetComponent<Terrain>();
                }
                else if (!string.IsNullOrEmpty(terrainName))
                {
                    var terrainObj = GameObject.Find(terrainName);
                    terrain = terrainObj?.GetComponent<Terrain>();
                }
                
                if (terrain == null)
                {
                    return "{\"success\": false, \"message\": \"未找到指定的地形\"}";
                }
                
                // 记录撤销操作
                Undo.RecordObject(terrain.terrainData, "Modify Terrain");
                
                // 获取地形数据
                TerrainData terrainData = terrain.terrainData;
                int heightmapWidth = terrainData.heightmapResolution;
                int heightmapHeight = terrainData.heightmapResolution;
                
                // 转换世界坐标到地形坐标
                Vector3 terrainPos = terrain.transform.position;
                Vector3 terrainSize = terrainData.size;
                
                float normalizedX = (position.x - terrainPos.x) / terrainSize.x;
                float normalizedZ = (position.y - terrainPos.z) / terrainSize.z;
                
                int centerX = Mathf.RoundToInt(normalizedX * (heightmapWidth - 1));
                int centerZ = Mathf.RoundToInt(normalizedZ * (heightmapHeight - 1));
                
                // 计算笔刷范围
                int brushRadius = Mathf.RoundToInt(brushSize / 2f);
                int startX = Mathf.Max(0, centerX - brushRadius);
                int endX = Mathf.Min(heightmapWidth - 1, centerX + brushRadius);
                int startZ = Mathf.Max(0, centerZ - brushRadius);
                int endZ = Mathf.Min(heightmapHeight - 1, centerZ + brushRadius);
                
                // 获取当前高度图
                float[,] heights = terrainData.GetHeights(startX, startZ, endX - startX + 1, endZ - startZ + 1);
                
                // 修改高度
                for (int x = 0; x < heights.GetLength(0); x++)
                {
                    for (int z = 0; z < heights.GetLength(1); z++)
                    {
                        int worldX = startX + x;
                        int worldZ = startZ + z;
                        
                        // 计算距离中心的距离
                        float distance = Vector2.Distance(new Vector2(worldX, worldZ), new Vector2(centerX, centerZ));
                        
                        if (distance <= brushRadius)
                        {
                            // 计算衰减
                            float falloff = 1f - (distance / brushRadius);
                            float heightChange = strength * falloff * 0.01f; // 转换为地形高度单位
                            
                            switch (operation.ToLower())
                            {
                                case "raise":
                                    heights[x, z] += heightChange;
                                    break;
                                case "lower":
                                    heights[x, z] -= heightChange;
                                    break;
                                case "flatten":
                                    float targetHeight = paramObj["targetHeight"]?.ToObject<float>() ?? 0.5f;
                                    heights[x, z] = Mathf.Lerp(heights[x, z], targetHeight, falloff * strength);
                                    break;
                            }
                            
                            // 限制高度范围
                            heights[x, z] = Mathf.Clamp01(heights[x, z]);
                        }
                    }
                }
                
                // 应用修改的高度图
                terrainData.SetHeights(startX, startZ, heights);
                
                McpLogger.LogTool($"地形修改成功: {terrain.name}");
                return $"{{\"success\": true, \"message\": \"地形 '{terrain.name}' 修改成功\"}}";
#else
                return "{\"success\": false, \"message\": \"此功能仅在编辑器模式下可用\"}";
#endif
            }
            catch (Exception ex)
            {
                McpLogger.LogException(ex, "修改地形时发生错误");
                return $"{{\"success\": false, \"message\": \"修改地形失败: {ex.Message}\"}}";
            }
        }
        
        /// <summary>
        /// 绘制地形纹理
        /// </summary>
        /// <param name="parameters">绘制参数JSON</param>
        /// <returns>操作结果</returns>
        public static string PaintTerrainTexture(string parameters)
        {
            try
            {
#if UNITY_EDITOR
                var paramObj = JObject.Parse(parameters);
                
                // 解析参数
                string terrainName = paramObj["terrainName"]?.ToString();
                int terrainId = paramObj["terrainId"]?.ToObject<int>() ?? 0;
                string texturePath = paramObj["texturePath"]?.ToString();
                int textureIndex = paramObj["textureIndex"]?.ToObject<int>() ?? 0;
                float strength = paramObj["strength"]?.ToObject<float>() ?? 1f;
                float brushSize = paramObj["brushSize"]?.ToObject<float>() ?? 10f;
                Vector2 position = Vector2.zero;
                
                if (paramObj["position"] != null)
                {
                    var posObj = paramObj["position"];
                    position = new Vector2(
                        posObj["x"]?.ToObject<float>() ?? 0f,
                        posObj["z"]?.ToObject<float>() ?? 0f
                    );
                }
                
                // 查找地形
                Terrain terrain = null;
                if (terrainId != 0)
                {
                    var obj = EditorUtility.InstanceIDToObject(terrainId) as GameObject;
                    terrain = obj?.GetComponent<Terrain>();
                }
                else if (!string.IsNullOrEmpty(terrainName))
                {
                    var terrainObj = GameObject.Find(terrainName);
                    terrain = terrainObj?.GetComponent<Terrain>();
                }
                
                if (terrain == null)
                {
                    return "{\"success\": false, \"message\": \"未找到指定的地形\"}";
                }
                
                // 记录撤销操作
                Undo.RecordObject(terrain.terrainData, "Paint Terrain Texture");
                
                TerrainData terrainData = terrain.terrainData;
                
                // 如果提供了纹理路径，添加新的地形层
                if (!string.IsNullOrEmpty(texturePath))
                {
                    Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
                    if (texture != null)
                    {
                        // 创建新的地形层
                        TerrainLayer terrainLayer = new TerrainLayer();
                        terrainLayer.diffuseTexture = texture;
                        terrainLayer.tileSize = new Vector2(15, 15); // 默认平铺大小
                        
                        // 保存地形层资产
                        string layerPath = $"Assets/TerrainLayer_{texture.name}.terrainlayer";
                        AssetDatabase.CreateAsset(terrainLayer, layerPath);
                        
                        // 添加到地形
                        var layers = new List<TerrainLayer>(terrainData.terrainLayers);
                        layers.Add(terrainLayer);
                        terrainData.terrainLayers = layers.ToArray();
                        
                        textureIndex = layers.Count - 1;
                    }
                }
                
                // 检查纹理索引是否有效
                if (textureIndex >= terrainData.terrainLayers.Length)
                {
                    return "{\"success\": false, \"message\": \"纹理索引超出范围\"}";
                }
                
                // 获取地形尺寸信息
                Vector3 terrainPos = terrain.transform.position;
                Vector3 terrainSize = terrainData.size;
                int alphamapWidth = terrainData.alphamapWidth;
                int alphamapHeight = terrainData.alphamapHeight;
                
                // 转换世界坐标到地形坐标
                float normalizedX = (position.x - terrainPos.x) / terrainSize.x;
                float normalizedZ = (position.y - terrainPos.z) / terrainSize.z;
                
                int centerX = Mathf.RoundToInt(normalizedX * (alphamapWidth - 1));
                int centerZ = Mathf.RoundToInt(normalizedZ * (alphamapHeight - 1));
                
                // 计算笔刷范围
                int brushRadius = Mathf.RoundToInt(brushSize / 2f);
                int startX = Mathf.Max(0, centerX - brushRadius);
                int endX = Mathf.Min(alphamapWidth - 1, centerX + brushRadius);
                int startZ = Mathf.Max(0, centerZ - brushRadius);
                int endZ = Mathf.Min(alphamapHeight - 1, centerZ + brushRadius);
                
                // 获取当前alpha贴图
                float[,,] alphamaps = terrainData.GetAlphamaps(startX, startZ, endX - startX + 1, endZ - startZ + 1);
                
                // 绘制纹理
                for (int x = 0; x < alphamaps.GetLength(0); x++)
                {
                    for (int z = 0; z < alphamaps.GetLength(1); z++)
                    {
                        int worldX = startX + x;
                        int worldZ = startZ + z;
                        
                        // 计算距离中心的距离
                        float distance = Vector2.Distance(new Vector2(worldX, worldZ), new Vector2(centerX, centerZ));
                        
                        if (distance <= brushRadius)
                        {
                            // 计算衰减
                            float falloff = 1f - (distance / brushRadius);
                            float paintStrength = strength * falloff;
                            
                            // 增加目标纹理的权重
                            alphamaps[x, z, textureIndex] += paintStrength;
                            
                            // 归一化所有纹理权重
                            float totalWeight = 0f;
                            for (int i = 0; i < alphamaps.GetLength(2); i++)
                            {
                                totalWeight += alphamaps[x, z, i];
                            }
                            
                            if (totalWeight > 0f)
                            {
                                for (int i = 0; i < alphamaps.GetLength(2); i++)
                                {
                                    alphamaps[x, z, i] /= totalWeight;
                                }
                            }
                        }
                    }
                }
                
                // 应用修改的alpha贴图
                terrainData.SetAlphamaps(startX, startZ, alphamaps);
                
                AssetDatabase.SaveAssets();
                
                McpLogger.LogTool($"地形纹理绘制成功: {terrain.name}");
                return $"{{\"success\": true, \"message\": \"地形 '{terrain.name}' 纹理绘制成功\"}}";
#else
                return "{\"success\": false, \"message\": \"此功能仅在编辑器模式下可用\"}";
#endif
            }
            catch (Exception ex)
            {
                McpLogger.LogException(ex, "绘制地形纹理时发生错误");
                return $"{{\"success\": false, \"message\": \"绘制地形纹理失败: {ex.Message}\"}}";
            }
        }
        
        /// <summary>
        /// 创建天空盒
        /// </summary>
        /// <param name="parameters">天空盒参数JSON</param>
        /// <returns>操作结果</returns>
        public static string CreateSkybox(string parameters)
        {
            try
            {
#if UNITY_EDITOR
                var paramObj = JObject.Parse(parameters);
                
                // 解析参数
                string skyboxType = paramObj["type"]?.ToString() ?? "procedural";
                string materialName = paramObj["materialName"]?.ToString() ?? "New Skybox";
                
                Material skyboxMaterial = null;
                
                switch (skyboxType.ToLower())
                {
                    case "procedural":
                        skyboxMaterial = CreateProceduralSkybox(paramObj, materialName);
                        break;
                    case "6sided":
                        skyboxMaterial = Create6SidedSkybox(paramObj, materialName);
                        break;
                    case "cubemap":
                        skyboxMaterial = CreateCubemapSkybox(paramObj, materialName);
                        break;
                    case "panoramic":
                        skyboxMaterial = CreatePanoramicSkybox(paramObj, materialName);
                        break;
                    default:
                        return "{\"success\": false, \"message\": \"不支持的天空盒类型\"}";
                }
                
                if (skyboxMaterial == null)
                {
                    return "{\"success\": false, \"message\": \"创建天空盒材质失败\"}";
                }
                
                // 保存材质资产
                string assetPath = $"Assets/Skybox_{materialName}.mat";
                AssetDatabase.CreateAsset(skyboxMaterial, assetPath);
                AssetDatabase.SaveAssets();
                
                // 应用到渲染设置
                bool applyToScene = paramObj["applyToScene"]?.ToObject<bool>() ?? true;
                if (applyToScene)
                {
                    RenderSettings.skybox = skyboxMaterial;
                }
                
                McpLogger.LogTool($"天空盒创建成功: {materialName}");
                return $"{{\"success\": true, \"message\": \"天空盒 '{materialName}' 创建成功\", \"assetPath\": \"{assetPath}\"}}";
#else
                return "{\"success\": false, \"message\": \"此功能仅在编辑器模式下可用\"}";
#endif
            }
            catch (Exception ex)
            {
                McpLogger.LogException(ex, "创建天空盒时发生错误");
                return $"{{\"success\": false, \"message\": \"创建天空盒失败: {ex.Message}\"}}";
            }
        }
        
        #endregion
        
        #region Private Helper Methods
        
#if UNITY_EDITOR
        private static Material CreateProceduralSkybox(JObject paramObj, string materialName)
        {
            // 创建程序化天空盒材质
            Shader skyboxShader = Shader.Find("Skybox/Procedural");
            if (skyboxShader == null) return null;
            
            Material material = new Material(skyboxShader);
            material.name = materialName;
            
            // 设置程序化天空盒参数
            if (paramObj["sunSize"] != null)
                material.SetFloat("_SunSize", paramObj["sunSize"].ToObject<float>());
            if (paramObj["sunSizeConvergence"] != null)
                material.SetFloat("_SunSizeConvergence", paramObj["sunSizeConvergence"].ToObject<float>());
            if (paramObj["atmosphereThickness"] != null)
                material.SetFloat("_AtmosphereThickness", paramObj["atmosphereThickness"].ToObject<float>());
            if (paramObj["skyTint"] != null)
            {
                var colorObj = paramObj["skyTint"];
                Color skyTint = new Color(
                    colorObj["r"]?.ToObject<float>() ?? 0.5f,
                    colorObj["g"]?.ToObject<float>() ?? 0.5f,
                    colorObj["b"]?.ToObject<float>() ?? 0.5f,
                    1f
                );
                material.SetColor("_SkyTint", skyTint);
            }
            if (paramObj["groundColor"] != null)
            {
                var colorObj = paramObj["groundColor"];
                Color groundColor = new Color(
                    colorObj["r"]?.ToObject<float>() ?? 0.369f,
                    colorObj["g"]?.ToObject<float>() ?? 0.349f,
                    colorObj["b"]?.ToObject<float>() ?? 0.341f,
                    1f
                );
                material.SetColor("_GroundColor", groundColor);
            }
            if (paramObj["exposure"] != null)
                material.SetFloat("_Exposure", paramObj["exposure"].ToObject<float>());
            
            return material;
        }
        
        private static Material Create6SidedSkybox(JObject paramObj, string materialName)
        {
            // 创建6面天空盒材质
            Shader skyboxShader = Shader.Find("Skybox/6 Sided");
            if (skyboxShader == null) return null;
            
            Material material = new Material(skyboxShader);
            material.name = materialName;
            
            // 设置6面纹理
            string[] faces = { "front", "back", "left", "right", "up", "down" };
            string[] properties = { "_FrontTex", "_BackTex", "_LeftTex", "_RightTex", "_UpTex", "_DownTex" };
            
            for (int i = 0; i < faces.Length; i++)
            {
                if (paramObj[faces[i]] != null)
                {
                    string texturePath = paramObj[faces[i]].ToString();
                    Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
                    if (texture != null)
                    {
                        material.SetTexture(properties[i], texture);
                    }
                }
            }
            
            // 设置其他参数
            if (paramObj["tint"] != null)
            {
                var colorObj = paramObj["tint"];
                Color tint = new Color(
                    colorObj["r"]?.ToObject<float>() ?? 0.5f,
                    colorObj["g"]?.ToObject<float>() ?? 0.5f,
                    colorObj["b"]?.ToObject<float>() ?? 0.5f,
                    1f
                );
                material.SetColor("_Tint", tint);
            }
            if (paramObj["exposure"] != null)
                material.SetFloat("_Exposure", paramObj["exposure"].ToObject<float>());
            if (paramObj["rotation"] != null)
                material.SetFloat("_Rotation", paramObj["rotation"].ToObject<float>());
            
            return material;
        }
        
        private static Material CreateCubemapSkybox(JObject paramObj, string materialName)
        {
            // 创建立方体贴图天空盒材质
            Shader skyboxShader = Shader.Find("Skybox/Cubemap");
            if (skyboxShader == null) return null;
            
            Material material = new Material(skyboxShader);
            material.name = materialName;
            
            // 设置立方体贴图
            if (paramObj["cubemap"] != null)
            {
                string cubemapPath = paramObj["cubemap"].ToString();
                Cubemap cubemap = AssetDatabase.LoadAssetAtPath<Cubemap>(cubemapPath);
                if (cubemap != null)
                {
                    material.SetTexture("_Tex", cubemap);
                }
            }
            
            // 设置其他参数
            if (paramObj["tint"] != null)
            {
                var colorObj = paramObj["tint"];
                Color tint = new Color(
                    colorObj["r"]?.ToObject<float>() ?? 0.5f,
                    colorObj["g"]?.ToObject<float>() ?? 0.5f,
                    colorObj["b"]?.ToObject<float>() ?? 0.5f,
                    1f
                );
                material.SetColor("_Tint", tint);
            }
            if (paramObj["exposure"] != null)
                material.SetFloat("_Exposure", paramObj["exposure"].ToObject<float>());
            if (paramObj["rotation"] != null)
                material.SetFloat("_Rotation", paramObj["rotation"].ToObject<float>());
            
            return material;
        }
        
        private static Material CreatePanoramicSkybox(JObject paramObj, string materialName)
        {
            // 创建全景天空盒材质
            Shader skyboxShader = Shader.Find("Skybox/Panoramic");
            if (skyboxShader == null) return null;
            
            Material material = new Material(skyboxShader);
            material.name = materialName;
            
            // 设置全景纹理
            if (paramObj["panoramicTexture"] != null)
            {
                string texturePath = paramObj["panoramicTexture"].ToString();
                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
                if (texture != null)
                {
                    material.SetTexture("_MainTex", texture);
                }
            }
            
            // 设置其他参数
            if (paramObj["tint"] != null)
            {
                var colorObj = paramObj["tint"];
                Color tint = new Color(
                    colorObj["r"]?.ToObject<float>() ?? 0.5f,
                    colorObj["g"]?.ToObject<float>() ?? 0.5f,
                    colorObj["b"]?.ToObject<float>() ?? 0.5f,
                    1f
                );
                material.SetColor("_Tint", tint);
            }
            if (paramObj["exposure"] != null)
                material.SetFloat("_Exposure", paramObj["exposure"].ToObject<float>());
            if (paramObj["rotation"] != null)
                material.SetFloat("_Rotation", paramObj["rotation"].ToObject<float>());
            if (paramObj["mapping"] != null)
                material.SetFloat("_Mapping", paramObj["mapping"].ToObject<float>());
            if (paramObj["imageType"] != null)
                material.SetFloat("_ImageType", paramObj["imageType"].ToObject<float>());
            if (paramObj["mirrorOnBack"] != null)
                material.SetFloat("_MirrorOnBack", paramObj["mirrorOnBack"].ToObject<bool>() ? 1f : 0f);
            if (paramObj["layout"] != null)
                material.SetFloat("_Layout", paramObj["layout"].ToObject<float>());
            
            return material;
        }
#endif
        
        #endregion
    }
}