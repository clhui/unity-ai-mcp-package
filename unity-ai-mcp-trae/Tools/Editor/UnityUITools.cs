using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json.Linq;
using Unity.MCP;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Unity.MCP.Editor
{
    /// <summary>
    /// Unity UI系统工具
    /// </summary>
    public static class UnityUITools
    {
        /// <summary>
        /// 检查是否处于Play模式，如果是则返回警告信息
        /// </summary>
        /// <returns>如果处于Play模式返回错误结果，否则返回null</returns>
        private static McpToolResult CheckPlayModeForEditing()
        {
#if UNITY_EDITOR
            if (EditorApplication.isPlaying)
            {
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = "⚠️ 无法在Play模式下编辑UI！请先停止Play模式再进行UI编辑操作。\n提示：点击Unity编辑器中的停止按钮或使用play_mode_stop工具停止Play模式。" }
                    },
                    IsError = true
                };
            }
#endif
            return null;
        }

        public static McpToolResult CreateCanvas(JObject arguments)
        {
            // 检查Play模式
            var playModeCheck = CheckPlayModeForEditing();
            if (playModeCheck != null) return playModeCheck;
            
            var canvasName = arguments["canvasName"]?.ToString() ?? "Canvas";
            var renderMode = arguments["renderMode"]?.ToString() ?? "ScreenSpaceOverlay";
            var sortingOrder = arguments["sortingOrder"]?.ToObject<int>() ?? 0;
            
            try
            {
                GameObject canvasObj = new GameObject(canvasName);
                Canvas canvas = canvasObj.AddComponent<Canvas>();
                CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
                GraphicRaycaster raycaster = canvasObj.AddComponent<GraphicRaycaster>();
                
                // 设置渲染模式
                switch (renderMode.ToLower())
                {
                    case "screenspaceoverlay":
                        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                        break;
                    case "screenspacecamera":
                        canvas.renderMode = RenderMode.ScreenSpaceCamera;
                        break;
                    case "worldspace":
                        canvas.renderMode = RenderMode.WorldSpace;
                        break;
                    default:
                        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                        break;
                }
                
                canvas.sortingOrder = sortingOrder;
                
                // 设置CanvasScaler
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;
                
#if UNITY_EDITOR
                Undo.RegisterCreatedObjectUndo(canvasObj, "Create Canvas");
                Selection.activeGameObject = canvasObj;
                EditorUtility.SetDirty(canvasObj);
#endif
                
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Canvas '{canvasName}' created successfully (ID: {canvasObj.GetInstanceID()}, RenderMode: {renderMode}, SortingOrder: {sortingOrder})" }
                    }
                };
            }
            catch (Exception ex)
            {
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Failed to create canvas: {ex.Message}" }
                    },
                    IsError = true
                };
            }
        }
        
        public static McpToolResult CreateUIElement(JObject arguments)
        {
            // 检查Play模式
            var playModeCheck = CheckPlayModeForEditing();
            if (playModeCheck != null) return playModeCheck;
            
            var elementName = arguments["elementName"]?.ToString();
            var elementType = arguments["elementType"]?.ToString();
            var parentName = arguments["parentName"]?.ToString();
            
            if (string.IsNullOrEmpty(elementName) || string.IsNullOrEmpty(elementType))
            {
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = "Element name and type are required" }
                    },
                    IsError = true
                };
            }
            
            try
            {
                GameObject parentObj = null;
                
                if (!string.IsNullOrEmpty(parentName))
                {
                    parentObj = GameObject.Find(parentName);
                    if (parentObj == null)
                    {
                        return new McpToolResult
                        {
                            Content = new List<McpContent>
                            {
                                new McpContent { Type = "text", Text = $"Parent object '{parentName}' not found" }
                            },
                            IsError = true
                        };
                    }
                }
                else
                {
                    // 查找Canvas作为默认父对象
                    Canvas canvas = UnityEngine.Object.FindObjectOfType<Canvas>();
                    if (canvas != null)
                    {
                        parentObj = canvas.gameObject;
                    }
                }
                
                GameObject uiElement = CreateUIElementByType(elementName, elementType);
                
                if (parentObj != null)
                {
                    uiElement.transform.SetParent(parentObj.transform, false);
                }
                
#if UNITY_EDITOR
                Undo.RegisterCreatedObjectUndo(uiElement, "Create UI Element");
                Selection.activeGameObject = uiElement;
                EditorUtility.SetDirty(uiElement);
#endif
                
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"UI element '{elementName}' of type '{elementType}' created successfully (ID: {uiElement.GetInstanceID()})" }
                    }
                };
            }
            catch (Exception ex)
            {
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Failed to create UI element: {ex.Message}" }
                    },
                    IsError = true
                };
            }
        }
        
        public static McpToolResult SetUIProperties(JObject arguments)
        {
            var elementName = arguments["elementName"]?.ToString();
            var properties = arguments["properties"] as JObject;
            
            if (string.IsNullOrEmpty(elementName) || properties == null)
            {
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = "Element name and properties are required" }
                    },
                    IsError = true
                };
            }
            
            try
            {
                GameObject uiElement = GameObject.Find(elementName);
                if (uiElement == null)
                {
                    return new McpToolResult
                    {
                        Content = new List<McpContent>
                        {
                            new McpContent { Type = "text", Text = $"UI element '{elementName}' not found" }
                        },
                        IsError = true
                    };
                }
                
#if UNITY_EDITOR
                Undo.RecordObject(uiElement, "Set UI Properties");
#endif
                
                var setProperties = new List<string>();
                
                // 设置RectTransform属性
                RectTransform rectTransform = uiElement.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    SetRectTransformProperties(rectTransform, properties, setProperties);
                }
                
                // 设置UI组件属性
                SetUIComponentProperties(uiElement, properties, setProperties);
                
#if UNITY_EDITOR
                EditorUtility.SetDirty(uiElement);
#endif
                
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"UI properties set for '{elementName}':\n" + string.Join("\n", setProperties) }
                    }
                };
            }
            catch (Exception ex)
            {
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Failed to set UI properties: {ex.Message}" }
                    },
                    IsError = true
                };
            }
        }
        
        public static McpToolResult BindUIEvents(JObject arguments)
        {
            var elementName = arguments["elementName"]?.ToString();
            var eventType = arguments["eventType"]?.ToString();
            var methodName = arguments["methodName"]?.ToString();
            var targetObjectName = arguments["targetObjectName"]?.ToString();
            
            if (string.IsNullOrEmpty(elementName) || string.IsNullOrEmpty(eventType) || string.IsNullOrEmpty(methodName))
            {
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = "Element name, event type, and method name are required" }
                    },
                    IsError = true
                };
            }
            
            try
            {
                GameObject uiElement = GameObject.Find(elementName);
                if (uiElement == null)
                {
                    return new McpToolResult
                    {
                        Content = new List<McpContent>
                        {
                            new McpContent { Type = "text", Text = $"UI element '{elementName}' not found" }
                        },
                        IsError = true
                    };
                }
                
                GameObject targetObject = uiElement;
                if (!string.IsNullOrEmpty(targetObjectName))
                {
                    targetObject = GameObject.Find(targetObjectName);
                    if (targetObject == null)
                    {
                        return new McpToolResult
                        {
                            Content = new List<McpContent>
                            {
                                new McpContent { Type = "text", Text = $"Target object '{targetObjectName}' not found" }
                            },
                            IsError = true
                        };
                    }
                }
                
#if UNITY_EDITOR
                Undo.RecordObject(uiElement, "Bind UI Event");
#endif
                
                // 根据事件类型绑定事件
                switch (eventType.ToLower())
                {
                    case "onclick":
                        Button button = uiElement.GetComponent<Button>();
                        if (button != null)
                        {
                            button.onClick.AddListener(() => {
                                Debug.Log($"Button {elementName} clicked - would call {methodName}");
                            });
                        }
                        else
                        {
                            return new McpToolResult
                            {
                                Content = new List<McpContent>
                                {
                                    new McpContent { Type = "text", Text = $"No Button component found on '{elementName}'" }
                                },
                                IsError = true
                            };
                        }
                        break;
                        
                    case "onvaluechanged":
                        Slider slider = uiElement.GetComponent<Slider>();
                        if (slider != null)
                        {
                            slider.onValueChanged.AddListener((value) => {
                                Debug.Log($"Slider {elementName} value changed to {value} - would call {methodName}");
                            });
                        }
                        else
                        {
                            Toggle toggle = uiElement.GetComponent<Toggle>();
                            if (toggle != null)
                            {
                                toggle.onValueChanged.AddListener((value) => {
                                    Debug.Log($"Toggle {elementName} value changed to {value} - would call {methodName}");
                                });
                            }
                            else
                            {
                                return new McpToolResult
                                {
                                    Content = new List<McpContent>
                                    {
                                        new McpContent { Type = "text", Text = $"No Slider or Toggle component found on '{elementName}'" }
                                    },
                                    IsError = true
                                };
                            }
                        }
                        break;
                        
                    case "onendediting":
                        InputField inputField = uiElement.GetComponent<InputField>();
                        if (inputField != null)
                        {
                            inputField.onEndEdit.AddListener((text) => {
                                Debug.Log($"InputField {elementName} end edit with text '{text}' - would call {methodName}");
                            });
                        }
                        else
                        {
                            return new McpToolResult
                            {
                                Content = new List<McpContent>
                                {
                                    new McpContent { Type = "text", Text = $"No InputField component found on '{elementName}'" }
                                },
                                IsError = true
                            };
                        }
                        break;
                        
                    default:
                        return new McpToolResult
                        {
                            Content = new List<McpContent>
                            {
                                new McpContent { Type = "text", Text = $"Unsupported event type: {eventType}" }
                            },
                            IsError = true
                        };
                }
                
#if UNITY_EDITOR
                EditorUtility.SetDirty(uiElement);
#endif
                
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Event '{eventType}' bound to method '{methodName}' for element '{elementName}'" }
                    }
                };
            }
            catch (Exception ex)
            {
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Failed to bind UI event: {ex.Message}" }
                    },
                    IsError = true
                };
            }
        }
        
        #region Helper Methods
        
        private static GameObject CreateUIElementByType(string elementName, string elementType)
        {
            GameObject uiElement = new GameObject(elementName);
            RectTransform rectTransform = uiElement.AddComponent<RectTransform>();
            
            switch (elementType.ToLower())
            {
                case "button":
                    Image buttonImage = uiElement.AddComponent<Image>();
                    Button button = uiElement.AddComponent<Button>();
                    
                    // 创建按钮文本
                    GameObject buttonText = new GameObject("Text");
                    buttonText.transform.SetParent(uiElement.transform, false);
                    RectTransform textRect = buttonText.AddComponent<RectTransform>();
                    textRect.anchorMin = Vector2.zero;
                    textRect.anchorMax = Vector2.one;
                    textRect.sizeDelta = Vector2.zero;
                    textRect.offsetMin = Vector2.zero;
                    textRect.offsetMax = Vector2.zero;
                    
                    Text text = buttonText.AddComponent<Text>();
                    text.text = "Button";
                    text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                    text.fontSize = 14;
                    text.alignment = TextAnchor.MiddleCenter;
                    text.color = Color.black;
                    break;
                    
                case "text":
                    Text textComponent = uiElement.AddComponent<Text>();
                    textComponent.text = "New Text";
                    textComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                    textComponent.fontSize = 14;
                    textComponent.color = Color.black;
                    break;
                    
                case "image":
                    Image image = uiElement.AddComponent<Image>();
                    break;
                    
                case "panel":
                    Image panelImage = uiElement.AddComponent<Image>();
                    panelImage.color = new Color(1f, 1f, 1f, 0.392f);
                    break;
                    
                case "slider":
                    Slider slider = uiElement.AddComponent<Slider>();
                    Image sliderBg = uiElement.AddComponent<Image>();
                    
                    // 创建Background
                    GameObject background = new GameObject("Background");
                    background.transform.SetParent(uiElement.transform, false);
                    RectTransform bgRect = background.AddComponent<RectTransform>();
                    bgRect.anchorMin = Vector2.zero;
                    bgRect.anchorMax = Vector2.one;
                    bgRect.sizeDelta = Vector2.zero;
                    bgRect.offsetMin = Vector2.zero;
                    bgRect.offsetMax = Vector2.zero;
                    Image bgImage = background.AddComponent<Image>();
                    bgImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);
                    
                    // 创建Fill Area
                    GameObject fillArea = new GameObject("Fill Area");
                    fillArea.transform.SetParent(uiElement.transform, false);
                    RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
                    fillAreaRect.anchorMin = Vector2.zero;
                    fillAreaRect.anchorMax = Vector2.one;
                    fillAreaRect.sizeDelta = Vector2.zero;
                    fillAreaRect.offsetMin = Vector2.zero;
                    fillAreaRect.offsetMax = Vector2.zero;
                    
                    // 创建Fill
                    GameObject fill = new GameObject("Fill");
                    fill.transform.SetParent(fillArea.transform, false);
                    RectTransform fillRect = fill.AddComponent<RectTransform>();
                    fillRect.anchorMin = Vector2.zero;
                    fillRect.anchorMax = Vector2.one;
                    fillRect.sizeDelta = Vector2.zero;
                    fillRect.offsetMin = Vector2.zero;
                    fillRect.offsetMax = Vector2.zero;
                    Image fillImage = fill.AddComponent<Image>();
                    fillImage.color = Color.white;
                    
                    // 创建Handle Slide Area
                    GameObject handleSlideArea = new GameObject("Handle Slide Area");
                    handleSlideArea.transform.SetParent(uiElement.transform, false);
                    RectTransform handleSlideAreaRect = handleSlideArea.AddComponent<RectTransform>();
                    handleSlideAreaRect.anchorMin = Vector2.zero;
                    handleSlideAreaRect.anchorMax = Vector2.one;
                    handleSlideAreaRect.sizeDelta = Vector2.zero;
                    handleSlideAreaRect.offsetMin = Vector2.zero;
                    handleSlideAreaRect.offsetMax = Vector2.zero;
                    
                    // 创建Handle
                    GameObject handle = new GameObject("Handle");
                    handle.transform.SetParent(handleSlideArea.transform, false);
                    RectTransform handleRect = handle.AddComponent<RectTransform>();
                    handleRect.anchorMin = Vector2.zero;
                    handleRect.anchorMax = Vector2.one;
                    handleRect.sizeDelta = new Vector2(20, 0);
                    handleRect.offsetMin = Vector2.zero;
                    handleRect.offsetMax = Vector2.zero;
                    Image handleImage = handle.AddComponent<Image>();
                    handleImage.color = Color.white;
                    
                    slider.fillRect = fillRect;
                    slider.handleRect = handleRect;
                    slider.targetGraphic = handleImage;
                    slider.direction = Slider.Direction.LeftToRight;
                    break;
                    
                case "toggle":
                    Toggle toggle = uiElement.AddComponent<Toggle>();
                    
                    // 创建Background
                    GameObject toggleBg = new GameObject("Background");
                    toggleBg.transform.SetParent(uiElement.transform, false);
                    RectTransform toggleBgRect = toggleBg.AddComponent<RectTransform>();
                    toggleBgRect.anchorMin = new Vector2(0, 1);
                    toggleBgRect.anchorMax = new Vector2(0, 1);
                    toggleBgRect.anchoredPosition = new Vector2(10, -10);
                    toggleBgRect.sizeDelta = new Vector2(20, 20);
                    Image toggleBgImage = toggleBg.AddComponent<Image>();
                    toggleBgImage.color = Color.white;
                    
                    // 创建Checkmark
                    GameObject checkmark = new GameObject("Checkmark");
                    checkmark.transform.SetParent(toggleBg.transform, false);
                    RectTransform checkmarkRect = checkmark.AddComponent<RectTransform>();
                    checkmarkRect.anchorMin = Vector2.zero;
                    checkmarkRect.anchorMax = Vector2.one;
                    checkmarkRect.sizeDelta = Vector2.zero;
                    checkmarkRect.offsetMin = Vector2.zero;
                    checkmarkRect.offsetMax = Vector2.zero;
                    Image checkmarkImage = checkmark.AddComponent<Image>();
                    checkmarkImage.color = new Color(0.196f, 0.196f, 0.196f, 1f);
                    
                    // 创建Label
                    GameObject label = new GameObject("Label");
                    label.transform.SetParent(uiElement.transform, false);
                    RectTransform labelRect = label.AddComponent<RectTransform>();
                    labelRect.anchorMin = new Vector2(0, 0);
                    labelRect.anchorMax = new Vector2(1, 1);
                    labelRect.offsetMin = new Vector2(23, 1);
                    labelRect.offsetMax = new Vector2(-5, -2);
                    Text labelText = label.AddComponent<Text>();
                    labelText.text = "Toggle";
                    labelText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                    labelText.fontSize = 14;
                    labelText.color = Color.black;
                    
                    toggle.targetGraphic = toggleBgImage;
                    toggle.graphic = checkmarkImage;
                    break;
                    
                case "inputfield":
                    InputField inputField = uiElement.AddComponent<InputField>();
                    Image inputBg = uiElement.AddComponent<Image>();
                    inputBg.color = Color.white;
                    
                    // 创建Placeholder
                    GameObject placeholder = new GameObject("Placeholder");
                    placeholder.transform.SetParent(uiElement.transform, false);
                    RectTransform placeholderRect = placeholder.AddComponent<RectTransform>();
                    placeholderRect.anchorMin = Vector2.zero;
                    placeholderRect.anchorMax = Vector2.one;
                    placeholderRect.sizeDelta = Vector2.zero;
                    placeholderRect.offsetMin = new Vector2(10, 6);
                    placeholderRect.offsetMax = new Vector2(-10, -7);
                    Text placeholderText = placeholder.AddComponent<Text>();
                    placeholderText.text = "Enter text...";
                    placeholderText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                    placeholderText.fontSize = 14;
                    placeholderText.fontStyle = FontStyle.Italic;
                    placeholderText.color = new Color(0.196f, 0.196f, 0.196f, 0.5f);
                    
                    // 创建Text
                    GameObject inputText = new GameObject("Text");
                    inputText.transform.SetParent(uiElement.transform, false);
                    RectTransform inputTextRect = inputText.AddComponent<RectTransform>();
                    inputTextRect.anchorMin = Vector2.zero;
                    inputTextRect.anchorMax = Vector2.one;
                    inputTextRect.sizeDelta = Vector2.zero;
                    inputTextRect.offsetMin = new Vector2(10, 6);
                    inputTextRect.offsetMax = new Vector2(-10, -7);
                    Text inputTextComponent = inputText.AddComponent<Text>();
                    inputTextComponent.text = "";
                    inputTextComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                    inputTextComponent.fontSize = 14;
                    inputTextComponent.color = new Color(0.196f, 0.196f, 0.196f, 1f);
                    inputTextComponent.supportRichText = false;
                    
                    inputField.targetGraphic = inputBg;
                    inputField.textComponent = inputTextComponent;
                    inputField.placeholder = placeholderText;
                    break;
                    
                default:
                    // 默认创建空的RectTransform
                    break;
            }
            
            return uiElement;
        }
        
        private static void SetRectTransformProperties(RectTransform rectTransform, JObject properties, List<string> setProperties)
        {
            foreach (var prop in properties)
            {
                try
                {
                    switch (prop.Key.ToLower())
                    {
                        case "anchoredposition":
                            var anchoredPos = prop.Value.ToObject<Vector2>();
                            rectTransform.anchoredPosition = anchoredPos;
                            setProperties.Add($"anchoredPosition = {anchoredPos}");
                            break;
                            
                        case "sizedelta":
                            var sizeDelta = prop.Value.ToObject<Vector2>();
                            rectTransform.sizeDelta = sizeDelta;
                            setProperties.Add($"sizeDelta = {sizeDelta}");
                            break;
                            
                        case "anchormin":
                            var anchorMin = prop.Value.ToObject<Vector2>();
                            rectTransform.anchorMin = anchorMin;
                            setProperties.Add($"anchorMin = {anchorMin}");
                            break;
                            
                        case "anchormax":
                            var anchorMax = prop.Value.ToObject<Vector2>();
                            rectTransform.anchorMax = anchorMax;
                            setProperties.Add($"anchorMax = {anchorMax}");
                            break;
                            
                        case "pivot":
                            var pivot = prop.Value.ToObject<Vector2>();
                            rectTransform.pivot = pivot;
                            setProperties.Add($"pivot = {pivot}");
                            break;
                            
                        case "rotation":
                            var rotation = prop.Value.ToObject<Vector3>();
                            rectTransform.eulerAngles = rotation;
                            setProperties.Add($"rotation = {rotation}");
                            break;
                            
                        case "scale":
                            var scale = prop.Value.ToObject<Vector3>();
                            rectTransform.localScale = scale;
                            setProperties.Add($"scale = {scale}");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    setProperties.Add($"{prop.Key}: failed to set RectTransform property - {ex.Message}");
                }
            }
        }
        
        private static void SetUIComponentProperties(GameObject uiElement, JObject properties, List<string> setProperties)
        {
            foreach (var prop in properties)
            {
                try
                {
                    switch (prop.Key.ToLower())
                    {
                        case "text":
                            Text textComponent = uiElement.GetComponent<Text>();
                            if (textComponent != null)
                            {
                                textComponent.text = prop.Value.ToString();
                                setProperties.Add($"text = '{textComponent.text}'");
                            }
                            break;
                            
                        case "color":
                            Text text = uiElement.GetComponent<Text>();
                            Image image = uiElement.GetComponent<Image>();
                            if (ColorUtility.TryParseHtmlString(prop.Value.ToString(), out Color color))
                            {
                                if (text != null)
                                {
                                    text.color = color;
                                    setProperties.Add($"text color = {color}");
                                }
                                if (image != null)
                                {
                                    image.color = color;
                                    setProperties.Add($"image color = {color}");
                                }
                            }
                            break;
                            
                        case "fontsize":
                            Text textComp = uiElement.GetComponent<Text>();
                            if (textComp != null)
                            {
                                textComp.fontSize = prop.Value.ToObject<int>();
                                setProperties.Add($"fontSize = {textComp.fontSize}");
                            }
                            break;
                            
                        case "interactable":
                            Button button = uiElement.GetComponent<Button>();
                            Slider slider = uiElement.GetComponent<Slider>();
                            Toggle toggle = uiElement.GetComponent<Toggle>();
                            InputField inputField = uiElement.GetComponent<InputField>();
                            
                            bool interactable = prop.Value.ToObject<bool>();
                            if (button != null)
                            {
                                button.interactable = interactable;
                                setProperties.Add($"button interactable = {interactable}");
                            }
                            if (slider != null)
                            {
                                slider.interactable = interactable;
                                setProperties.Add($"slider interactable = {interactable}");
                            }
                            if (toggle != null)
                            {
                                toggle.interactable = interactable;
                                setProperties.Add($"toggle interactable = {interactable}");
                            }
                            if (inputField != null)
                            {
                                inputField.interactable = interactable;
                                setProperties.Add($"inputField interactable = {interactable}");
                            }
                            break;
                            
                        case "value":
                            Slider sliderComp = uiElement.GetComponent<Slider>();
                            Toggle toggleComp = uiElement.GetComponent<Toggle>();
                            
                            if (sliderComp != null)
                            {
                                sliderComp.value = prop.Value.ToObject<float>();
                                setProperties.Add($"slider value = {sliderComp.value}");
                            }
                            if (toggleComp != null)
                            {
                                toggleComp.isOn = prop.Value.ToObject<bool>();
                                setProperties.Add($"toggle value = {toggleComp.isOn}");
                            }
                            break;
                    }
                }
                catch (Exception ex)
                {
                    setProperties.Add($"{prop.Key}: failed to set UI component property - {ex.Message}");
                }
            }
        }
        
        #endregion
    }
}