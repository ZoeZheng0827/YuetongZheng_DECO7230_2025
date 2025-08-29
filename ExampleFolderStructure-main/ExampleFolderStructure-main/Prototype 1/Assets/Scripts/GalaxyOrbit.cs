using UnityEngine;

public class GalaxyOrbit : MonoBehaviour
{
    [Header("Orbit Settings")]
    public float orbitRadius = 3f;
    public float orbitSpeed = 20f; // degrees per second
    public float verticalVariation = 1f;
    public int orbitLayer = 0; // Different layers for collision avoidance
    
    private float startAngle;
    private float currentAngle;
    private Vector3 orbitCenter;
    private float verticalOffset;
    
    void Start()
    {
        // Get the galaxy center position
        orbitCenter = transform.parent.position;
        
        // Set random starting angle to spread planets
        startAngle = Random.Range(0f, 360f);
        currentAngle = startAngle;
        
        // Add some vertical variation
        verticalOffset = Random.Range(-verticalVariation, verticalVariation);
        
        // Randomize orbit parameters slightly
        orbitRadius += Random.Range(-0.5f, 0.5f);
        orbitSpeed += Random.Range(-5f, 5f);
        
        // Set initial position
        UpdateOrbitPosition();
    }
    
    void Update()
    {
        // Update orbital angle
        currentAngle += orbitSpeed * Time.deltaTime;
        
        // Keep angle in 0-360 range
        if (currentAngle >= 360f) currentAngle -= 360f;
        if (currentAngle < 0f) currentAngle += 360f;
        
        UpdateOrbitPosition();
    }
    
    void UpdateOrbitPosition()
    {
        // Calculate position on circular orbit
        float radians = currentAngle * Mathf.Deg2Rad;
        
        Vector3 orbitPosition = new Vector3(
            Mathf.Cos(radians) * orbitRadius,
            verticalOffset,
            Mathf.Sin(radians) * orbitRadius
        );
        
        // Set world position relative to galaxy center
        transform.position = orbitCenter + orbitPosition;
    }
}