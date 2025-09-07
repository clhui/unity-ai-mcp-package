using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;
using System.Linq;
using Unity.MCP;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Unity.MCP.Tools.Editor
{
    public static class UnityAnimationTools
    {
        /// <summary>
        /// 创建Animator组件
        /// </summary>
        public static McpToolResult CreateAnimator(string gameObjectName, string animatorControllerPath = null)
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
                
                Animator animator = targetObj.GetComponent<Animator>();
                if (animator == null)
                {
                    animator = targetObj.AddComponent<Animator>();
                }
                
                if (!string.IsNullOrEmpty(animatorControllerPath))
                {
                    RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(animatorControllerPath);
                    if (controller != null)
                    {
                        animator.runtimeAnimatorController = controller;
                    }
                    else
                    {
                        return new McpToolResult
                {
                    IsError = true,
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Animator controller not found at path: {animatorControllerPath}" }
                    }
                };
                    }
                }
                
                EditorUtility.SetDirty(targetObj);
                
                return new McpToolResult
                {
                    IsError = false,
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Animator created for '{gameObjectName}'" + " Data: " + JsonConvert.SerializeObject(new {  gameObjectName = gameObjectName, controllerPath = animatorControllerPath  })
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
                        new McpContent { Type = "text", Text = $"Failed to create animator: {ex.Message}" }
                    }
                };
            }
        }
        
        /// <summary>
        /// 设置动画剪辑
        /// </summary>
        public static McpToolResult SetAnimationClip(string gameObjectName, string clipName, string clipPath)
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
                
                Animation animationComponent = targetObj.GetComponent<Animation>();
                if (animationComponent == null)
                {
                    animationComponent = targetObj.AddComponent<Animation>();
                }
                
                AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
                if (clip == null)
                {
                    return new McpToolResult
                {
                    IsError = true,
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Animation clip not found at path: {clipPath}" }
                    }
                };
                }
                
                animationComponent.AddClip(clip, clipName);
                
                if (animationComponent.clip == null)
                {
                    animationComponent.clip = clip;
                }
                
                EditorUtility.SetDirty(targetObj);
                
                return new McpToolResult
                {
                    IsError = false,
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Animation clip '{clipName}' set for '{gameObjectName}'" + " Data: " + JsonConvert.SerializeObject(new {  gameObjectName = gameObjectName, clipName = clipName, clipPath = clipPath  })
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
                        new McpContent { Type = "text", Text = $"Failed to set animation clip: {ex.Message}" }
                    }
                };
            }
        }
        
        /// <summary>
        /// 播放动画
        /// </summary>
        public static McpToolResult PlayAnimation(string gameObjectName, string animationName = null, bool loop = false)
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
                
                // 尝试使用Animator
                Animator animator = targetObj.GetComponent<Animator>();
                if (animator != null && !string.IsNullOrEmpty(animationName))
                {
                    animator.Play(animationName);
                    
                    return new McpToolResult
                {
                    IsError = false,
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Animation '{animationName}' played on '{gameObjectName}' using Animator" + " Data: " + JsonConvert.SerializeObject(new {  gameObjectName = gameObjectName, animationName = animationName, component = "Animator"  })
                    }
                }
                    };
                }
                
                // 尝试使用Animation组件
                Animation animationComponent = targetObj.GetComponent<Animation>();
                if (animationComponent != null)
                {
                    if (!string.IsNullOrEmpty(animationName))
                    {
                        animationComponent[animationName].wrapMode = loop ? WrapMode.Loop : WrapMode.Once;
                        animationComponent.Play(animationName);
                    }
                    else if (animationComponent.clip != null)
                    {
                        animationComponent.clip.wrapMode = loop ? WrapMode.Loop : WrapMode.Once;
                        animationComponent.Play();
                    }
                    else
                    {
                        return new McpToolResult
                {
                    IsError = true,
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"No animation clip found on '{gameObjectName}'" }
                    }
                };
                    }
                    
                    return new McpToolResult
                {
                    IsError = false,
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Animation played on '{gameObjectName}' using Animation component" + " Data: " + JsonConvert.SerializeObject(new {  gameObjectName = gameObjectName, animationName = animationName, loop = loop, component = "Animation"  })
                    }
                }
                    };
                }
                
                return new McpToolResult
                {
                    IsError = true,
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"No Animator or Animation component found on '{gameObjectName}'" }
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
                        new McpContent { Type = "text", Text = $"Failed to play animation: {ex.Message}" }
                    }
                };
            }
        }
        
        /// <summary>
        /// 设置动画参数
        /// </summary>
        public static McpToolResult SetAnimationParameters(string gameObjectName, string parameters)
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
                
                Animator animator = targetObj.GetComponent<Animator>();
                if (animator == null)
                {
                    return new McpToolResult
                {
                    IsError = true,
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"No Animator component found on '{gameObjectName}'" }
                    }
                };
                }
                
                var paramDict = JsonUtility.FromJson<Dictionary<string, object>>(parameters);
                int parametersSet = 0;
                
                foreach (var param in paramDict)
                {
                    try
                    {
                        // 根据参数类型设置值
                        if (param.Value is bool boolValue)
                        {
                            animator.SetBool(param.Key, boolValue);
                            parametersSet++;
                        }
                        else if (param.Value is int intValue)
                        {
                            animator.SetInteger(param.Key, intValue);
                            parametersSet++;
                        }
                        else if (param.Value is float floatValue)
                        {
                            animator.SetFloat(param.Key, floatValue);
                            parametersSet++;
                        }
                        else if (param.Value is string stringValue)
                        {
                            // 尝试触发器
                            animator.SetTrigger(stringValue);
                            parametersSet++;
                        }
                        else
                        {
                            // 尝试转换为float
                            if (float.TryParse(param.Value.ToString(), out float convertedFloat))
                            {
                                animator.SetFloat(param.Key, convertedFloat);
                                parametersSet++;
                            }
                        }
                    }
                    catch (System.Exception paramEx)
                    {
                        Debug.LogWarning($"Failed to set parameter '{param.Key}': {paramEx.Message}");
                    }
                }
                
                EditorUtility.SetDirty(targetObj);
                
                return new McpToolResult
                {
                    IsError = false,
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Set {parametersSet} animation parameters for '{gameObjectName}'" + " Data: " + JsonConvert.SerializeObject(new {  gameObjectName = gameObjectName, parametersSet = parametersSet, totalParameters = paramDict.Count  })
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
                        new McpContent { Type = "text", Text = $"Failed to set animation parameters: {ex.Message}" }
                    }
                };
            }
        }
        
        /// <summary>
        /// 创建动画剪辑
        /// </summary>
        public static McpToolResult CreateAnimationClip(string clipName, string savePath, string targetObjectName = null)
        {
            try
            {
                AnimationClip clip = new AnimationClip();
                clip.name = clipName;
                
                // 设置基本属性
                clip.frameRate = 30f;
                clip.wrapMode = WrapMode.Once;
                
                // 如果指定了目标对象，创建一个简单的位置动画
                if (!string.IsNullOrEmpty(targetObjectName))
                {
                    GameObject targetObj = GameObject.Find(targetObjectName);
                    if (targetObj != null)
                    {
                        // 创建位置动画曲线
                        AnimationCurve xCurve = AnimationCurve.Linear(0f, targetObj.transform.position.x, 1f, targetObj.transform.position.x + 1f);
                        AnimationCurve yCurve = AnimationCurve.Linear(0f, targetObj.transform.position.y, 1f, targetObj.transform.position.y);
                        AnimationCurve zCurve = AnimationCurve.Linear(0f, targetObj.transform.position.z, 1f, targetObj.transform.position.z);
                        
                        clip.SetCurve("", typeof(Transform), "localPosition.x", xCurve);
                        clip.SetCurve("", typeof(Transform), "localPosition.y", yCurve);
                        clip.SetCurve("", typeof(Transform), "localPosition.z", zCurve);
                    }
                }
                
                // 保存动画剪辑
                string fullPath = savePath.EndsWith(".anim") ? savePath : savePath + "/" + clipName + ".anim";
                AssetDatabase.CreateAsset(clip, fullPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                
                return new McpToolResult
                {
                    IsError = false,
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Animation clip '{clipName}' created at '{fullPath}'" + " Data: " + JsonConvert.SerializeObject(new {  clipName = clipName, savePath = fullPath, targetObject = targetObjectName  })
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
                        new McpContent { Type = "text", Text = $"Failed to create animation clip: {ex.Message}" }
                    }
                };
            }
        }
    }
}