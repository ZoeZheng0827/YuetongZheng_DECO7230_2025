using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class ProximityPlanetInteraction : MonoBehaviour
{
    [Header("Proximity Settings")]
    [Tooltip("Distance threshold to start interaction")]
    public float interactionDistance = 3.0f;
    
    [Tooltip("Time player needs to stay close to trigger transition")]
    public float holdTime = 2.0f;
    
    [Header("Scene Transition")]
    [Tooltip("Name of the scene to load")]
    public string targetSceneName = "VoiceChannel";
    
    [Header("Visual Feedback")]
    [Tooltip("Scale multiplier when player is nearby")]
    public float hoverScale = 1.2f;
    
    [Tooltip("Scale animation speed")]
    public float scaleSpeed = 2.0f;
    
    [Tooltip("Glow intensity when interacting")]
    public float glowIntensity = 2.0f;
    
    [Header("Debug")]
    public bool showDebugInfo = true;
    public bool showDebugGizmos = true;
    
    // Components
    private Camera playerCamera;
    private Transform player;
    private Renderer planetRenderer;
    private Material originalMaterial;
    private Material glowMaterial;
    
    // State
    private bool playerInRange = false;
    private bool isInteracting = false;
    private bool isTransitioning = false;
    private float interactionTimer = 0f;
    private Vector3 originalScale;
    
    // Visual effects
    private Color originalColor;
    private Color glowColor = Color.cyan;

    void Start()
    {
        // Find player camera/transform
        playerCamera = Camera.main;
        if (playerCamera == null) playerCamera = FindObjectOfType<Camera>();
        
        if (playerCamera != null)
        {
            player = playerCamera.transform;
        }
        else
        {
            Debug.LogError($"[PlanetInteraction] No camera found for {name}!");
            enabled = false;
            return;
        }
        
        // Get renderer for visual effects
        planetRenderer = GetComponent<Renderer>();
        if (planetRenderer != null)
        {
            originalMaterial = planetRenderer.material;
            originalColor = originalMaterial.color;
            
            // Create glow material
            glowMaterial = new Material(originalMaterial);
            glowColor = originalColor * glowIntensity;
        }
        
        // Store original scale
        originalScale = transform.localScale;
        
        // Ensure we have a collider for trigger detection
        if (GetComponent<Collider>() == null)
        {
            SphereCollider col = gameObject.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = interactionDistance;
            if (showDebugInfo) Debug.Log($"[PlanetInteraction] Added trigger collider to {name}");
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"[PlanetInteraction] {name} initialized. Player: {player?.name}");
            Debug.Log($"[PlanetInteraction] Interaction distance: {interactionDistance}, Hold time: {holdTime}s");
        }
    }

    void Update()
    {
        if (isTransitioning || player == null) return;
        
        CheckPlayerProximity();
        UpdateVisualEffects();
        HandleInteraction();
    }
/*
The code snippet (1. Proximity-based Interaction) below has been adapted with
assistance from Claude AI to replace the previous "grab and hold" mechanic.
It now uses player proximity and hold time detection to trigger scene transition.
*/
    void CheckPlayerProximity()
    {
        float distance = Vector3.Distance(transform.position, player.position);
        bool wasInRange = playerInRange;
        playerInRange = distance <= interactionDistance;
        
        // Debug info when state changes
        if (playerInRange != wasInRange && showDebugInfo)
        {
            Debug.Log($"[PlanetInteraction] Player {(playerInRange ? "entered" : "left")} range of {name}. Distance: {distance:F2}");
        }
        
        // Reset interaction if player leaves range
        if (!playerInRange && isInteracting)
        {
            isInteracting = false;
            interactionTimer = 0f;
            if (showDebugInfo)
                Debug.Log($"[PlanetInteraction] Interaction cancelled - player left range of {name}");
        }
    }

    void HandleInteraction()
    {
        if (playerInRange && !isInteracting && !isTransitioning)
        {
            // Start interaction
            isInteracting = true;
            interactionTimer = 0f;
            if (showDebugInfo)
                Debug.Log($"[PlanetInteraction] Started interaction with {name}");
        }
        
        if (isInteracting)
        {
            interactionTimer += Time.deltaTime;
            
            // Debug progress
            if (showDebugInfo && Time.frameCount % 30 == 0)
            {
                float progress = (interactionTimer / holdTime) * 100f;
                Debug.Log($"[PlanetInteraction] Interaction progress: {progress:F0}% ({interactionTimer:F1}s/{holdTime}s)");
            }
            
            // Check if hold time completed
            if (interactionTimer >= holdTime)
            {
                StartTransition();
            }
        }
    }

    void UpdateVisualEffects()
    {
        if (planetRenderer == null) return;
        
        // Scale animation
        Vector3 targetScale = originalScale;
        Color targetColor = originalColor;
        
        if (playerInRange)
        {
            targetScale = originalScale * hoverScale;
            
            if (isInteracting)
            {
                // Pulsing glow effect during interaction
                float pulseIntensity = 0.5f + 0.5f * Mathf.Sin(Time.time * 5f);
                targetColor = Color.Lerp(originalColor, glowColor, pulseIntensity);
                
                // Scale increases with interaction progress
                float progressScale = 1f + (interactionTimer / holdTime) * 0.5f;
                targetScale = originalScale * hoverScale * progressScale;
            }
            else
            {
                targetColor = Color.Lerp(originalColor, glowColor, 0.3f);
            }
        }
        
        // Apply visual changes
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, scaleSpeed * Time.deltaTime);
        planetRenderer.material.color = Color.Lerp(planetRenderer.material.color, targetColor, scaleSpeed * Time.deltaTime);
    }

    void StartTransition()
    {
        if (isTransitioning) return;
        
        isTransitioning = true;
        isInteracting = false;
        
        if (showDebugInfo)
            Debug.Log($"[PlanetInteraction] Starting transition to {targetSceneName} from {name}");
        
        StartCoroutine(DoTransition());
    }

    IEnumerator DoTransition()
    {
        // Disable orbit and collision scripts if present
        GalaxyOrbit orbit = GetComponent<GalaxyOrbit>();
        StrongCollisionAvoidance collision = GetComponent<StrongCollisionAvoidance>();
        
        if (orbit != null) orbit.enabled = false;
        if (collision != null) collision.enabled = false;
        
        // Flash effect
        if (planetRenderer != null)
        {
            for (int i = 0; i < 3; i++)
            {
                planetRenderer.material.color = Color.white;
                yield return new WaitForSeconds(0.1f);
                planetRenderer.material.color = originalColor;
                yield return new WaitForSeconds(0.1f);
            }
        }
        
        // Scale up then disappear
        Vector3 startScale = transform.localScale;
        Vector3 maxScale = startScale * 3f;
        
        // Scale up
        float duration = 0.3f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            transform.localScale = Vector3.Lerp(startScale, maxScale, progress);
            yield return null;
        }
        
        // Scale down to zero
        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            transform.localScale = Vector3.Lerp(maxScale, Vector3.zero, progress);
            yield return null;
        }
        
        // Load new scene
        if (showDebugInfo)
            Debug.Log($"[PlanetInteraction] Loading scene: {targetSceneName}");
        
        SceneManager.LoadScene(targetSceneName);
    }

    // Trigger detection (alternative method)
    void OnTriggerEnter(Collider other)
    {
        // Check if it's the player
        if (other.transform == player || other.GetComponentInParent<Camera>() == playerCamera)
        {
            if (showDebugInfo)
                Debug.Log($"[PlanetInteraction] Player entered trigger zone of {name}");
        }
    }

    void OnTriggerExit(Collider other)
    {
        // Check if it's the player
        if (other.transform == player || other.GetComponentInParent<Camera>() == playerCamera)
        {
            if (showDebugInfo)
                Debug.Log($"[PlanetInteraction] Player left trigger zone of {name}");
        }
    }
// End code snippet (1. Proximity-based Interaction)
    // GUI for debugging and progress display
    void OnGUI()
    {
        if (!showDebugInfo) return;
        
        // Show interaction progress
        if (isInteracting)
        {
            float progress = interactionTimer / holdTime;
            float screenCenterX = Screen.width * 0.5f;
            float screenCenterY = Screen.height * 0.8f;
            
            // Progress bar background
            GUI.color = Color.black;
            GUI.Box(new Rect(screenCenterX - 100, screenCenterY - 10, 200, 20), "");
            
            // Progress bar fill
            GUI.color = Color.green;
            GUI.Box(new Rect(screenCenterX - 98, screenCenterY - 8, 196 * progress, 16), "");
            
            // Text
            GUI.color = Color.white;
            GUI.Label(new Rect(screenCenterX - 50, screenCenterY - 5, 100, 20), $"Entering... {progress*100:F0}%");
        }
        
        // Debug info (only in editor)
        if (Application.isEditor)
        {
            GUI.color = Color.white;
            GUI.Box(new Rect(10, 400, 300, 120), $"Planet Interaction - {name}");
            GUI.Label(new Rect(15, 420, 290, 20), $"Player in range: {playerInRange}");
            GUI.Label(new Rect(15, 440, 290, 20), $"Is interacting: {isInteracting}");
            GUI.Label(new Rect(15, 460, 290, 20), $"Timer: {interactionTimer:F1}s / {holdTime}s");
            
            if (player != null)
            {
                float dist = Vector3.Distance(transform.position, player.position);
                GUI.Label(new Rect(15, 480, 290, 20), $"Distance: {dist:F2} / {interactionDistance}");
            }
            
            GUI.Label(new Rect(15, 500, 290, 20), "Walk close and stay for 2 seconds");
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos) return;
        
        // Draw interaction range
        Gizmos.color = playerInRange ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);
        
        // Draw connection to player if in range
        if (playerInRange && player != null)
        {
            Gizmos.color = isInteracting ? Color.green : Color.yellow;
            Gizmos.DrawLine(transform.position, player.position);
        }
        
        // Draw interaction progress as a partial circle
        if (isInteracting)
        {
            Gizmos.color = Color.cyan;
            // This would need a more complex implementation to draw partial circles
            Gizmos.DrawWireSphere(transform.position, interactionDistance * 1.1f);
        }
    }

    void OnDestroy()
    {
        // Clean up created materials
        if (glowMaterial != null && glowMaterial != originalMaterial)
        {
            DestroyImmediate(glowMaterial);
        }
    }
}