using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Text;
using Unity.MCP;

namespace Unity.MCP.Tools.Editor
{
    public static class UnityInputTools
    {
        /// <summary>
        /// 设置输入动作
        /// </summary>
        public static McpToolResult SetupInputActions(string inputActionsJson)
        {
            try
            {
#if UNITY_INPUT_SYSTEM
                var inputActions = JsonUtility.FromJson<InputActionSetup>(inputActionsJson);
                
                // 创建输入动作资产
                var inputActionAsset = ScriptableObject.CreateInstance<UnityEngine.InputSystem.InputActionAsset>();
                
                foreach (var actionMap in inputActions.actionMaps)
                {
                    var map = inputActionAsset.AddActionMap(actionMap.name);
                    
                    foreach (var action in actionMap.actions)
                    {
                        var inputAction = map.AddAction(action.name, action.type);
                        
                        foreach (var binding in action.bindings)
                        {
                            inputAction.AddBinding(binding.path, binding.interactions, binding.processors);
                        }
                    }
                }
                
                // 保存输入动作资产
                string assetPath = "Assets/InputActions.inputactions";
                AssetDatabase.CreateAsset(inputActionAsset, assetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                
                return new McpToolResult
                {
                    IsError = false,
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Input actions setup completed. Asset saved at: {assetPath}" + " Data: " + JsonConvert.SerializeObject(new {  assetPath = assetPath, actionMapsCount = inputActions.actionMaps.Length  })
                    }
                }
                };
#else
                return new McpToolResult
                {
                    IsError = true,
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = "Input System package is not installed. Please install it from Package Manager." }
                    }
                };
#endif
            }
            catch (System.Exception ex)
            {
                return new McpToolResult
                {
                    IsError = true,
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Failed to setup input actions: {ex.Message}" }
                    }
                };
            }
        }
        
        /// <summary>
        /// 绑定输入事件
        /// </summary>
        public static McpToolResult BindInputEvents(string gameObjectName, string inputEventBindings)
        {
            try
            {
                GameObject targetObj = GameObject.Find(gameObjectName);
                if (targetObj == null)
                {
                    return new McpToolResult
                {
                    IsError = true,
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"GameObject '{gameObjectName}' not found" }
                    }
                };
                }
                
                var bindings = JsonUtility.FromJson<InputEventBindings>(inputEventBindings);
                
                // 添加或获取输入处理组件
                var inputHandler = targetObj.GetComponent<MonoBehaviour>();
                if (inputHandler == null)
                {
                    // 创建一个简单的输入处理脚本
                    string scriptContent = GenerateInputHandlerScript(bindings);
                    string scriptPath = "Assets/Scripts/InputHandler.cs";
                    
                    System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(scriptPath));
                    System.IO.File.WriteAllText(scriptPath, scriptContent);
                    
                    AssetDatabase.Refresh();
                    
                    return new McpToolResult
                {
                    IsError = false,
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Input handler script created at: {scriptPath}. Please attach it to '{gameObjectName}' manually." + " Data: " + JsonConvert.SerializeObject(new {  scriptPath = scriptPath, gameObjectName = gameObjectName  })
                    }
                }
                    };
                }
                
                return new McpToolResult
                {
                    IsError = false,
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Input events binding setup for '{gameObjectName}'" + " Data: " + JsonConvert.SerializeObject(new {  gameObjectName = gameObjectName, bindingsCount = bindings.bindings?.Length ?? 0  })
                    }
                }
                };
            }
            catch (System.Exception ex)
            {
                return new McpToolResult
                {
                    IsError = true,
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Failed to bind input events: {ex.Message}" }
                    }
                };
            }
        }
        
        /// <summary>
        /// 模拟输入
        /// </summary>
        public static McpToolResult SimulateInput(string inputType, string inputData)
        {
            try
            {
#if UNITY_INPUT_SYSTEM
                var inputSystem = UnityEngine.InputSystem.InputSystem.s_Manager;
                
                switch (inputType.ToLower())
                {
                    case "key":
                        var keyData = JsonUtility.FromJson<KeyInputData>(inputData);
                        var keyboard = UnityEngine.InputSystem.Keyboard.current;
                        if (keyboard != null)
                        {
                            var key = (UnityEngine.InputSystem.Key)System.Enum.Parse(typeof(UnityEngine.InputSystem.Key), keyData.keyCode);
                            
                            if (keyData.pressed)
                            {
                                UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState(key));
                            }
                            else
                            {
                                UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState());
                            }
                        }
                        break;
                        
                    case "mouse":
                        var mouseData = JsonUtility.FromJson<MouseInputData>(inputData);
                        var mouse = UnityEngine.InputSystem.Mouse.current;
                        if (mouse != null)
                        {
                            var mouseState = new UnityEngine.InputSystem.LowLevel.MouseState
                            {
                                position = new Vector2(mouseData.x, mouseData.y),
                                buttons = mouseData.leftButton ? 1u : 0u
                            };
                            UnityEngine.InputSystem.InputSystem.QueueStateEvent(mouse, mouseState);
                        }
                        break;
                        
                    default:
                        return new McpToolResult
                {
                    IsError = true,
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Unsupported input type: {inputType}" }
                    }
                };
                }
                
                UnityEngine.InputSystem.InputSystem.Update();
                
                return new McpToolResult
                {
                    IsError = false,
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Input simulation completed for type: {inputType}" + " Data: " + JsonConvert.SerializeObject(new {  inputType = inputType, inputData = inputData  })
                    }
                }
                };
#else
                // 使用传统输入系统模拟
                switch (inputType.ToLower())
                {
                    case "key":
                        var keyData = JsonUtility.FromJson<KeyInputData>(inputData);
                        Debug.Log($"Simulating key input: {keyData.keyCode} pressed: {keyData.pressed}");
                        break;
                        
                    case "mouse":
                        var mouseData = JsonUtility.FromJson<MouseInputData>(inputData);
                        Debug.Log($"Simulating mouse input at ({mouseData.x}, {mouseData.y}) left button: {mouseData.leftButton}");
                        break;
                }
                
                return new McpToolResult
                {
                    IsError = false,
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Input simulation logged for type: {inputType} (Legacy Input System)" + " Data: " + JsonConvert.SerializeObject(new {  inputType = inputType, inputData = inputData  })
                    }
                }
                };
#endif
            }
            catch (System.Exception ex)
            {
                return new McpToolResult
                {
                    IsError = true,
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Failed to simulate input: {ex.Message}" }
                    }
                };
            }
        }
        
        /// <summary>
        /// 创建输入映射
        /// </summary>
        public static McpToolResult CreateInputMapping(string mappingName, string inputMappingData)
        {
            try
            {
                var mappingData = JsonUtility.FromJson<InputMappingData>(inputMappingData);
                
                // 创建输入映射配置文件
                string configPath = $"Assets/InputMappings/{mappingName}.json";
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(configPath));
                System.IO.File.WriteAllText(configPath, inputMappingData);
                
                AssetDatabase.Refresh();
                
                return new McpToolResult
                {
                    IsError = false,
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Input mapping '{mappingName}' created at: {configPath}" + " Data: " + JsonConvert.SerializeObject(new {  mappingName = mappingName, configPath = configPath, actionsCount = mappingData.actions?.Length ?? 0  })
                    }
                }
                };
            }
            catch (System.Exception ex)
            {
                return new McpToolResult
                {
                    IsError = true,
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Failed to create input mapping: {ex.Message}" }
                    }
                };
            }
        }
        
        /// <summary>
        /// 生成输入处理脚本
        /// </summary>
        private static string GenerateInputHandlerScript(InputEventBindings bindings)
        {
            var script = new StringBuilder();
            script.AppendLine("using UnityEngine;");
            script.AppendLine("");
            script.AppendLine("public class InputHandler : MonoBehaviour");
            script.AppendLine("{");
            script.AppendLine("    void Update()");
            script.AppendLine("    {");
            
            if (bindings.bindings != null)
            {
                foreach (var binding in bindings.bindings)
                {
                    switch (binding.eventType?.ToLower())
                    {
                        case "keydown":
                            script.AppendLine($"        if (Input.GetKeyDown(KeyCode.{binding.inputName}))");
                            script.AppendLine("        {");
                            script.AppendLine($"            {binding.methodName}();");
                            script.AppendLine("        }");
                            break;
                        case "keyup":
                            script.AppendLine($"        if (Input.GetKeyUp(KeyCode.{binding.inputName}))");
                            script.AppendLine("        {");
                            script.AppendLine($"            {binding.methodName}();");
                            script.AppendLine("        }");
                            break;
                        case "mousedown":
                            script.AppendLine($"        if (Input.GetMouseButtonDown({binding.inputName}))");
                            script.AppendLine("        {");
                            script.AppendLine($"            {binding.methodName}();");
                            script.AppendLine("        }");
                            break;
                    }
                }
            }
            
            script.AppendLine("    }");
            
            // 添加方法声明
            if (bindings.bindings != null)
            {
                script.AppendLine("");
                foreach (var binding in bindings.bindings)
                {
                    script.AppendLine($"    public void {binding.methodName}()");
                    script.AppendLine("    {");
                    script.AppendLine($"        Debug.Log(\"{binding.methodName} called\");");
                    script.AppendLine("        // Add your custom logic here");
                    script.AppendLine("    }");
                }
            }
            
            script.AppendLine("}");
            
            return script.ToString();
        }
    }
    
    #region Input System Helper Classes
    
    [System.Serializable]
    public class InputActionSetup
    {
        public InputActionMap[] actionMaps;
    }
    
    [System.Serializable]
    public class InputActionMap
    {
        public string name;
        public InputAction[] actions;
    }
    
    [System.Serializable]
    public class InputAction
    {
        public string name;
        public string type;
        public InputBinding[] bindings;
    }
    
    [System.Serializable]
    public class InputBinding
    {
        public string path;
        public string interactions;
        public string processors;
    }
    
    [System.Serializable]
    public class InputEventBindings
    {
        public InputEventBinding[] bindings;
    }
    
    [System.Serializable]
    public class InputEventBinding
    {
        public string inputName;
        public string methodName;
        public string eventType;
    }
    
    [System.Serializable]
    public class KeyInputData
    {
        public string keyCode;
        public bool pressed;
    }
    
    [System.Serializable]
    public class MouseInputData
    {
        public float x;
        public float y;
        public bool leftButton;
        public bool rightButton;
    }
    
    [System.Serializable]
    public class InputMappingData
    {
        public string name;
        public InputMappingAction[] actions;
    }
    
    [System.Serializable]
    public class InputMappingAction
    {
        public string name;
        public string key;
        public string action;
    }
    
    #endregion
}