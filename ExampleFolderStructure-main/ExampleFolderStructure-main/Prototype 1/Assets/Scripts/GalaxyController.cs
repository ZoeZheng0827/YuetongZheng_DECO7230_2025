using UnityEngine;

public class GalaxyController : MonoBehaviour
{
    [Header("Galaxy Control")]
    public float mouseSensitivity = 2f;
    public float speedMultiplierRange = 5f;
    public float smoothingSpeed = 2f;
    
    private GalaxyOrbit[] allPlanets;
    private float[] originalSpeeds;
    private float currentSpeedMultiplier = 1f;
    private float targetSpeedMultiplier = 1f;
    
    void Start()
    {
        // Find all planets and store their original speeds
        allPlanets = FindObjectsOfType<GalaxyOrbit>();
        originalSpeeds = new float[allPlanets.Length];
        
        for (int i = 0; i < allPlanets.Length; i++)
        {
            originalSpeeds[i] = allPlanets[i].orbitSpeed;
        }
    }
    
    void Update()
    {
        HandleMouseControl();
        UpdatePlanetSpeeds();
    }
    
    void HandleMouseControl()
    {
        // Check if right mouse button is held down
        if (Input.GetMouseButton(1))
        {
            // Get mouse movement
            float mouseY = Input.GetAxis("Mouse Y");
            
            // Update target speed multiplier based on mouse movement
            targetSpeedMultiplier += mouseY * mouseSensitivity;
            targetSpeedMultiplier = Mathf.Clamp(targetSpeedMultiplier, -speedMultiplierRange, speedMultiplierRange);
        }
        else
        {
            // Gradually return to normal speed when not controlling
            targetSpeedMultiplier = Mathf.Lerp(targetSpeedMultiplier, 1f, Time.deltaTime);
        }
    }
    
    void UpdatePlanetSpeeds()
    {
        // Smooth the current multiplier
        currentSpeedMultiplier = Mathf.Lerp(currentSpeedMultiplier, targetSpeedMultiplier, smoothingSpeed * Time.deltaTime);
        
        // Apply speed multiplier to all planets
        for (int i = 0; i < allPlanets.Length; i++)
        {
            if (allPlanets[i] != null)
            {
                allPlanets[i].orbitSpeed = originalSpeeds[i] * currentSpeedMultiplier;
            }
        }
    }
}