using UnityEngine;
using System.Collections;

namespace Unity.MCP.Examples
{
    /// <summary>
    /// Example controller demonstrating Unity MCP Trae functionality
    /// This script shows how the MCP server can interact with Unity objects
    /// </summary>
    public class McpExampleController : MonoBehaviour
    {
        [Header("MCP Example Settings")]
        [SerializeField] private GameObject targetObject;
        [SerializeField] private Material[] materials;
        [SerializeField] private AudioClip exampleSound;
        [SerializeField] private ParticleSystem particles;
        
        [Header("Animation Settings")]
        [SerializeField] private float rotationSpeed = 45f;
        [SerializeField] private float moveDistance = 2f;
        [SerializeField] private float moveDuration = 2f;
        
        private Vector3 originalPosition;
        private bool isMoving = false;
        private Renderer objectRenderer;
        private AudioSource audioSource;
        
        void Start()
        {
            // Store original position
            originalPosition = transform.position;
            
            // Get components
            objectRenderer = GetComponent<Renderer>();
            audioSource = GetComponent<AudioSource>();
            
            // Setup audio source if available
            if (audioSource && exampleSound)
            {
                audioSource.clip = exampleSound;
                audioSource.playOnAwake = false;
            }
            
            Debug.Log("[MCP Example] Controller initialized. This object can be controlled via MCP server.");
        }
        
        void Update()
        {
            // Continuous rotation for visual effect
            transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);
        }
        
        /// <summary>
        /// Move object to a new position (can be called via MCP)
        /// </summary>
        public void MoveToPosition(Vector3 newPosition)
        {
            if (!isMoving)
            {
                StartCoroutine(MoveCoroutine(newPosition));
            }
        }
        
        /// <summary>
        /// Reset object to original position (can be called via MCP)
        /// </summary>
        public void ResetPosition()
        {
            MoveToPosition(originalPosition);
        }
        
        /// <summary>
        /// Change material (can be called via MCP)
        /// </summary>
        public void ChangeMaterial(int materialIndex)
        {
            if (objectRenderer && materials != null && materialIndex >= 0 && materialIndex < materials.Length)
            {
                objectRenderer.material = materials[materialIndex];
                Debug.Log($"[MCP Example] Material changed to: {materials[materialIndex].name}");
            }
        }
        
        /// <summary>
        /// Play sound effect (can be called via MCP)
        /// </summary>
        public void PlaySound()
        {
            if (audioSource && exampleSound)
            {
                audioSource.Play();
                Debug.Log("[MCP Example] Sound played via MCP command");
            }
        }
        
        /// <summary>
        /// Toggle particle system (can be called via MCP)
        /// </summary>
        public void ToggleParticles()
        {
            if (particles)
            {
                if (particles.isPlaying)
                {
                    particles.Stop();
                    Debug.Log("[MCP Example] Particles stopped");
                }
                else
                {
                    particles.Play();
                    Debug.Log("[MCP Example] Particles started");
                }
            }
        }
        
        /// <summary>
        /// Scale object (can be called via MCP)
        /// </summary>
        public void ScaleObject(float scaleFactor)
        {
            transform.localScale = Vector3.one * scaleFactor;
            Debug.Log($"[MCP Example] Object scaled to: {scaleFactor}");
        }
        
        /// <summary>
        /// Get current object status (can be called via MCP)
        /// </summary>
        public string GetStatus()
        {
            return $"Position: {transform.position}, Scale: {transform.localScale.x}, IsMoving: {isMoving}";
        }
        
        private IEnumerator MoveCoroutine(Vector3 targetPosition)
        {
            isMoving = true;
            Vector3 startPosition = transform.position;
            float elapsedTime = 0;
            
            while (elapsedTime < moveDuration)
            {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / moveDuration;
                
                // Smooth movement with easing
                progress = Mathf.SmoothStep(0, 1, progress);
                transform.position = Vector3.Lerp(startPosition, targetPosition, progress);
                
                yield return null;
            }
            
            transform.position = targetPosition;
            isMoving = false;
            
            Debug.Log($"[MCP Example] Moved to position: {targetPosition}");
        }
        
        /// <summary>
        /// Demonstrate MCP integration by showing available commands
        /// </summary>
        [ContextMenu("Show MCP Commands")]
        public void ShowMcpCommands()
        {
            Debug.Log("[MCP Example] Available MCP commands for this object:");
            Debug.Log("- MoveToPosition(Vector3): Move object to specified position");
            Debug.Log("- ResetPosition(): Return to original position");
            Debug.Log("- ChangeMaterial(int): Change to material by index");
            Debug.Log("- PlaySound(): Play the assigned audio clip");
            Debug.Log("- ToggleParticles(): Start/stop particle system");
            Debug.Log("- ScaleObject(float): Scale the object");
            Debug.Log("- GetStatus(): Get current object information");
        }
        
        void OnDrawGizmosSelected()
        {
            // Draw movement range
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(originalPosition, moveDistance);
            
            // Draw current position
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, 0.1f);
        }
    }
}