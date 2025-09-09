using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System;
using Unity.MCP;
using Unity.MCP.Editor;
using Unity.MCP.Tools.Editor;
using Newtonsoft.Json.Linq;

namespace Unity.MCP.Tools.Editor
{
    /// <summary>
    /// Unity MCP工具主入口类 - 模块化重构版本
    /// </summary>
    public static class UnityToolsMain
    {
        #region Events - 事件
        
        /// <summary>
        /// 场景变化事件
        /// </summary>
        public static event System.Action<string, string> OnSceneChanged;
        
        #endregion
        
        #region Scene Management - 场景管理
        
        public static McpToolResult ListScenes(JObject arguments)
        {
            return UnitySceneTools.ListScenes(arguments);
        }
        
        public static McpToolResult GetPlayModeStatus(JObject arguments)
        {
            return UnitySceneTools.GetPlayModeStatus(arguments);
        }
        
        public static McpToolResult GetCurrentSceneInfo(JObject arguments)
        {
            return UnitySceneTools.GetCurrentSceneInfo(arguments);
        }
        
        public static McpToolResult StartPlayMode(JObject arguments)
        {
            return UnityPlayModeTools.StartPlayMode(arguments);
        }
        
        public static McpToolResult StopPlayMode(JObject arguments)
        {
            return UnityPlayModeTools.StopPlayMode(arguments);
        }
        
        #endregion
        
        #region Script Management - 脚本管理
        
        public static McpToolResult CreateScript(string scriptName, string scriptType, string namespaceName = null, string baseClass = null, string savePath = null)
        {
            return UnityScriptTools.CreateScript(scriptName, scriptType, namespaceName, baseClass, savePath);
        }
        
        public static McpToolResult CreateScript(JObject arguments)
        {
            return UnityScriptTools.CreateScript(arguments);
        }
        
        public static McpToolResult ModifyScript(string scriptPath, string newContent)
        {
            var arguments = new JObject();
            arguments["path"] = scriptPath;
            arguments["content"] = newContent;
            return UnityScriptTools.ModifyScript(arguments);
        }
        
        public static McpToolResult ModifyScript(JObject arguments)
        {
            return UnityScriptTools.ModifyScript(arguments);
        }
        
        public static McpToolResult GetScriptErrors(JObject arguments)
        {
            return UnityScriptTools.GetScriptErrors(arguments);
        }
        
        #endregion
        
        #region GameObject Management - 游戏对象管理
        
        public static McpToolResult CreateGameObject(string name, string parentName = null, Vector3? position = null, Vector3? rotation = null, Vector3? scale = null)
        {
            var arguments = new JObject();
            arguments["name"] = name;
            if (!string.IsNullOrEmpty(parentName))
                arguments["parent"] = parentName;
            if (position.HasValue)
                arguments["position"] = JObject.FromObject(new { x = position.Value.x, y = position.Value.y, z = position.Value.z });
            if (rotation.HasValue)
                arguments["rotation"] = JObject.FromObject(new { x = rotation.Value.x, y = rotation.Value.y, z = rotation.Value.z });
            if (scale.HasValue)
                arguments["scale"] = JObject.FromObject(new { x = scale.Value.x, y = scale.Value.y, z = scale.Value.z });
            
            return UnityGameObjectTools.CreateGameObject(arguments);
        }
        
        public static McpToolResult CreateGameObject(JObject arguments)
        {
            return UnityGameObjectTools.CreateGameObject(arguments);
        }
        
        public static McpToolResult AddComponent(string gameObjectName, string componentType, string componentData = null)
        {
            var arguments = new JObject();
            arguments["gameObject"] = gameObjectName;
            arguments["component"] = componentType;
            if (!string.IsNullOrEmpty(componentData))
                arguments["componentData"] = componentData;
            
            return UnityGameObjectTools.AddComponent(arguments);
        }
        
        public static McpToolResult AddComponent(JObject arguments)
        {
            return UnityGameObjectTools.AddComponent(arguments);
        }
        
        public static McpToolResult FindGameObject(string name, bool includeInactive = false)
        {
            var arguments = new JObject();
            arguments["name"] = name;
            arguments["includeInactive"] = includeInactive;
            return UnityGameObjectTools.FindGameObject(arguments);
        }
        
        public static McpToolResult FindGameObject(JObject arguments)
        {
            return UnityGameObjectTools.FindGameObject(arguments);
        }
        
        public static McpToolResult DeleteGameObject(string name)
        {
            var arguments = new JObject();
            arguments["name"] = name;
            return UnityGameObjectTools.DeleteGameObject(arguments);
        }
        
        public static McpToolResult DeleteGameObject(JObject arguments)
        {
            return UnityGameObjectTools.DeleteGameObject(arguments);
        }
        
        public static McpToolResult DuplicateGameObject(string name, string newName = null)
        {
            var arguments = new JObject();
            arguments["name"] = name;
            if (!string.IsNullOrEmpty(newName))
                arguments["newName"] = newName;
            return UnityGameObjectTools.DuplicateGameObject(arguments);
        }
        
        public static McpToolResult DuplicateGameObject(JObject arguments)
        {
            return UnityGameObjectTools.DuplicateGameObject(arguments);
        }
        
        public static McpToolResult SetParent(string childName, string parentName)
        {
            var arguments = new JObject();
            arguments["child"] = childName;
            arguments["parent"] = parentName;
            return UnityGameObjectTools.SetParent(arguments);
        }
        
        public static McpToolResult SetParent(JObject arguments)
        {
            return UnityGameObjectTools.SetParent(arguments);
        }
        
        public static McpToolResult SetTransform(JObject arguments)
        {
            return UnityGameObjectTools.SetTransform(arguments);
        }
        
        public static McpToolResult GetGameObjectInfo(string name)
        {
            var arguments = new JObject();
            arguments["name"] = name;
            return UnityGameObjectTools.GetGameObjectInfo(arguments);
        }
        
        public static McpToolResult GetGameObjectInfo(JObject arguments)
        {
            return UnityGameObjectTools.GetGameObjectInfo(arguments);
        }
        
        public static McpToolResult RemoveComponent(string gameObjectName, string componentType)
        {
            var arguments = new JObject();
            arguments["gameObject"] = gameObjectName;
            arguments["component"] = componentType;
            return UnityGameObjectTools.RemoveComponent(arguments);
        }
        
        public static McpToolResult RemoveComponent(JObject arguments)
        {
            return UnityGameObjectTools.RemoveComponent(arguments);
        }
        
        #endregion
        
        #region UI System - UI系统
        
        public static McpToolResult CreateCanvas(string canvasName, string renderMode = "ScreenSpaceOverlay")
        {
            var arguments = new JObject();
            arguments["canvasName"] = canvasName;
            arguments["renderMode"] = renderMode;
            return UnityUITools.CreateCanvas(arguments);
        }
        
        public static McpToolResult CreateCanvas(JObject arguments)
        {
            return UnityUITools.CreateCanvas(arguments);
        }
        
        public static McpToolResult CreateUIElement(string elementName, string elementType, string parentName = null)
        {
            var arguments = new JObject();
            arguments["elementName"] = elementName;
            arguments["elementType"] = elementType;
            if (!string.IsNullOrEmpty(parentName))
                arguments["parentName"] = parentName;
            return UnityUITools.CreateUIElement(arguments);
        }
        
        public static McpToolResult CreateUIElement(JObject arguments)
        {
            return UnityUITools.CreateUIElement(arguments);
        }
        
        public static McpToolResult SetUIProperties(string elementName, string propertiesJson)
        {
            var arguments = new JObject();
            arguments["elementName"] = elementName;
            arguments["properties"] = JObject.Parse(propertiesJson);
            return UnityUITools.SetUIProperties(arguments);
        }
        
        public static McpToolResult SetUIProperties(JObject arguments)
        {
            return UnityUITools.SetUIProperties(arguments);
        }
        
        public static McpToolResult BindUIEvents(string elementName, string eventType, string methodName)
        {
            var arguments = new JObject();
            arguments["elementName"] = elementName;
            arguments["eventType"] = eventType;
            arguments["methodName"] = methodName;
            return UnityUITools.BindUIEvents(arguments);
        }
        
        public static McpToolResult BindUIEvents(JObject arguments)
        {
            return UnityUITools.BindUIEvents(arguments);
        }
        
        #endregion
        
        #region Animation System - 动画系统
        
        public static McpToolResult CreateAnimator(string gameObjectName, string animatorControllerPath = null)
        {
            return UnityAnimationTools.CreateAnimator(gameObjectName, animatorControllerPath);
        }
        
        public static McpToolResult SetAnimationClip(string gameObjectName, string clipName, string clipPath)
        {
            return UnityAnimationTools.SetAnimationClip(gameObjectName, clipName, clipPath);
        }
        
        public static McpToolResult PlayAnimation(string gameObjectName, string animationName = null, bool loop = false)
        {
            return UnityAnimationTools.PlayAnimation(gameObjectName, animationName, loop);
        }
        
        public static McpToolResult SetAnimationParameters(string gameObjectName, string parameters)
        {
            return UnityAnimationTools.SetAnimationParameters(gameObjectName, parameters);
        }
        
        public static McpToolResult CreateAnimationClip(string clipName, string savePath, string targetObjectName = null)
        {
            return UnityAnimationTools.CreateAnimationClip(clipName, savePath, targetObjectName);
        }
        
        #endregion
        
        #region Input System - 输入系统
        
        public static McpToolResult SetupInputActions(string inputActionsJson)
        {
            return UnityInputTools.SetupInputActions(inputActionsJson);
        }
        
        public static McpToolResult BindInputEvents(string gameObjectName, string inputEventBindings)
        {
            return UnityInputTools.BindInputEvents(gameObjectName, inputEventBindings);
        }
        
        public static McpToolResult SimulateInput(string inputType, string inputData)
        {
            return UnityInputTools.SimulateInput(inputType, inputData);
        }
        
        public static McpToolResult CreateInputMapping(string mappingName, string inputMappingData)
        {
            return UnityInputTools.CreateInputMapping(mappingName, inputMappingData);
        }
        
        #endregion
        
        #region Particle System - 粒子系统
        
        public static string CreateParticleSystem(string gameObjectName, string particleSystemName = "")
        {
            return UnityParticleTools.CreateParticleSystem(gameObjectName, particleSystemName);
        }
        
        public static string SetParticleProperties(string gameObjectName, string propertiesJson)
        {
            return UnityParticleTools.SetParticleProperties(gameObjectName, propertiesJson);
        }
        
        public static string PlayParticleEffect(string gameObjectName, bool play = true)
        {
            return UnityParticleTools.PlayParticleEffect(gameObjectName, play);
        }
        
        public static string CreateParticleEffect(string effectName, string effectType, string targetObjectName = "")
        {
            return UnityParticleTools.CreateParticleEffect(effectName, effectType, targetObjectName);
        }
        
        #endregion
        
        #region Terrain System - 地形系统
        
        public static string CreateTerrain(string parameters)
        {
            return Unity.MCP.Editor.UnityTerrainTools.CreateTerrain(parameters);
        }
        
        public static string ModifyTerrainHeight(string parameters)
        {
            return Unity.MCP.Editor.UnityTerrainTools.ModifyTerrain(parameters);
        }
        
        public static string PaintTerrainTexture(string parameters)
        {
            return Unity.MCP.Editor.UnityTerrainTools.PaintTerrainTexture(parameters);
        }
        
        public static string CreateSkybox(string parameters)
        {
            return Unity.MCP.Editor.UnityTerrainTools.CreateSkybox(parameters);
        }
        
        #endregion
        
        #region Game Logic Management - 游戏逻辑管理
        
        public static string CreateGameManager(string parameters)
        {
            return Unity.MCP.Editor.UnityGameLogicTools.CreateGameManager(parameters);
        }
        
        public static string SetGameState(string parameters)
        {
            return Unity.MCP.Editor.UnityGameLogicTools.SetGameState(parameters);
        }
        
        public static string SaveGameData(string parameters)
        {
            return Unity.MCP.Editor.UnityGameLogicTools.SaveGameData(parameters);
        }
        
        public static string LoadGameData(string parameters)
        {
            return Unity.MCP.Editor.UnityGameLogicTools.LoadGameData(parameters);
        }
        
        public static string GetGameState(string parameters)
        {
            return Unity.MCP.Editor.UnityGameLogicTools.GetGameState(parameters);
        }
        
        #endregion
        
        #region Component Management - 组件管理
        
        public static McpToolResult GetComponentProperties(string gameObjectName, string componentType)
        {
            var arguments = new JObject();
            arguments["gameObject"] = gameObjectName;
            arguments["component"] = componentType;
            return UnityComponentTools.GetComponentProperties(arguments);
        }
        
        public static McpToolResult GetComponentProperties(JObject arguments)
        {
            return UnityComponentTools.GetComponentProperties(arguments);
        }
        
        public static McpToolResult SetComponentProperties(string gameObjectName, string componentType, string propertiesJson)
        {
            var arguments = new JObject();
            arguments["gameObject"] = gameObjectName;
            arguments["component"] = componentType;
            arguments["properties"] = JObject.Parse(propertiesJson);
            return UnityComponentTools.SetComponentProperties(arguments);
        }
        
        public static McpToolResult SetComponentProperties(JObject arguments)
        {
            return UnityComponentTools.SetComponentProperties(arguments);
        }
        
        public static McpToolResult GetAllComponents(string gameObjectName)
        {
            var arguments = new JObject();
            arguments["gameObject"] = gameObjectName;
            return UnityComponentTools.GetAllComponents(arguments);
        }
        
        public static McpToolResult GetAllComponents(JObject arguments)
        {
            return UnityComponentTools.GetAllComponents(arguments);
        }
        
        public static McpToolResult ListComponents(string gameObjectName)
        {
            var arguments = new JObject();
            arguments["gameObject"] = gameObjectName;
            return UnityComponentTools.ListComponents(arguments);
        }
        
        public static McpToolResult ListComponents(JObject arguments)
        {
            return UnityComponentTools.ListComponents(arguments);
        }
        
        #endregion
        
        #region Audio System - 音频系统
        
        public static McpToolResult PlayAudio(string gameObjectName, string audioClipPath, bool loop = false, float volume = 1.0f, float pitch = 1.0f)
        {
            var arguments = new JObject();
            arguments["gameObject"] = gameObjectName;
            arguments["audioClip"] = audioClipPath;
            arguments["loop"] = loop;
            arguments["volume"] = volume;
            arguments["pitch"] = pitch;
            return UnityAudioTools.PlayAudio(arguments);
        }
        
        public static McpToolResult PlayAudio(JObject arguments)
        {
            return UnityAudioTools.PlayAudio(arguments);
        }
        
        public static McpToolResult StopAudio(string gameObjectName)
        {
            var arguments = new JObject();
            arguments["gameObject"] = gameObjectName;
            return UnityAudioTools.StopAudio(arguments);
        }
        
        public static McpToolResult StopAudio(JObject arguments)
        {
            return UnityAudioTools.StopAudio(arguments);
        }
        
        public static McpToolResult SetAudioVolume(string gameObjectName, float volume)
        {
            var arguments = new JObject();
            arguments["gameObject"] = gameObjectName;
            arguments["volume"] = volume;
            return UnityAudioTools.SetAudioVolume(arguments);
        }
        
        public static McpToolResult SetAudioVolume(JObject arguments)
        {
            return UnityAudioTools.SetAudioVolume(arguments);
        }
        
        #endregion
        
        #region Physics System - 物理系统
        
        public static McpToolResult SetRigidbodyProperties(string gameObjectName, string propertiesJson)
        {
            var arguments = new JObject();
            arguments["gameObject"] = gameObjectName;
            arguments["properties"] = JObject.Parse(propertiesJson);
            return UnityPhysicsTools.SetRigidbodyProperties(arguments);
        }
        
        public static McpToolResult SetRigidbodyProperties(JObject arguments)
        {
            return UnityPhysicsTools.SetRigidbodyProperties(arguments);
        }
        
        public static McpToolResult AddForce(string gameObjectName, Vector3 force, string forceMode = "Force")
        {
            var arguments = new JObject();
            arguments["gameObject"] = gameObjectName;
            arguments["force"] = JObject.FromObject(new { x = force.x, y = force.y, z = force.z });
            arguments["forceMode"] = forceMode;
            return UnityPhysicsTools.AddForce(arguments);
        }
        
        public static McpToolResult AddForce(JObject arguments)
        {
            return UnityPhysicsTools.AddForce(arguments);
        }
        
        public static McpToolResult SetColliderProperties(string gameObjectName, string colliderType, string propertiesJson)
        {
            var arguments = new JObject();
            arguments["gameObject"] = gameObjectName;
            arguments["colliderType"] = colliderType;
            arguments["properties"] = JObject.Parse(propertiesJson);
            return UnityPhysicsTools.SetColliderProperties(arguments);
        }
        
        public static McpToolResult SetColliderProperties(JObject arguments)
        {
            return UnityPhysicsTools.SetColliderProperties(arguments);
        }
        
        public static McpToolResult Raycast(JObject arguments)
        {
            return UnityPhysicsTools.Raycast(arguments);
        }
        
        #endregion
        
        #region Material System - 材质系统
        
        public static McpToolResult CreateMaterial(string name, string shaderName = "Standard")
        {
            var arguments = new JObject();
            arguments["name"] = name;
            arguments["shader"] = shaderName;
            return UnityMaterialTools.CreateMaterial(arguments);
        }
        
        public static McpToolResult CreateMaterial(JObject arguments)
        {
            return UnityMaterialTools.CreateMaterial(arguments);
        }
        
        public static McpToolResult SetMaterialProperties(string materialPath, string propertiesJson)
        {
            var arguments = new JObject();
            arguments["materialPath"] = materialPath;
            arguments["properties"] = JObject.Parse(propertiesJson);
            return UnityMaterialTools.SetMaterialProperties(arguments);
        }
        
        public static McpToolResult SetMaterialProperties(JObject arguments)
        {
            return UnityMaterialTools.SetMaterialProperties(arguments);
        }
        
        public static McpToolResult ApplyMaterial(string gameObjectName, string materialPath)
        {
            var arguments = new JObject();
            arguments["gameObject"] = gameObjectName;
            arguments["materialPath"] = materialPath;
            return UnityMaterialTools.ApplyMaterial(arguments);
        }
        
        public static McpToolResult ApplyMaterial(JObject arguments)
        {
            return UnityMaterialTools.ApplyMaterial(arguments);
        }
        
        public static McpToolResult AssignMaterial(string gameObjectName, string materialPath)
        {
            var arguments = new JObject();
            arguments["gameObject"] = gameObjectName;
            arguments["materialPath"] = materialPath;
            return UnityMaterialTools.ApplyMaterial(arguments);
        }
        
        public static McpToolResult AssignMaterial(JObject arguments)
        {
            return UnityMaterialTools.ApplyMaterial(arguments);
        }
        
        #endregion
        
        #region Debug and Diagnostics - 调试和诊断
        
        public static McpToolResult GetThreadStackInfo(JObject arguments)
        {
            return UnityTools.GetThreadStackInfo(arguments);
        }
        
        #endregion
        
        #region Asset Management - 资源管理系统
        public static McpToolResult ImportAsset(JObject arguments)
        {
            return UnityAssetTools.ImportAsset(arguments);
        }
        
        public static McpToolResult RefreshAssets(JObject arguments)
        {
            return UnityAssetTools.RefreshAssets(arguments);
        }
        
        public static McpToolResult CompileScripts(JObject arguments)
        {
            return UnityAssetTools.CompileScripts(arguments);
        }
        
        public static McpToolResult WaitForCompilation(JObject arguments)
        {
            return UnityAssetTools.WaitForCompilation(arguments);
        }
        #endregion

        #region Audio System - 音频系统
        public static McpToolResult SetAudioProperties(JObject arguments)
        {
            return UnityAudioTools.SetAudioProperties(arguments);
        }
        #endregion

        #region Lighting System - 光照系统
        public static McpToolResult CreateLight(JObject arguments)
        {
            return UnityLightTools.CreateLight(arguments);
        }

        public static McpToolResult SetLightProperties(JObject arguments)
        {
            return UnityLightTools.SetLightProperties(arguments);
        }
        #endregion

        #region Build System - 构建系统
        
        public static string BuildProject(string parameters)
        {
            return Unity.MCP.Editor.UnityBuildTools.BuildProject(parameters);
        }
        
        public static string SetBuildSettings(string parameters)
        {
            return Unity.MCP.Editor.UnityBuildTools.SetBuildSettings(parameters);
        }
        
        public static string ExportPackage(string parameters)
        {
            return Unity.MCP.Editor.UnityBuildTools.ExportPackage(parameters);
        }
        
        public static string GetBuildInfo(string parameters)
        {
            return Unity.MCP.Editor.UnityBuildTools.GetBuildInfo(parameters);
        }
        
        #endregion

        #region Editor Tools - 编辑器工具
        
        /// <summary>
        /// 强制刷新Unity编辑器界面
        /// </summary>
        /// <param name="arguments">参数</param>
        /// <returns>操作结果</returns>
        public static McpToolResult RefreshEditor(JObject arguments)
        {
            return UnityEditorTools.RefreshEditor(arguments);
        }
        
        /// <summary>
        /// 获取编辑器状态信息
        /// </summary>
        /// <param name="arguments">参数</param>
        /// <returns>编辑器状态信息</returns>
        public static McpToolResult GetEditorStatus(JObject arguments)
        {
            return UnityEditorTools.GetEditorStatus(arguments);
        }
        
        #endregion

        #region Prefab Management - 预制体管理
        
        /// <summary>
        /// 将游戏对象保存为预制体
        /// </summary>
        /// <param name="arguments">参数</param>
        /// <returns>操作结果</returns>
        public static McpToolResult CreatePrefab(JObject arguments)
        {
            return UnityPrefabTools.CreatePrefab(arguments);
        }
        
        /// <summary>
        /// 实例化预制体
        /// </summary>
        /// <param name="arguments">参数</param>
        /// <returns>操作结果</returns>
        public static McpToolResult InstantiatePrefab(JObject arguments)
        {
            return UnityPrefabTools.InstantiatePrefab(arguments);
        }
        
        /// <summary>
        /// 列出项目中的所有预制体
        /// </summary>
        /// <param name="arguments">参数</param>
        /// <returns>预制体列表</returns>
        public static McpToolResult ListPrefabs(JObject arguments)
        {
            return UnityPrefabTools.ListPrefabs(arguments);
        }
        
        /// <summary>
        /// 获取预制体信息
        /// </summary>
        /// <param name="arguments">参数</param>
        /// <returns>预制体信息</returns>
        public static McpToolResult GetPrefabInfo(JObject arguments)
        {
            return UnityPrefabTools.GetPrefabInfo(arguments);
        }
        
        #endregion
    }
}