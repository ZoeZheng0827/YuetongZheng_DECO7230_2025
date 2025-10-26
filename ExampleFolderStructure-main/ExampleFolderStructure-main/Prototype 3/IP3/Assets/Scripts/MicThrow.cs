using UnityEngine;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;

public class MicThrow : MonoBehaviour
{
    [Header("Throw Settings")]
    [SerializeField] private float throwForceMultiplier = 1.5f;
    
    private Grabbable grabbable;
    private Rigidbody rb;
    private Vector3 lastPosition;
    private Vector3 velocity;

    void Start()
    {
        // Get components
        grabbable = GetComponent<Grabbable>();
        rb = GetComponent<Rigidbody>();
        
        // Check if components exist
        if (grabbable == null)
        {
            Debug.LogError("Grabbable component not found!");
            return;
        }
        
        if (rb == null)
        {
            Debug.LogError("Rigidbody component not found!");
            return;
        }
        
        // Subscribe to grab events
        grabbable.WhenPointerEventRaised += HandlePointerEvent;
        
        lastPosition = transform.position;
    }

    void HandlePointerEvent(PointerEvent pointerEvent)
    {
        // When released, throw the object
        if (pointerEvent.Type == PointerEventType.Unselect)
        {
            ThrowObject();
        }
    }

    void FixedUpdate()
    {
        // Track velocity while grabbed
        if (grabbable != null && grabbable.SelectingPointsCount > 0)
        {
            velocity = (transform.position - lastPosition) / Time.fixedDeltaTime;
            lastPosition = transform.position;
        }
    }

    void ThrowObject()
    {
        // Apply throw force
        if (rb != null)
        {
            rb.velocity = velocity * throwForceMultiplier;
            Debug.Log("Throw velocity: " + rb.velocity.magnitude);
        }
    }

    void OnDestroy()
    {
        // Unsubscribe from events
        if (grabbable != null)
        {
            grabbable.WhenPointerEventRaised -= HandlePointerEvent;
        }
    }
}