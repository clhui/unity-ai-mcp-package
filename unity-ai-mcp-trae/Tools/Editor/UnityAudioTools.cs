using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json.Linq;
using Unity.MCP;

namespace Unity.MCP.Tools.Editor
{
    /// <summary>
    /// Unity音频系统工具类
    /// 提供音频播放、停止等功能
    /// </summary>
    public static class UnityAudioTools
    {
        /// <summary>
        /// 播放音频
        /// </summary>
        /// <param name="arguments">包含gameObject、audioClip、loop、volume、pitch等参数的JSON对象</param>
        /// <returns>操作结果</returns>
        public static McpToolResult PlayAudio(JObject arguments)
        {
            var gameObjectName = arguments["gameObject"]?.ToString();
            var audioClipPath = arguments["audioClip"]?.ToString();
            var loop = arguments["loop"]?.ToObject<bool>() ?? false;
            var volume = arguments["volume"]?.ToObject<float>() ?? 1.0f;
            var pitch = arguments["pitch"]?.ToObject<float>() ?? 1.0f;
            
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
                
                var audioSource = go.GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    // 如果没有AudioSource组件，自动添加一个
                    audioSource = go.AddComponent<AudioSource>();
#if UNITY_EDITOR
                    // 使用EditorApplication.delayCall避免阻塞
                    EditorApplication.delayCall += () => {
                        if (audioSource != null)
                        {
                            Undo.RegisterCreatedObjectUndo(audioSource, "Add AudioSource");
                        }
                    };
#endif
                }
                
                // 如果提供了音频剪辑路径，加载音频剪辑
                if (!string.IsNullOrEmpty(audioClipPath))
                {
                    AudioClip audioClip = null;
#if UNITY_EDITOR
                    audioClip = AssetDatabase.LoadAssetAtPath<AudioClip>(audioClipPath);
#else
                    var clipName = Path.GetFileNameWithoutExtension(audioClipPath);
                    audioClip = Resources.Load<AudioClip>(clipName);
#endif
                    
                    if (audioClip == null)
                    {
                        return new McpToolResult
                        {
                            Content = new List<McpContent>
                            {
                                new McpContent { Type = "text", Text = $"Audio clip not found at path: {audioClipPath}" }
                            },
                            IsError = true
                        };
                    }
                    
                    audioSource.clip = audioClip;
                }
                
                if (audioSource.clip == null)
                {
                    return new McpToolResult
                    {
                        Content = new List<McpContent>
                        {
                            new McpContent { Type = "text", Text = $"No audio clip assigned to AudioSource on {gameObjectName}" }
                        },
                        IsError = true
                    };
                }
                
                // 设置音频属性
                audioSource.loop = loop;
                audioSource.volume = Mathf.Clamp01(volume);
                audioSource.pitch = pitch;
                
                // 播放音频
                audioSource.Play();
                
#if UNITY_EDITOR
                // 使用EditorApplication.delayCall避免阻塞
                EditorApplication.delayCall += () => {
                    if (audioSource != null)
                    {
                        Undo.RecordObject(audioSource, "Play Audio");
                        EditorUtility.SetDirty(audioSource);
                    }
                };
#endif
                
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Playing audio on {gameObjectName}: {audioSource.clip.name} (loop: {loop}, volume: {volume}, pitch: {pitch})" }
                    }
                };
            }
            catch (Exception ex)
            {
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Failed to play audio: {ex.Message}" }
                    },
                    IsError = true
                };
            }
        }
        
        /// <summary>
        /// 停止音频播放
        /// </summary>
        /// <param name="arguments">包含gameObject、fadeOut、fadeTime等参数的JSON对象</param>
        /// <returns>操作结果</returns>
        public static McpToolResult StopAudio(JObject arguments)
        {
            var gameObjectName = arguments["gameObject"]?.ToString();
            var fadeOut = arguments["fadeOut"]?.ToObject<bool>() ?? false;
            var fadeTime = arguments["fadeTime"]?.ToObject<float>() ?? 1.0f;
            
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
                
                var audioSource = go.GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    return new McpToolResult
                    {
                        Content = new List<McpContent>
                        {
                            new McpContent { Type = "text", Text = $"No AudioSource component found on GameObject: {gameObjectName}" }
                        },
                        IsError = true
                    };
                }
                
                if (!audioSource.isPlaying)
                {
                    return new McpToolResult
                    {
                        Content = new List<McpContent>
                        {
                            new McpContent { Type = "text", Text = $"Audio is not currently playing on {gameObjectName}" }
                        }
                    };
                }
                
                // 直接停止音频，避免在编辑器模式下的阻塞问题
                audioSource.Stop();
                
                var message = fadeOut && fadeTime > 0 
                    ? $"Stopped audio on {gameObjectName} with fade out ({fadeTime}s)" 
                    : $"Stopped audio on {gameObjectName}";
                
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = message }
                    }
                };
            }
            catch (Exception ex)
            {
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Failed to stop audio: {ex.Message}" }
                    },
                    IsError = true
                };
            }
        }
        
        /// <summary>
        /// 设置音频音量
        /// </summary>
        /// <param name="arguments">包含gameObject、volume等参数的JSON对象</param>
        /// <returns>操作结果</returns>
        public static McpToolResult SetAudioVolume(JObject arguments)
        {
            var gameObjectName = arguments["gameObject"]?.ToString();
            var volume = arguments["volume"]?.ToObject<float>() ?? 1.0f;
            
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
                
                var audioSource = go.GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    return new McpToolResult
                    {
                        Content = new List<McpContent>
                        {
                            new McpContent { Type = "text", Text = $"No AudioSource component found on GameObject: {gameObjectName}" }
                        },
                        IsError = true
                    };
                }
                
                // 设置音量
                audioSource.volume = Mathf.Clamp01(volume);
                
#if UNITY_EDITOR
                Undo.RecordObject(audioSource, "Set Audio Volume");
                EditorUtility.SetDirty(audioSource);
#endif
                
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Set audio volume on {gameObjectName} to {audioSource.volume}" }
                    }
                };
            }
            catch (Exception ex)
            {
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Failed to set audio volume: {ex.Message}" }
                    },
                    IsError = true
                };
            }
        }
        
        /// <summary>
        /// 设置音频属性
        /// </summary>
        /// <param name="arguments">包含gameObject和properties参数的JSON对象</param>
        /// <returns>操作结果</returns>
        public static McpToolResult SetAudioProperties(JObject arguments)
        {
            try
            {
                if (arguments == null)
                {
                    return new McpToolResult
                    {
                        Content = new List<McpContent>
                        {
                            new McpContent { Type = "text", Text = "Arguments cannot be null" }
                        },
                        IsError = true
                    };
                }

                string gameObjectName = arguments["gameObject"]?.ToString();
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

                GameObject go = GameObject.Find(gameObjectName);
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

                AudioSource audioSource = go.GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    return new McpToolResult
                    {
                        Content = new List<McpContent>
                        {
                            new McpContent { Type = "text", Text = $"No AudioSource component found on GameObject: {gameObjectName}" }
                        },
                        IsError = true
                    };
                }

                var properties = arguments["properties"] as JObject;
                if (properties == null)
                {
                    return new McpToolResult
                    {
                        Content = new List<McpContent>
                        {
                            new McpContent { Type = "text", Text = "Properties parameter is required" }
                        },
                        IsError = true
                    };
                }

#if UNITY_EDITOR
                Undo.RecordObject(audioSource, "Set Audio Properties");
#endif

                var setProperties = new List<string>();

                // 设置各种音频属性
                foreach (var prop in properties)
                {
                    try
                    {
                        switch (prop.Key.ToLower())
                        {
                            case "volume":
                                audioSource.volume = Mathf.Clamp01(prop.Value.ToObject<float>());
                                setProperties.Add($"volume = {audioSource.volume}");
                                break;
                            case "pitch":
                                audioSource.pitch = prop.Value.ToObject<float>();
                                setProperties.Add($"pitch = {audioSource.pitch}");
                                break;
                            case "loop":
                                audioSource.loop = prop.Value.ToObject<bool>();
                                setProperties.Add($"loop = {audioSource.loop}");
                                break;
                            case "mute":
                                audioSource.mute = prop.Value.ToObject<bool>();
                                setProperties.Add($"mute = {audioSource.mute}");
                                break;
                            case "priority":
                                audioSource.priority = Mathf.Clamp(prop.Value.ToObject<int>(), 0, 256);
                                setProperties.Add($"priority = {audioSource.priority}");
                                break;
                            case "stereoPan":
                                audioSource.panStereo = Mathf.Clamp(prop.Value.ToObject<float>(), -1f, 1f);
                                setProperties.Add($"stereoPan = {audioSource.panStereo}");
                                break;
                            case "spatialBlend":
                                audioSource.spatialBlend = Mathf.Clamp01(prop.Value.ToObject<float>());
                                setProperties.Add($"spatialBlend = {audioSource.spatialBlend}");
                                break;
                            case "reverbZoneMix":
                                audioSource.reverbZoneMix = Mathf.Clamp01(prop.Value.ToObject<float>());
                                setProperties.Add($"reverbZoneMix = {audioSource.reverbZoneMix}");
                                break;
                            case "dopplerLevel":
                                audioSource.dopplerLevel = Mathf.Max(0f, prop.Value.ToObject<float>());
                                setProperties.Add($"dopplerLevel = {audioSource.dopplerLevel}");
                                break;
                            case "spread":
                                audioSource.spread = Mathf.Clamp(prop.Value.ToObject<float>(), 0f, 360f);
                                setProperties.Add($"spread = {audioSource.spread}");
                                break;
                            case "minDistance":
                                audioSource.minDistance = Mathf.Max(0f, prop.Value.ToObject<float>());
                                setProperties.Add($"minDistance = {audioSource.minDistance}");
                                break;
                            case "maxDistance":
                                audioSource.maxDistance = Mathf.Max(audioSource.minDistance, prop.Value.ToObject<float>());
                                setProperties.Add($"maxDistance = {audioSource.maxDistance}");
                                break;
                            case "rolloffMode":
                                if (Enum.TryParse<AudioRolloffMode>(prop.Value.ToString(), true, out var rolloffMode))
                                {
                                    audioSource.rolloffMode = rolloffMode;
                                    setProperties.Add($"rolloffMode = {audioSource.rolloffMode}");
                                }
                                else
                                {
                                    setProperties.Add($"rolloffMode: invalid value {prop.Value}");
                                }
                                break;
                            default:
                                setProperties.Add($"{prop.Key}: unknown property");
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        setProperties.Add($"{prop.Key}: failed to set - {ex.Message}");
                    }
                }

#if UNITY_EDITOR
                EditorUtility.SetDirty(audioSource);
#endif

                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Set audio properties on {gameObjectName}:\n" + string.Join("\n", setProperties) }
                    }
                };
            }
            catch (Exception ex)
            {
                return new McpToolResult
                {
                    Content = new List<McpContent>
                    {
                        new McpContent { Type = "text", Text = $"Failed to set audio properties: {ex.Message}" }
                    },
                    IsError = true
                };
            }
        }
    }
}