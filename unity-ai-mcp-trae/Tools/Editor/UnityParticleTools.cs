using UnityEngine;
using UnityEditor;
using System;

namespace Unity.MCP
{
    public static class UnityParticleTools
    {
        /// <summary>
        /// 创建粒子系统
        /// </summary>
        public static string CreateParticleSystem(string gameObjectName, string particleSystemName = "")
        {
            try
            {
                GameObject targetObject = string.IsNullOrEmpty(gameObjectName) ? 
                    new GameObject(string.IsNullOrEmpty(particleSystemName) ? "ParticleSystem" : particleSystemName) :
                    GameObject.Find(gameObjectName);
                    
                if (targetObject == null)
                {
                    targetObject = new GameObject(gameObjectName);
                }
                
                ParticleSystem particleSystem = targetObject.GetComponent<ParticleSystem>();
                if (particleSystem == null)
                {
                    particleSystem = targetObject.AddComponent<ParticleSystem>();
                }
                
                // Set default particle system properties
                var main = particleSystem.main;
                main.startLifetime = 5.0f;
                main.startSpeed = 5.0f;
                main.startSize = 1.0f;
                main.startColor = Color.white;
                main.maxParticles = 1000;
                
                return $"Particle system created on GameObject: {targetObject.name}";
            }
            catch (System.Exception e)
            {
                return $"Error creating particle system: {e.Message}";
            }
        }
        
        /// <summary>
        /// 设置粒子系统属性
        /// </summary>
        public static string SetParticleProperties(string gameObjectName, string propertiesJson)
        {
            try
            {
                GameObject targetObject = GameObject.Find(gameObjectName);
                if (targetObject == null)
                {
                    return $"GameObject '{gameObjectName}' not found";
                }
                
                ParticleSystem particleSystem = targetObject.GetComponent<ParticleSystem>();
                if (particleSystem == null)
                {
                    return $"No ParticleSystem component found on '{gameObjectName}'";
                }
                
                var properties = JsonUtility.FromJson<ParticleProperties>(propertiesJson);
                
                // Main module
                var main = particleSystem.main;
                if (properties.startLifetime > 0) main.startLifetime = properties.startLifetime;
                if (properties.startSpeed > 0) main.startSpeed = properties.startSpeed;
                if (properties.startSize > 0) main.startSize = properties.startSize;
                if (properties.maxParticles > 0) main.maxParticles = properties.maxParticles;
                if (!string.IsNullOrEmpty(properties.startColor))
                {
                    if (ColorUtility.TryParseHtmlString(properties.startColor, out Color color))
                    {
                        main.startColor = color;
                    }
                }
                
                // Emission module
                if (properties.emissionRate > 0)
                {
                    var emission = particleSystem.emission;
                    emission.rateOverTime = properties.emissionRate;
                }
                
                // Shape module
                if (!string.IsNullOrEmpty(properties.shape))
                {
                    var shape = particleSystem.shape;
                    switch (properties.shape.ToLower())
                    {
                        case "sphere":
                            shape.shapeType = ParticleSystemShapeType.Sphere;
                            break;
                        case "box":
                            shape.shapeType = ParticleSystemShapeType.Box;
                            break;
                        case "cone":
                            shape.shapeType = ParticleSystemShapeType.Cone;
                            break;
                        case "circle":
                            shape.shapeType = ParticleSystemShapeType.Circle;
                            break;
                    }
                }
                
                return $"Particle system properties updated for '{gameObjectName}'";
            }
            catch (System.Exception e)
            {
                return $"Error setting particle properties: {e.Message}";
            }
        }
        
        /// <summary>
        /// 播放粒子效果
        /// </summary>
        public static string PlayParticleEffect(string gameObjectName, bool play = true)
        {
            try
            {
                GameObject targetObject = GameObject.Find(gameObjectName);
                if (targetObject == null)
                {
                    return $"GameObject '{gameObjectName}' not found";
                }
                
                ParticleSystem particleSystem = targetObject.GetComponent<ParticleSystem>();
                if (particleSystem == null)
                {
                    return $"No ParticleSystem component found on '{gameObjectName}'";
                }
                
                if (play)
                {
                    particleSystem.Play();
                    return $"Particle effect started on '{gameObjectName}'";
                }
                else
                {
                    particleSystem.Stop();
                    return $"Particle effect stopped on '{gameObjectName}'";
                }
            }
            catch (System.Exception e)
            {
                return $"Error controlling particle effect: {e.Message}";
            }
        }
        
        /// <summary>
        /// 创建预定义粒子效果
        /// </summary>
        public static string CreateParticleEffect(string effectName, string effectType, string targetObjectName = "")
        {
            try
            {
                GameObject targetObject = null;
                if (!string.IsNullOrEmpty(targetObjectName))
                {
                    targetObject = GameObject.Find(targetObjectName);
                }
                
                GameObject effectObject = new GameObject(effectName);
                if (targetObject != null)
                {
                    effectObject.transform.SetParent(targetObject.transform);
                }
                
                ParticleSystem particleSystem = effectObject.AddComponent<ParticleSystem>();
                
                // 根据效果类型设置不同的粒子参数
                var main = particleSystem.main;
                var emission = particleSystem.emission;
                var shape = particleSystem.shape;
                
                switch (effectType.ToLower())
                {
                    case "fire":
                        main.startColor = Color.red;
                        main.startLifetime = 2.0f;
                        main.startSpeed = 3.0f;
                        main.startSize = 0.5f;
                        emission.rateOverTime = 50;
                        shape.shapeType = ParticleSystemShapeType.Cone;
                        break;
                    case "smoke":
                        main.startColor = Color.gray;
                        main.startLifetime = 5.0f;
                        main.startSpeed = 1.0f;
                        main.startSize = 1.0f;
                        emission.rateOverTime = 20;
                        shape.shapeType = ParticleSystemShapeType.Circle;
                        break;
                    case "explosion":
                        main.startColor = Color.yellow;
                        main.startLifetime = 1.0f;
                        main.startSpeed = 10.0f;
                        main.startSize = 0.3f;
                        emission.rateOverTime = 100;
                        shape.shapeType = ParticleSystemShapeType.Sphere;
                        break;
                    case "rain":
                        main.startColor = Color.blue;
                        main.startLifetime = 3.0f;
                        main.startSpeed = 8.0f;
                        main.startSize = 0.1f;
                        emission.rateOverTime = 200;
                        shape.shapeType = ParticleSystemShapeType.Box;
                        break;
                    default:
                        main.startColor = Color.white;
                        main.startLifetime = 5.0f;
                        main.startSpeed = 5.0f;
                        main.startSize = 1.0f;
                        emission.rateOverTime = 30;
                        shape.shapeType = ParticleSystemShapeType.Sphere;
                        break;
                }
                
                return $"Particle effect '{effectType}' created as '{effectName}'";
            }
            catch (System.Exception e)
            {
                return $"Error creating particle effect: {e.Message}";
            }
        }
    }
    
    /// <summary>
    /// 粒子系统属性类
    /// </summary>
    [System.Serializable]
    public class ParticleProperties
    {
        public float startLifetime;
        public float startSpeed;
        public float startSize;
        public string startColor;
        public int maxParticles;
        public float emissionRate;
        public string shape;
    }
}