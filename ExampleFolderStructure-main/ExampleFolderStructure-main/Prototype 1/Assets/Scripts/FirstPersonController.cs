using UnityEngine;

public class FirstPersonController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 3f;
    public float mouseSensitivity = 2f;

    [Header("View Angle Limits")]
    public float minLookAngle = -60f;
    public float maxLookAngle = 60f;

    [Header("Sitting Settings")]
    [Tooltip("Radius used to search for a chair around the player.")]
    public float interactionDistance = 5f;   // try 5-6 for easier testing
    public KeyCode sitKey = KeyCode.F;

    private Camera playerCamera;
    private float verticalRotation = 0f;
    private bool isSeated = false;
    private Chair currentChair;
    private Vector3 standingPosition;
    private Quaternion standingRotation;

    void Start()
    {
        playerCamera = GetComponentInChildren<Camera>();

        // optional: start unlocked so you can interact with UI
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Debug.Log("First Person Controller Started - Press F to sit/stand");
    }

    void Update()
    {
        if (!isSeated)
        {
            HandleMovement();
        }

        HandleMouseLook();
        HandleEscapeKey();
        HandleSitting();
    }

    void HandleMovement()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 direction = transform.right * horizontal + transform.forward * vertical;
        direction.y = 0f;

        transform.Translate(direction * walkSpeed * Time.deltaTime, Space.World);
    }

    void HandleMouseLook()
    {
        if (Cursor.lockState == CursorLockMode.Locked || Input.GetMouseButton(1))
        {
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

            transform.Rotate(Vector3.up * mouseX);

            verticalRotation -= mouseY;
            verticalRotation = Mathf.Clamp(verticalRotation, minLookAngle, maxLookAngle);
            playerCamera.transform.localEulerAngles = Vector3.right * verticalRotation;
        }
    }

    void HandleSitting()
    {
        if (Input.GetKeyDown(sitKey))
        {
            if (isSeated)
                StandUp();
            else
                TryToSit();
        }
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

    // SCHEME A: use OverlapSphere + sit point distance
    Chair FindNearbyChair()
    {
        var hits = Physics.OverlapSphere(
            transform.position,
            interactionDistance,
            ~0,
            QueryTriggerInteraction.Ignore
        );

        Chair closest = null;
        float closestDist = float.MaxValue;

        foreach (var h in hits)
        {
            var chair = h.GetComponentInParent<Chair>();
            if (chair == null) continue;

            float d = Vector3.Distance(transform.position, chair.GetSitPosition());
            if (d < closestDist)
            {
                closestDist = d;
                closest = chair;
            }
        }

        if (closest != null)
            Debug.Log($"[ChairCheck] nearest={closest.name}, dist={closestDist:F2}");
        else
            Debug.Log("[ChairCheck] none in sphere");

        return closest;
    }

    void SitDown(Chair chair)
    {
        standingPosition = transform.position;
        standingRotation = transform.rotation;

        isSeated = true;
        currentChair = chair;

        transform.position = chair.GetSitPosition();
        transform.rotation = chair.GetSitRotation();

        chair.SetOccupied(true);

        Debug.Log("Seated - Press F to stand up");
    }

    void StandUp()
    {
        if (currentChair != null)
        {
            currentChair.SetOccupied(false);
            currentChair = null;
        }

        isSeated = false;
        transform.position = standingPosition;
        transform.rotation = standingRotation;

        Debug.Log("Standing - You can move now");
    }

    void HandleEscapeKey()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        if (Input.GetMouseButtonDown(1))
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else if (Input.GetMouseButtonUp(1))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
