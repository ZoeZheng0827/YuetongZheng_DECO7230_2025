using UnityEngine;

public class StrongCollisionAvoidance : MonoBehaviour
{
    [Header("Collision Prevention")]
    public float safeDistance = 6.0f;
    public float detectionRadius = 8.0f;
    public float avoidanceStrength = 15f;
    public float maxAvoidanceSpeed = 20f;
    
    private Vector3 currentAvoidanceForce;
    
    void Update()
    {
        ApplyCollisionPrevention();
    }
    
    void ApplyCollisionPrevention()
    {
        currentAvoidanceForce = Vector3.zero;
        
        // Find all objects with "Planet" tag
        GameObject[] allPlanets = GameObject.FindGameObjectsWithTag("Planet");
        
        foreach (GameObject otherPlanet in allPlanets)
        {
            if (otherPlanet == gameObject) continue;
            
            Vector3 directionToOther = otherPlanet.transform.position - transform.position;
            float distance = directionToOther.magnitude;
            
            if (distance < detectionRadius)
            {
                Vector3 avoidanceDirection = -directionToOther.normalized;
                float repulsionStrength = (detectionRadius - distance) / detectionRadius;
                repulsionStrength = Mathf.Pow(repulsionStrength, 2);
                
                currentAvoidanceForce += avoidanceDirection * repulsionStrength * avoidanceStrength;
            }
        }
        
        if (currentAvoidanceForce.magnitude > maxAvoidanceSpeed)
        {
            currentAvoidanceForce = currentAvoidanceForce.normalized * maxAvoidanceSpeed;
        }
        
        if (currentAvoidanceForce.magnitude > 0.1f)
        {
            transform.position += currentAvoidanceForce * Time.deltaTime;
        }
    }
}