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
    }
}