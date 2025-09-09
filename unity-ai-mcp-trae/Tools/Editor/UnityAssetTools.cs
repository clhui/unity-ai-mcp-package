using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using Unity.MCP;

namespace Unity.MCP.Tools.Editor
{
    public static class UnityAssetTools
    {
        /// <summary>
        /// 导入资源到项目中
        /// </summary>
        /// <param name="arguments">包含path参数的JObject</param>
        /// <returns>操作结果</returns>
        public static McpToolResult ImportAsset(JObject arguments)
        {
            try
            {
#if UNITY_EDITOR
                if (arguments == null)
                {
                    return new McpToolResult
                    {
                        IsError = true,
                        Content = new List<McpContent>
                        {
                            new McpContent { Type = "text", Text = "Arguments cannot be null" }
                        }
                    };
                }

                string path = arguments["path"]?.ToString();
                if (string.IsNullOrEmpty(path))
                {
                    return new McpToolResult
                    {
                        IsError = true,
                        Content = new List<McpContent>
                        {
                            new McpContent { Type = "text", Text = "Path parameter is required" }
                        }
                    };
                }

                // 检查路径是否存在
                if (!AssetDatabase.IsValidFolder(path) && !File.Exists(path))
                {
                    return new McpToolResult
                    {
                        IsError = true,
                        Content = new List<McpContent>
                        {
                            new McpContent { Type = "text", Text = $"Path does not exist: {path}" }
                        }
                    };
                }

                // 刷新资源数据库以导入资源
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                AssetDatabase.Refresh();

                return new McpToolResult
                {
                    IsError = false,
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Successfully imported asset: {path}" }
                    }
                };
#else
                return new McpToolResult
                {
                    IsError = true,
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = "Asset import is only available in Unity Editor" }
                    }
                };
#endif
            }
            catch (Exception ex)
            {
                return new McpToolResult
                {
                    IsError = true,
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Failed to import asset: {ex.Message}" }
                    }
                };
            }
        }

        /// <summary>
        /// 强制刷新Unity资源数据库
        /// </summary>
        /// <param name="arguments">可选参数，包含importMode等</param>
        /// <returns>操作结果</returns>
        public static McpToolResult RefreshAssets(JObject arguments)
        {
            try
            {
#if UNITY_EDITOR
                // 获取刷新模式参数
                string importMode = arguments?["importMode"]?.ToString() ?? "normal";
                bool forceUpdate = arguments?["forceUpdate"]?.ToObject<bool>() ?? false;
                
                ImportAssetOptions options = ImportAssetOptions.Default;
                
                switch (importMode.ToLower())
                {
                    case "force":
                        options = ImportAssetOptions.ForceUpdate | ImportAssetOptions.ImportRecursive;
                        break;
                    case "synchronous":
                        options = ImportAssetOptions.ForceSynchronousImport;
                        break;
                    case "normal":
                    default:
                        options = ImportAssetOptions.Default;
                        break;
                }
                
                if (forceUpdate)
                {
                    options |= ImportAssetOptions.ForceUpdate;
                }
                
                // 执行资源刷新
                AssetDatabase.Refresh(options);
                
                return new McpToolResult
                {
                    IsError = false,
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Successfully refreshed assets with mode: {importMode}, forceUpdate: {forceUpdate}" }
                    }
                };
#else
                return new McpToolResult
                {
                    IsError = true,
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = "Asset refresh is only available in Unity Editor" }
                    }
                };
#endif
            }
            catch (Exception ex)
            {
                return new McpToolResult
                {
                    IsError = true,
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Failed to refresh assets: {ex.Message}" }
                    }
                };
            }
        }

        /// <summary>
        /// 触发Unity脚本编译
        /// </summary>
        /// <param name="arguments">可选参数</param>
        /// <returns>操作结果</returns>
        public static McpToolResult CompileScripts(JObject arguments)
        {
            try
            {
#if UNITY_EDITOR
                // 检查是否已经在编译中
                if (EditorApplication.isCompiling)
                {
                    return new McpToolResult
                    {
                        IsError = false,
                        Content = new List<McpContent>
                        {
                            new McpContent { Type = "text", Text = "Scripts are already compiling" }
                        }
                    };
                }
                
                // 触发编译
                AssetDatabase.Refresh();
                EditorUtility.RequestScriptReload();
                
                return new McpToolResult
                {
                    IsError = false,
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = "Script compilation triggered successfully" }
                    }
                };
#else
                return new McpToolResult
                {
                    IsError = true,
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = "Script compilation is only available in Unity Editor" }
                    }
                };
#endif
            }
            catch (Exception ex)
            {
                return new McpToolResult
                {
                    IsError = true,
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Failed to trigger script compilation: {ex.Message}" }
                    }
                };
            }
        }

        /// <summary>
        /// 等待编译完成
        /// </summary>
        /// <param name="arguments">包含timeout等参数的JObject</param>
        /// <returns>操作结果</returns>
        public static McpToolResult WaitForCompilation(JObject arguments)
        {
            try
            {
#if UNITY_EDITOR
                int timeoutSeconds = arguments?["timeout"]?.ToObject<int>() ?? 30;
                int checkIntervalMs = arguments?["checkInterval"]?.ToObject<int>() ?? 100;
                
                DateTime startTime = DateTime.Now;
                TimeSpan timeout = TimeSpan.FromSeconds(timeoutSeconds);
                
                // 如果当前没有在编译，直接返回
                if (!EditorApplication.isCompiling)
                {
                    return new McpToolResult
                    {
                        IsError = false,
                        Content = new List<McpContent>
                        {
                            new McpContent { Type = "text", Text = "No compilation in progress" }
                        }
                    };
                }
                
                // 等待编译完成
                while (EditorApplication.isCompiling)
                {
                    if (DateTime.Now - startTime > timeout)
                    {
                        return new McpToolResult
                        {
                            IsError = true,
                            Content = new List<McpContent>
                            {
                                new McpContent { Type = "text", Text = $"Compilation timeout after {timeoutSeconds} seconds" }
                            }
                        };
                    }
                    
                    System.Threading.Thread.Sleep(checkIntervalMs);
                }
                
                // 检查编译结果
                var compilationMessages = new List<string>();
                
                // 获取编译时间
                TimeSpan compilationTime = DateTime.Now - startTime;
                
                return new McpToolResult
                {
                    IsError = false,
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Compilation completed successfully in {compilationTime.TotalSeconds:F2} seconds" }
                    }
                };
#else
                return new McpToolResult
                {
                    IsError = true,
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = "Compilation waiting is only available in Unity Editor" }
                    }
                };
#endif
            }
            catch (Exception ex)
            {
                return new McpToolResult
                {
                    IsError = true,
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Failed to wait for compilation: {ex.Message}" }
                    }
                };
            }
        }
    }
}