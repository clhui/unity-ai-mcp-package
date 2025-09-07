using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json.Linq;
using Unity.MCP;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Unity.MCP.Tools.Editor
{
    /// <summary>
    /// Unity脚本管理工具
    /// </summary>
    public static class UnityScriptTools
    {
        public static McpToolResult CreateScript(string scriptName, string scriptType, string namespaceName = null, string baseClass = null, string savePath = null)
        {
            var arguments = new JObject();
            arguments["name"] = scriptName;
            arguments["type"] = scriptType;
            if (!string.IsNullOrEmpty(namespaceName))
                arguments["namespace"] = namespaceName;
            if (!string.IsNullOrEmpty(baseClass))
                arguments["baseClass"] = baseClass;
            if (!string.IsNullOrEmpty(savePath))
                arguments["path"] = savePath;
            
            return CreateScript(arguments);
        }
        
        public static McpToolResult CreateScript(JObject arguments)
        {
#if UNITY_EDITOR
            try
            {
                string scriptName = arguments["name"]?.ToString();
                string scriptPath = arguments["path"]?.ToString();
                string scriptContent = arguments["content"]?.ToString();
                string scriptType = arguments["type"]?.ToString() ?? "MonoBehaviour";
                
                if (string.IsNullOrEmpty(scriptName))
                {
                    return new McpToolResult
                    {
                        Content = new List<McpContent>
                        {
                            new McpContent { Type = "text", Text = "Script name is required" }
                        },
                        IsError = true
                    };
                }
                
                // 如果没有指定路径，使用默认的Scripts文件夹
                if (string.IsNullOrEmpty(scriptPath))
                {
                    scriptPath = "Assets/Scripts";
                }
                
                // 确保Scripts文件夹存在
                if (!AssetDatabase.IsValidFolder(scriptPath))
                {
                    string[] pathParts = scriptPath.Split('/');
                    string currentPath = pathParts[0];
                    for (int i = 1; i < pathParts.Length; i++)
                    {
                        string newPath = currentPath + "/" + pathParts[i];
                        if (!AssetDatabase.IsValidFolder(newPath))
                        {
                            AssetDatabase.CreateFolder(currentPath, pathParts[i]);
                        }
                        currentPath = newPath;
                    }
                }
                
                string fullPath = $"{scriptPath}/{scriptName}.cs";
                
                // 检查文件是否已存在
                if (File.Exists(fullPath))
                {
                    return new McpToolResult
                    {
                        Content = new List<McpContent>
                        {
                            new McpContent { Type = "text", Text = $"Script already exists: {fullPath}" }
                        },
                        IsError = true
                    };
                }
                
                // 如果没有提供内容，生成默认模板
                if (string.IsNullOrEmpty(scriptContent))
                {
                    scriptContent = GenerateScriptTemplate(scriptName, scriptType);
                }
                
                // 创建脚本文件
                File.WriteAllText(fullPath, scriptContent);
                AssetDatabase.Refresh();
                
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Script created successfully: {fullPath}" }
                    }
                };
            }
            catch (Exception ex)
            {
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Failed to create script: {ex.Message}" }
                    },
                    IsError = true
                };
            }
#else
            return new McpToolResult
            {
                Content = new List<McpContent>
                {
                    new McpContent { Type = "text", Text = "Script creation is only available in Unity Editor" }
                },
                IsError = true
            };
#endif
        }
        
        public static McpToolResult ModifyScript(JObject arguments)
        {
#if UNITY_EDITOR
            try
            {
                string scriptPath = arguments["path"]?.ToString();
                string newContent = arguments["content"]?.ToString();
                
                if (string.IsNullOrEmpty(scriptPath))
                {
                    return new McpToolResult
                    {
                        Content = new List<McpContent>
                        {
                            new McpContent { Type = "text", Text = "Script path is required" }
                        },
                        IsError = true
                    };
                }
                
                if (string.IsNullOrEmpty(newContent))
                {
                    return new McpToolResult
                    {
                        Content = new List<McpContent>
                        {
                            new McpContent { Type = "text", Text = "Script content is required" }
                        },
                        IsError = true
                    };
                }
                
                // 检查文件是否存在
                if (!File.Exists(scriptPath))
                {
                    return new McpToolResult
                    {
                        Content = new List<McpContent>
                        {
                            new McpContent { Type = "text", Text = $"Script not found: {scriptPath}" }
                        },
                        IsError = true
                    };
                }
                
                // 备份原文件
                string backupPath = scriptPath + ".backup";
                File.Copy(scriptPath, backupPath, true);
                
                // 写入新内容
                File.WriteAllText(scriptPath, newContent);
                AssetDatabase.Refresh();
                
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Script modified successfully: {scriptPath}\nBackup created: {backupPath}" }
                    }
                };
            }
            catch (Exception ex)
            {
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Failed to modify script: {ex.Message}" }
                    },
                    IsError = true
                };
            }
#else
            return new McpToolResult
            {
                Content = new List<McpContent>
                {
                    new McpContent { Type = "text", Text = "Script modification is only available in Unity Editor" }
                },
                IsError = true
            };
#endif
        }
        
        public static McpToolResult CompileScripts(JObject arguments)
        {
#if UNITY_EDITOR
            try
            {
                // 强制重新编译所有脚本
                AssetDatabase.Refresh();
                
                // 等待编译完成
                int timeout = 30; // 30秒超时
                int elapsed = 0;
                
                while (EditorApplication.isCompiling && elapsed < timeout)
                {
                    System.Threading.Thread.Sleep(1000);
                    elapsed++;
                }
                
                if (EditorApplication.isCompiling)
                {
                    return new McpToolResult
                    {
                        Content = new List<McpContent>
                        {
                            new McpContent { Type = "text", Text = "Compilation timeout after 30 seconds" }
                        },
                        IsError = true
                    };
                }
                
                // 检查编译错误
                var errors = GetCompilationErrors();
                
                if (errors.Count > 0)
                {
                    return new McpToolResult
                    {
                        Content = new List<McpContent>
                        {
                            new McpContent { Type = "text", Text = $"Compilation completed with {errors.Count} errors:\n" + string.Join("\n", errors) }
                        },
                        IsError = true
                    };
                }
                
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = "Scripts compiled successfully with no errors" }
                    }
                };
            }
            catch (Exception ex)
            {
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Failed to compile scripts: {ex.Message}" }
                    },
                    IsError = true
                };
            }
#else
            return new McpToolResult
            {
                Content = new List<McpContent>
                {
                    new McpContent { Type = "text", Text = "Script compilation is only available in Unity Editor" }
                },
                IsError = true
            };
#endif
        }
        
        public static McpToolResult GetScriptErrors(JObject arguments)
        {
#if UNITY_EDITOR
            try
            {
                var errors = GetCompilationErrors();
                var warnings = GetCompilationWarnings();
                
                var result = new System.Text.StringBuilder();
                result.AppendLine($"Compilation Status:");
                result.AppendLine($"- Is Compiling: {EditorApplication.isCompiling}");
                result.AppendLine($"- Errors: {errors.Count}");
                result.AppendLine($"- Warnings: {warnings.Count}");
                result.AppendLine();
                
                if (errors.Count > 0)
                {
                    result.AppendLine("🔴 ERRORS:");
                    foreach (var error in errors)
                    {
                        result.AppendLine($"  {error}");
                    }
                    result.AppendLine();
                }
                
                if (warnings.Count > 0)
                {
                    result.AppendLine("🟡 WARNINGS:");
                    foreach (var warning in warnings)
                    {
                        result.AppendLine($"  {warning}");
                    }
                    result.AppendLine();
                }
                
                if (errors.Count == 0 && warnings.Count == 0)
                {
                    result.AppendLine("✅ No compilation errors or warnings found");
                }
                
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = result.ToString() }
                    }
                };
            }
            catch (Exception ex)
            {
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Failed to get script errors: {ex.Message}" }
                    },
                    IsError = true
                };
            }
#else
            return new McpToolResult
            {
                Content = new List<McpContent>
                {
                    new McpContent { Type = "text", Text = "Script error checking is only available in Unity Editor" }
                },
                IsError = true
            };
#endif
        }
        
        // 辅助方法：生成脚本模板
        private static string GenerateScriptTemplate(string scriptName, string scriptType)
        {
            switch (scriptType.ToLower())
            {
                case "monobehaviour":
                    return $@"using UnityEngine;

public class {scriptName} : MonoBehaviour
{{
    void Start()
    {{
        
    }}
    
    void Update()
    {{
        
    }}
}}";
                    
                case "scriptableobject":
                    return $@"using UnityEngine;

[CreateAssetMenu(fileName = ""New {scriptName}"", menuName = ""{scriptName}"")]
public class {scriptName} : ScriptableObject
{{
    
}}";
                    
                case "editor":
                    return $@"using UnityEngine;
using UnityEditor;

[CustomEditor(typeof({scriptName}))]
public class {scriptName}Editor : Editor
{{
    public override void OnInspectorGUI()
    {{
        DrawDefaultInspector();
    }}
}}";
                    
                case "interface":
                    return $@"using UnityEngine;

public interface I{scriptName}
{{
    
}}";
                    
                default:
                    return $@"using UnityEngine;

public class {scriptName}
{{
    
}}";
            }
        }
        
#if UNITY_EDITOR
        // 获取编译错误
        private static List<string> GetCompilationErrors()
        {
            var errors = new List<string>();
            
            try
            {
                // 使用反射获取编译错误信息
                var consoleWindowType = typeof(EditorWindow).Assembly.GetType("UnityEditor.ConsoleWindow");
                if (consoleWindowType != null)
                {
                    var getCountsByTypeMethod = consoleWindowType.GetMethod("GetCountsByType", 
                        System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                    
                    if (getCountsByTypeMethod != null)
                    {
                        var result = getCountsByTypeMethod.Invoke(null, null) as int[];
                        if (result != null && result.Length > 0)
                        {
                            // result[0] = info count, result[1] = warning count, result[2] = error count
                            if (result.Length > 2 && result[2] > 0)
                            {
                                errors.Add($"Found {result[2]} compilation errors. Check Console window for details.");
                            }
                        }
                    }
                }
            }
            catch
            {
                // 如果反射失败，返回空列表
            }
            
            return errors;
        }
        
        // 获取编译警告
        private static List<string> GetCompilationWarnings()
        {
            var warnings = new List<string>();
            
            try
            {
                // 使用反射获取编译警告信息
                var consoleWindowType = typeof(EditorWindow).Assembly.GetType("UnityEditor.ConsoleWindow");
                if (consoleWindowType != null)
                {
                    var getCountsByTypeMethod = consoleWindowType.GetMethod("GetCountsByType", 
                        System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                    
                    if (getCountsByTypeMethod != null)
                    {
                        var result = getCountsByTypeMethod.Invoke(null, null) as int[];
                        if (result != null && result.Length > 1)
                        {
                            // result[0] = info count, result[1] = warning count, result[2] = error count
                            if (result[1] > 0)
                            {
                                warnings.Add($"Found {result[1]} compilation warnings. Check Console window for details.");
                            }
                        }
                    }
                }
            }
            catch
            {
                // 如果反射失败，返回空列表
            }
            
            return warnings;
        }
#endif
    }
}