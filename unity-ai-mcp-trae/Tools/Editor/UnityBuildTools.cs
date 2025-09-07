using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.MCP;
using Unity.MCP.Editor;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build.Reporting;
#endif

namespace Unity.MCP.Editor
{
    public static class UnityBuildTools
    {
        #region Build System
        
        /// <summary>
        /// 构建项目
        /// </summary>
        /// <param name="parameters">构建参数JSON</param>
        /// <returns>操作结果</returns>
        public static string BuildProject(string parameters)
        {
            try
            {
#if UNITY_EDITOR
                var paramObj = JObject.Parse(parameters);
                
                // 解析参数
                string targetPlatform = paramObj["platform"]?.ToString() ?? "StandaloneWindows64";
                string buildPath = paramObj["buildPath"]?.ToString();
                string productName = paramObj["productName"]?.ToString() ?? PlayerSettings.productName;
                bool developmentBuild = paramObj["developmentBuild"]?.ToObject<bool>() ?? false;
                bool autoRunPlayer = paramObj["autoRunPlayer"]?.ToObject<bool>() ?? false;
                bool allowDebugging = paramObj["allowDebugging"]?.ToObject<bool>() ?? false;
                bool connectProfiler = paramObj["connectProfiler"]?.ToObject<bool>() ?? false;
                var scenePaths = paramObj["scenes"]?.ToObject<string[]>();
                
                // 获取构建目标
                BuildTarget buildTarget = GetBuildTarget(targetPlatform);
                if (buildTarget == BuildTarget.NoTarget)
                {
                    return $"{{\"success\": false, \"message\": \"不支持的构建平台: {targetPlatform}\"}}";
                }
                
                // 设置构建路径
                if (string.IsNullOrEmpty(buildPath))
                {
                    string defaultPath = Path.Combine(Directory.GetCurrentDirectory(), "Builds", targetPlatform);
                    buildPath = Path.Combine(defaultPath, $"{productName}{GetExecutableExtension(buildTarget)}");
                }
                
                // 确保构建目录存在
                string buildDir = Path.GetDirectoryName(buildPath);
                if (!Directory.Exists(buildDir))
                {
                    Directory.CreateDirectory(buildDir);
                }
                
                // 获取场景列表
                string[] scenes;
                if (scenePaths != null && scenePaths.Length > 0)
                {
                    scenes = scenePaths;
                }
                else
                {
                    // 使用构建设置中的场景
                    scenes = EditorBuildSettings.scenes
                        .Where(scene => scene.enabled)
                        .Select(scene => scene.path)
                        .ToArray();
                }
                
                if (scenes.Length == 0)
                {
                    return "{\"success\": false, \"message\": \"没有找到可构建的场景\"}";
                }
                
                // 配置构建选项
                BuildOptions buildOptions = BuildOptions.None;
                if (developmentBuild) buildOptions |= BuildOptions.Development;
                if (autoRunPlayer) buildOptions |= BuildOptions.AutoRunPlayer;
                if (allowDebugging) buildOptions |= BuildOptions.AllowDebugging;
                if (connectProfiler) buildOptions |= BuildOptions.ConnectWithProfiler;
                
                // 创建构建参数
                BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
                {
                    scenes = scenes,
                    locationPathName = buildPath,
                    target = buildTarget,
                    options = buildOptions
                };
                
                // 执行构建
                McpLogger.LogTool($"开始构建项目: {productName} ({targetPlatform})");
                BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
                
                // 分析构建结果
                BuildSummary summary = report.summary;
                
                if (summary.result == BuildResult.Succeeded)
                {
                    var buildInfo = new
                    {
                        success = true,
                        message = "项目构建成功",
                        buildPath = buildPath,
                        platform = targetPlatform,
                        buildSize = GetBuildSize(buildPath),
                        buildTime = summary.buildEndedAt - summary.buildStartedAt,
                        totalWarnings = summary.totalWarnings,
                        totalErrors = summary.totalErrors
                    };
                    
                    McpLogger.LogTool($"构建成功: {buildPath}");
                    return JsonConvert.SerializeObject(buildInfo);
                }
                else
                {
                    string errorMessage = $"构建失败: {summary.result}";
                    if (summary.totalErrors > 0)
                    {
                        errorMessage += $" (错误数: {summary.totalErrors})";
                    }
                    
                    McpLogger.LogError(errorMessage);
                    return $"{{\"success\": false, \"message\": \"{errorMessage}\", \"totalErrors\": {summary.totalErrors}, \"totalWarnings\": {summary.totalWarnings}}}";
                }
#else
                return "{\"success\": false, \"message\": \"此功能仅在编辑器模式下可用\"}";
#endif
            }
            catch (Exception ex)
            {
                McpLogger.LogException(ex, "构建项目时发生错误");
                return $"{{\"success\": false, \"message\": \"构建项目失败: {ex.Message}\"}}";
            }
        }
        
        /// <summary>
        /// 设置构建设置
        /// </summary>
        /// <param name="parameters">构建设置参数JSON</param>
        /// <returns>操作结果</returns>
        public static string SetBuildSettings(string parameters)
        {
            try
            {
#if UNITY_EDITOR
                var paramObj = JObject.Parse(parameters);
                
                // 产品设置
                if (paramObj["productName"] != null)
                {
                    PlayerSettings.productName = paramObj["productName"].ToString();
                }
                
                if (paramObj["companyName"] != null)
                {
                    PlayerSettings.companyName = paramObj["companyName"].ToString();
                }
                
                if (paramObj["version"] != null)
                {
                    PlayerSettings.bundleVersion = paramObj["version"].ToString();
                }
                
                if (paramObj["bundleIdentifier"] != null)
                {
                    PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Standalone, paramObj["bundleIdentifier"].ToString());
                }
                
                // 图标设置
                if (paramObj["iconPath"] != null)
                {
                    string iconPath = paramObj["iconPath"].ToString();
                    Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath);
                    if (icon != null)
                    {
                        PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Standalone, new Texture2D[] { icon });
                    }
                }
                
                // 分辨率和显示设置
                if (paramObj["defaultScreenWidth"] != null)
                {
                    PlayerSettings.defaultScreenWidth = paramObj["defaultScreenWidth"].ToObject<int>();
                }
                
                if (paramObj["defaultScreenHeight"] != null)
                {
                    PlayerSettings.defaultScreenHeight = paramObj["defaultScreenHeight"].ToObject<int>();
                }
                
                if (paramObj["fullscreenMode"] != null)
                {
                    string fullscreenModeStr = paramObj["fullscreenMode"].ToString();
                    if (Enum.TryParse<FullScreenMode>(fullscreenModeStr, out FullScreenMode fullscreenMode))
                    {
                        PlayerSettings.fullScreenMode = fullscreenMode;
                    }
                }
                
                if (paramObj["resizableWindow"] != null)
                {
                    PlayerSettings.resizableWindow = paramObj["resizableWindow"].ToObject<bool>();
                }
                
                // 质量设置
                if (paramObj["qualityLevel"] != null)
                {
                    int qualityLevel = paramObj["qualityLevel"].ToObject<int>();
                    QualitySettings.SetQualityLevel(qualityLevel);
                }
                
                // 脚本编译设置
                if (paramObj["scriptingBackend"] != null)
                {
                    string backendStr = paramObj["scriptingBackend"].ToString();
                    if (Enum.TryParse<ScriptingImplementation>(backendStr, out ScriptingImplementation backend))
                    {
                        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, backend);
                    }
                }
                
                if (paramObj["apiCompatibilityLevel"] != null)
                {
                    string apiLevelStr = paramObj["apiCompatibilityLevel"].ToString();
                    if (Enum.TryParse<ApiCompatibilityLevel>(apiLevelStr, out ApiCompatibilityLevel apiLevel))
                    {
                        PlayerSettings.SetApiCompatibilityLevel(BuildTargetGroup.Standalone, apiLevel);
                    }
                }
                
                // 场景设置
                if (paramObj["scenes"] != null)
                {
                    var sceneArray = paramObj["scenes"].ToObject<string[]>();
                    if (sceneArray != null && sceneArray.Length > 0)
                    {
                        List<EditorBuildSettingsScene> buildScenes = new List<EditorBuildSettingsScene>();
                        
                        for (int i = 0; i < sceneArray.Length; i++)
                        {
                            string scenePath = sceneArray[i];
                            if (File.Exists(scenePath))
                            {
                                buildScenes.Add(new EditorBuildSettingsScene(scenePath, true));
                            }
                        }
                        
                        EditorBuildSettings.scenes = buildScenes.ToArray();
                    }
                }
                
                // 保存设置
                AssetDatabase.SaveAssets();
                
                McpLogger.LogTool("构建设置已更新");
                return "{\"success\": true, \"message\": \"构建设置已成功更新\"}";
#else
                return "{\"success\": false, \"message\": \"此功能仅在编辑器模式下可用\"}";
#endif
            }
            catch (Exception ex)
            {
                McpLogger.LogException(ex, "设置构建设置时发生错误");
                return $"{{\"success\": false, \"message\": \"设置构建设置失败: {ex.Message}\"}}";
            }
        }
        
        /// <summary>
        /// 导出Unity包
        /// </summary>
        /// <param name="parameters">导出参数JSON</param>
        /// <returns>操作结果</returns>
        public static string ExportPackage(string parameters)
        {
            try
            {
#if UNITY_EDITOR
                var paramObj = JObject.Parse(parameters);
                
                // 解析参数
                string packageName = paramObj["packageName"]?.ToString() ?? "ExportedPackage";
                string exportPath = paramObj["exportPath"]?.ToString();
                var assetPaths = paramObj["assetPaths"]?.ToObject<string[]>();
                bool includeLibraryAssets = paramObj["includeLibraryAssets"]?.ToObject<bool>() ?? false;
                bool includeDependencies = paramObj["includeDependencies"]?.ToObject<bool>() ?? true;
                bool interactive = paramObj["interactive"]?.ToObject<bool>() ?? false;
                
                // 设置导出路径
                if (string.IsNullOrEmpty(exportPath))
                {
                    string defaultDir = Path.Combine(Directory.GetCurrentDirectory(), "Exports");
                    if (!Directory.Exists(defaultDir))
                    {
                        Directory.CreateDirectory(defaultDir);
                    }
                    exportPath = Path.Combine(defaultDir, $"{packageName}.unitypackage");
                }
                
                // 确保导出目录存在
                string exportDir = Path.GetDirectoryName(exportPath);
                if (!Directory.Exists(exportDir))
                {
                    Directory.CreateDirectory(exportDir);
                }
                
                // 获取要导出的资源路径
                string[] pathsToExport;
                if (assetPaths != null && assetPaths.Length > 0)
                {
                    // 验证路径是否存在
                    List<string> validPaths = new List<string>();
                    foreach (string path in assetPaths)
                    {
                        if (AssetDatabase.IsValidFolder(path) || File.Exists(path))
                        {
                            validPaths.Add(path);
                        }
                        else
                        {
                            McpLogger.LogWarning($"资源路径不存在: {path}");
                        }
                    }
                    pathsToExport = validPaths.ToArray();
                }
                else
                {
                    // 导出整个Assets文件夹
                    pathsToExport = new string[] { "Assets" };
                }
                
                if (pathsToExport.Length == 0)
                {
                    return "{\"success\": false, \"message\": \"没有找到有效的资源路径进行导出\"}";
                }
                
                // 设置导出选项
                ExportPackageOptions exportOptions = ExportPackageOptions.Default;
                if (includeLibraryAssets) exportOptions |= ExportPackageOptions.IncludeLibraryAssets;
                if (includeDependencies) exportOptions |= ExportPackageOptions.IncludeDependencies;
                if (interactive) exportOptions |= ExportPackageOptions.Interactive;
                else exportOptions |= ExportPackageOptions.Recurse;
                
                // 执行导出
                McpLogger.LogTool($"开始导出Unity包: {packageName}");
                
                if (interactive)
                {
                    // 交互式导出
                    AssetDatabase.ExportPackage(pathsToExport, exportPath, exportOptions);
                }
                else
                {
                    // 非交互式导出
                    AssetDatabase.ExportPackage(pathsToExport, exportPath, exportOptions);
                }
                
                // 检查导出结果
                if (File.Exists(exportPath))
                {
                    FileInfo packageFile = new FileInfo(exportPath);
                    
                    var exportInfo = new
                    {
                        success = true,
                        message = "Unity包导出成功",
                        packageName = packageName,
                        exportPath = exportPath,
                        packageSize = FormatFileSize(packageFile.Length),
                        assetCount = pathsToExport.Length,
                        exportedPaths = pathsToExport
                    };
                    
                    McpLogger.LogTool($"Unity包导出成功: {exportPath}");
                    return JsonConvert.SerializeObject(exportInfo);
                }
                else
                {
                    return "{\"success\": false, \"message\": \"Unity包导出失败，文件未生成\"}";
                }
#else
                return "{\"success\": false, \"message\": \"此功能仅在编辑器模式下可用\"}";
#endif
            }
            catch (Exception ex)
            {
                McpLogger.LogException(ex, "导出Unity包时发生错误");
                return $"{{\"success\": false, \"message\": \"导出Unity包失败: {ex.Message}\"}}";
            }
        }
        
        /// <summary>
        /// 获取构建信息
        /// </summary>
        /// <param name="parameters">查询参数JSON</param>
        /// <returns>操作结果</returns>
        public static string GetBuildInfo(string parameters)
        {
            try
            {
#if UNITY_EDITOR
                var buildInfo = new
                {
                    success = true,
                    playerSettings = new
                    {
                        productName = PlayerSettings.productName,
                        companyName = PlayerSettings.companyName,
                        version = PlayerSettings.bundleVersion,
                        bundleIdentifier = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Standalone),
                        defaultScreenWidth = PlayerSettings.defaultScreenWidth,
                        defaultScreenHeight = PlayerSettings.defaultScreenHeight,
                        fullScreenMode = PlayerSettings.fullScreenMode.ToString(),
                        resizableWindow = PlayerSettings.resizableWindow
                    },
                    buildSettings = new
                    {
                        scenes = EditorBuildSettings.scenes.Select(scene => new
                        {
                            path = scene.path,
                            enabled = scene.enabled
                        }).ToArray(),
                        activeBuildTarget = EditorUserBuildSettings.activeBuildTarget.ToString(),
                        developmentBuild = EditorUserBuildSettings.development,
                        allowDebugging = EditorUserBuildSettings.allowDebugging,
                        connectProfiler = EditorUserBuildSettings.connectProfiler
                    },
                    qualitySettings = new
                    {
                        currentQualityLevel = QualitySettings.GetQualityLevel(),
                        qualityLevelNames = QualitySettings.names
                    },
                    scriptingSettings = new
                    {
                        scriptingBackend = PlayerSettings.GetScriptingBackend(BuildTargetGroup.Standalone).ToString(),
                        apiCompatibilityLevel = PlayerSettings.GetApiCompatibilityLevel(BuildTargetGroup.Standalone).ToString()
                    }
                };
                
                return JsonConvert.SerializeObject(buildInfo);
#else
                return "{\"success\": false, \"message\": \"此功能仅在编辑器模式下可用\"}";
#endif
            }
            catch (Exception ex)
            {
                McpLogger.LogException(ex, "获取构建信息时发生错误");
                return $"{{\"success\": false, \"message\": \"获取构建信息失败: {ex.Message}\"}}";
            }
        }
        
        #endregion
        
        #region Private Helper Methods
        
#if UNITY_EDITOR
        private static BuildTarget GetBuildTarget(string platformName)
        {
            switch (platformName.ToLower())
            {
                case "standalonewindows":
                case "windows":
                    return BuildTarget.StandaloneWindows;
                case "standalonewindows64":
                case "windows64":
                    return BuildTarget.StandaloneWindows64;
                case "standaloneosx":
                case "macos":
                case "osx":
                    return BuildTarget.StandaloneOSX;
                case "standalonelinux64":
                case "linux":
                case "linux64":
                    return BuildTarget.StandaloneLinux64;
                case "android":
                    return BuildTarget.Android;
                case "ios":
                    return BuildTarget.iOS;
                case "webgl":
                    return BuildTarget.WebGL;
                case "ps4":
                    return BuildTarget.PS4;
                case "xboxone":
                    return BuildTarget.XboxOne;
                // Nintendo3DS platform has been deprecated by Unity
                // case "nintendo3ds":
                //     return BuildTarget.Nintendo3DS;
                case "switch":
                case "nintendoswitch":
                    return BuildTarget.Switch;
                default:
                    return BuildTarget.NoTarget;
            }
        }
        
        private static string GetExecutableExtension(BuildTarget buildTarget)
        {
            switch (buildTarget)
            {
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                    return ".exe";
                case BuildTarget.StandaloneOSX:
                    return ".app";
                case BuildTarget.StandaloneLinux64:
                    return "";
                case BuildTarget.Android:
                    return ".apk";
                case BuildTarget.WebGL:
                    return "";
                default:
                    return "";
            }
        }
        
        private static string GetBuildSize(string buildPath)
        {
            try
            {
                if (File.Exists(buildPath))
                {
                    FileInfo fileInfo = new FileInfo(buildPath);
                    return FormatFileSize(fileInfo.Length);
                }
                else if (Directory.Exists(buildPath))
                {
                    DirectoryInfo dirInfo = new DirectoryInfo(buildPath);
                    long totalSize = GetDirectorySize(dirInfo);
                    return FormatFileSize(totalSize);
                }
                return "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }
        
        private static long GetDirectorySize(DirectoryInfo directory)
        {
            long size = 0;
            
            // 计算文件大小
            FileInfo[] files = directory.GetFiles();
            foreach (FileInfo file in files)
            {
                size += file.Length;
            }
            
            // 递归计算子目录大小
            DirectoryInfo[] subdirs = directory.GetDirectories();
            foreach (DirectoryInfo subdir in subdirs)
            {
                size += GetDirectorySize(subdir);
            }
            
            return size;
        }
#endif
        
        private static string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            
            return $"{len:0.##} {sizes[order]}";
        }
        
        #endregion
    }
}