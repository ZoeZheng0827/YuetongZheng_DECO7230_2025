using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;

public class VRFirstPersonController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 3f;
    public float rotationSpeed = 30f; // Snap turn degrees
    
    [Header("VR Settings")]
    public Transform cameraRig;
    public Camera vrCamera;
    
    [Header("Input Settings")]
    [Tooltip("Deadzone for controller input")]
    public float inputDeadzone = 0.2f;
    [Tooltip("Threshold for snap turning")]
    public float snapTurnThreshold = 0.8f;
    
    [Header("Sitting Settings")]
    [Tooltip("Radius used to search for a chair around the player")]
    public float interactionDistance = 5f;

    // Internal state
    private bool isSeated = false;
    private Chair currentChair;
    private Vector3 standingPosition;
    private Quaternion standingRotation;
    
    // VR input tracking
    private List<InputDevice> leftHandDevices = new List<InputDevice>();
    private List<InputDevice> rightHandDevices = new List<InputDevice>();
    private InputDevice leftHandDevice;
    private InputDevice rightHandDevice;
    
    // Input state tracking
    private bool previousTriggerState = false;
    private bool previousSnapTurnState = false;

    void Start()
    {
        // Get camera rig if not assigned
        if (cameraRig == null)
        {
            // Try to find XR Origin or Camera Rig
            cameraRig = transform.root;
            Debug.Log($"Camera Rig found: {cameraRig.name}");
        }
            
        if (vrCamera == null)
        {
            vrCamera = Camera.main;
            if (vrCamera == null)
                vrCamera = FindObjectOfType<Camera>();
            Debug.Log($"VR Camera found: {vrCamera.name}");
        }

        // Force update input devices at start
        InvokeRepeating("UpdateInputDevices", 0f, 1f);

        Debug.Log("VR First Person Controller Started - Use controller trigger near chairs to sit/stand");
        Debug.Log("Checking for XR devices...");
        
        // Check XR status
        Debug.Log($"XR enabled: {XRSettings.enabled}");
        Debug.Log($"XR device: {XRSettings.loadedDeviceName}");
    }

    void Update()
    {
        // Don't update input devices every frame, use InvokeRepeating instead
        // UpdateInputDevices();
        
        if (!isSeated)
        {
            HandleMovement();
            HandleRotation();
        }
        
        HandleSitting();
    }

    void UpdateInputDevices()
    {
        // Get left hand devices
        InputDevices.GetDevicesAtXRNode(XRNode.LeftHand, leftHandDevices);
        if (leftHandDevices.Count > 0)
        {
            leftHandDevice = leftHandDevices[0];
            Debug.Log($"Left hand device found: {leftHandDevice.name}");
        }
            
        // Get right hand devices
        InputDevices.GetDevicesAtXRNode(XRNode.RightHand, rightHandDevices);
        if (rightHandDevices.Count > 0)
        {
            rightHandDevice = rightHandDevices[0];
            Debug.Log($"Right hand device found: {rightHandDevice.name}");
        }
        
        // Debug all available devices
        var allDevices = new List<InputDevice>();
        InputDevices.GetDevices(allDevices);
        foreach (var device in allDevices)
        {
            Debug.Log($"Device: {device.name}, Node: {device.characteristics}");
        }
    }

    void HandleMovement()
    {
        Vector2 moveInput = Vector2.zero;
        bool inputFound = false;
        
        // Try to get movement input from left controller
        if (leftHandDevice.isValid)
        {
            if (leftHandDevice.TryGetFeatureValue(CommonUsages.primary2DAxis, out moveInput))
            {
                inputFound = true;
                Debug.Log($"Left controller input: {moveInput}");
            }
        }
        
        // If left controller doesn't work, try right controller
        if (!inputFound && rightHandDevice.isValid)
        {
            if (rightHandDevice.TryGetFeatureValue(CommonUsages.primary2DAxis, out moveInput))
            {
                inputFound = true;
                Debug.Log($"Right controller input: {moveInput}");
            }
        }
        
        // Try alternative input methods for simulators
        if (!inputFound)
        {
            // Try getting any device with primary2DAxis
            var allDevices = new List<InputDevice>();
            InputDevices.GetDevices(allDevices);
            
            foreach (var device in allDevices)
            {
                if (device.TryGetFeatureValue(CommonUsages.primary2DAxis, out moveInput))
                {
                    inputFound = true;
                    Debug.Log($"Alternative device input: {device.name}, Input: {moveInput}");
                    break;
                }
            }
        }
        
        // Apply deadzone
        if (moveInput.magnitude < inputDeadzone)
            moveInput = Vector2.zero;
        
        if (moveInput.magnitude > 0.1f)
        {
            // Calculate movement direction based on camera forward
            Vector3 cameraForward = vrCamera.transform.forward;
            Vector3 cameraRight = vrCamera.transform.right;
            
            // Remove Y component to keep movement on horizontal plane
            cameraForward.y = 0f;
            cameraRight.y = 0f;
            
            cameraForward.Normalize();
            cameraRight.Normalize();
            
            // Calculate movement direction
            Vector3 moveDirection = (cameraForward * moveInput.y + cameraRight * moveInput.x);
            
            // Apply movement to the camera rig position directly
            cameraRig.position += moveDirection * walkSpeed * Time.deltaTime;
            
            Debug.Log($"Moving: direction={moveDirection}, newPos={cameraRig.position}");
        }
    }

    void HandleRotation()
    {
        Vector2 rotateInput = Vector2.zero;
        
        // Get rotation input from right controller
        if (rightHandDevice.isValid)
        {
            rightHandDevice.TryGetFeatureValue(CommonUsages.primary2DAxis, out rotateInput);
        }
        
        // Snap turning to reduce motion sickness
        bool currentSnapTurnState = Mathf.Abs(rotateInput.x) > snapTurnThreshold;
        
        if (currentSnapTurnState && !previousSnapTurnState)
        {
            float rotationAmount = Mathf.Sign(rotateInput.x) * rotationSpeed;
            // Rotate around Y axis in world space
            cameraRig.Rotate(Vector3.up, rotationAmount, Space.World);
        }
        
        previousSnapTurnState = currentSnapTurnState;
    }

    void HandleSitting()
    {
        bool triggerPressed = false;
        
        // Check trigger on both controllers
        if (leftHandDevice.isValid)
        {
            bool leftTrigger;
            leftHandDevice.TryGetFeatureValue(CommonUsages.triggerButton, out leftTrigger);
            triggerPressed |= leftTrigger;
        }
        
        if (rightHandDevice.isValid)
        {
            bool rightTrigger;
            rightHandDevice.TryGetFeatureValue(CommonUsages.triggerButton, out rightTrigger);
            triggerPressed |= rightTrigger;
        }
        
        // Only act on trigger press (not hold)
        if (triggerPressed && !previousTriggerState)
        {
            if (isSeated)
                StandUp();
            else
                TryToSit();
        }
        
        previousTriggerState = triggerPressed;
    }

    void TryToSit()
    {
        Chair nearbyChair = FindNearbyChair();

        if (nearbyChair != null && !nearbyChair.IsOccupied())
        {
            SitDown(nearbyChair);
        }
        else if (nearbyChair != null && nearbyChair.IsOccupied())
        {
            Debug.Log("Chair is occupied!");
        }
        else
        {
            Debug.Log("No chair nearby");
        }
    }

    Chair FindNearbyChair()
    {
        // Use camera rig position for detection
        Vector3 searchPosition = cameraRig.position;
        
        var hits = Physics.OverlapSphere(
            searchPosition,
            interactionDistance,
            ~0,
            QueryTriggerInteraction.Ignore
        );

        Chair closest = null;
        float closestDist = float.MaxValue;

        foreach (var hit in hits)
        {
            var chair = hit.GetComponentInParent<Chair>();
            if (chair == null) continue;

            float distance = Vector3.Distance(searchPosition, chair.GetSitPosition());
            if (distance < closestDist)
            {
                closestDist = distance;
                closest = chair;
            }
        }

        if (closest != null)
            Debug.Log($"[ChairCheck] Nearest chair: {closest.name}, Distance: {closestDist:F2}");
        else
            Debug.Log("[ChairCheck] No chairs found in range");

        return closest;
    }

    void SitDown(Chair chair)
    {
        // Store standing position and rotation
        standingPosition = cameraRig.position;
        standingRotation = cameraRig.rotation;

        isSeated = true;
        currentChair = chair;

        // Move camera rig to chair position
        cameraRig.position = chair.GetSitPosition();
        cameraRig.rotation = chair.GetSitRotation();

        chair.SetOccupied(true);

        Debug.Log("Seated - Use controller trigger to stand up");
    }

    void StandUp()
    {
        if (currentChair != null)
        {
            currentChair.SetOccupied(false);
            currentChair = null;
        }

        isSeated = false;
        
        // Return to standing position
        cameraRig.position = standingPosition;
        cameraRig.rotation = standingRotation;

        Debug.Log("Standing - You can move now");
    }

    // Alternative: Keyboard fallback for testing in editor
    void HandleKeyboardFallback()
    {
        if (!Application.isEditor) return;
        
        // WASD movement for testing in editor
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        
        if (!isSeated && (Mathf.Abs(horizontal) > 0.1f || Mathf.Abs(vertical) > 0.1f))
        {
            Vector3 direction = cameraRig.right * horizontal + cameraRig.forward * vertical;
            direction.y = 0f;
            // Use position instead of Translate to avoid moving the world
            cameraRig.position += direction * walkSpeed * Time.deltaTime;
        }
        
        // F key for sitting (editor testing)
        if (Input.GetKeyDown(KeyCode.F))
        {
            if (isSeated)
                StandUp();
            else
                TryToSit();
        }
        
        // Q/E for rotation (editor testing)
        if (Input.GetKeyDown(KeyCode.Q))
        {
            // Use Transform.Rotate for local rotation
            cameraRig.Rotate(0, -rotationSpeed, 0, Space.Self);
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            cameraRig.Rotate(0, rotationSpeed, 0, Space.Self);
        }
    }

    void LateUpdate()
    {
        // Add keyboard fallback for editor testing
        HandleKeyboardFallback();
    }

    // Debug information
    void OnGUI()
    {
        if (!Application.isEditor) return;
        
        GUI.Box(new Rect(10, 10, 300, 120), "VR Controller Debug");
        GUI.Label(new Rect(15, 30, 290, 20), $"Left Device Valid: {leftHandDevice.isValid}");
        GUI.Label(new Rect(15, 50, 290, 20), $"Right Device Valid: {rightHandDevice.isValid}");
        GUI.Label(new Rect(15, 70, 290, 20), $"Is Seated: {isSeated}");
        GUI.Label(new Rect(15, 90, 290, 20), $"Current Chair: {(currentChair != null ? currentChair.name : "None")}");
        GUI.Label(new Rect(15, 110, 290, 20), "Editor: Use WASD + F key for testing");
    }

    // Gizmos for debugging interaction range
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 position = cameraRig != null ? cameraRig.position : transform.position;
        Gizmos.DrawWireSphere(position, interactionDistance);
    }
}