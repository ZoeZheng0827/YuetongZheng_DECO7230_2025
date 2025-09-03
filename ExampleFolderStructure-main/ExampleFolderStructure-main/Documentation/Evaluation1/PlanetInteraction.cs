using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class PlanetInteraction : MonoBehaviour
{
    // ───────────────────────────── Targets ─────────────────────────────
    [Header("Targets")]
    [Tooltip("If left empty, Main Camera's transform will be used automatically.")]
    public Transform transitionTarget;

    [Tooltip("Optional explicit camera. If empty, will use Camera.main, then any Camera found.")]
    public Camera cam;

    // ───────────────────────────── Drag ─────────────────────────────
    [Header("Drag")]
    [Tooltip("Movement speed on the drag plane.")]
    public float dragSpeed = 10f;

    [Tooltip("Max radius around the camera while dragging.")]
    public float maxDragRadius = 25f;

    [Tooltip("Hold time before drag starts.")]
    public float minHoldTime = 0.15f;

    [Tooltip("Minimum pixel delta to start dragging.")]
    public float dragDeadzonePixels = 6f;

    // ────────────────────── Approach Toward Camera ──────────────────────
    [Header("Approach Toward Camera")]
    [Tooltip("Closest distance the object will approach while holding.")]
    public float minApproachDistance = 2.0f;

    [Tooltip("Rate at which the drag plane moves toward the camera.")]
    public float approachUnitsPerSec = 3.5f;

    [Tooltip("Acceleration applied to the approach for smoother feel.")]
    public float approachAccel = 2.0f;

    // ─────────────────────────── Visual Feedback ───────────────────────────
    [Header("Scaling Feedback")]
    [Tooltip("Max visual scale when close to the camera.")]
    public float maxScale = 8f;

    [Tooltip("Interpolation speed for scale changes.")]
    public float scaleLerp = 8f;

    // ───────────────────────────── Transition ─────────────────────────────
    [Header("Transition")]
    [Tooltip("Name of the scene to load when close enough (must be added to Build Settings).")]
    public string voiceChannelSceneName = "VoiceChannel";

    [Tooltip("Small delay used for a scale-out effect before loading.")]
    public float transitionDelay = 0.35f;

    [Tooltip("Distance threshold to trigger the scene transition.")]
    public float enterDistance = 3.0f;

    // ───────────────────────────── Debug ─────────────────────────────
    [Header("Debug")]
    [Tooltip("Show debug logs and gizmos.")]
    public bool showDebug = true;

    // ───────────────────────────── Internals ─────────────────────────────
    private GalaxyOrbit orbitScript;
    private StrongCollisionAvoidance collisionScript;

    private Vector3 originalScale;
    private bool isPointerDown = false;
    private bool isDragging = false;
    private bool isTransitioning = false;

    private float pointerDownTime;
    private Vector2 pointerDownScreenPos;

    private Plane dragPlane;
    private float initialDistanceToCam;
    private float currentPlaneDistance;
    private float approachVelocity;

    void Awake()
    {
        // Camera fallback: explicit -> Camera.main -> any Camera in scene
        if (cam == null) cam = Camera.main;
        if (cam == null) cam = FindAnyObjectByType<Camera>();
        if (cam == null)
        {
            Debug.LogError("[PlanetInteraction] No Camera found. " +
                           "Assign a Camera or tag your camera as MainCamera.", this);
            enabled = false;
            return;
        }

        if (transitionTarget == null)
            transitionTarget = cam.transform;
    }

    void Start()
    {
        orbitScript = GetComponent<GalaxyOrbit>();
        collisionScript = GetComponent<StrongCollisionAvoidance>();
        originalScale = transform.localScale;

        if (minApproachDistance > enterDistance && showDebug)
            Debug.LogWarning("[PlanetInteraction] minApproachDistance > enterDistance. " +
                             "Drag may never reach the enter threshold.", this);
    }

    void Update()
    {
        if (!enabled || isTransitioning) return;

        HandlePointer();

        // Scaling feedback & in-drag proximity check
        if (isDragging)
        {
            float d = Vector3.Distance(transform.position, transitionTarget.position);
            float reduction = Mathf.Max(0f, initialDistanceToCam - d);
            float normalized = Mathf.Clamp01(reduction / Mathf.Max(0.0001f, initialDistanceToCam * 0.8f));
            Vector3 targetScale = originalScale * Mathf.Lerp(1f, maxScale, normalized);
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, scaleLerp * Time.deltaTime);

            if (d <= enterDistance)
            {
                if (showDebug) Debug.Log($"[PlanetInteraction] Enter during drag. d={d:F2} <= {enterDistance}", this);
                StartTransition();
            }
        }
        else
        {
            // Relax back to original scale when not dragging
            transform.localScale = Vector3.Lerp(transform.localScale, originalScale, scaleLerp * Time.deltaTime);
        }
    }

    // ───────────────────────────── Input Handling ─────────────────────────────
    void HandlePointer()
    {
        // Begin pointer
        if (Input.GetMouseButtonDown(0) && !Input.GetMouseButton(1))
            TryPointerDown();

        // Promote to dragging after hold & pixel threshold
        if (isPointerDown && !isDragging && Input.GetMouseButton(0) && !Input.GetMouseButton(1))
            TryStartDraggingAfterHold();

        // While dragging, update plane & position
        if (isDragging && Input.GetMouseButton(0) && !Input.GetMouseButton(1))
        {
            UpdateApproachPlane();
            UpdateDragPosition();
        }

        // Release
        if (Input.GetMouseButtonUp(0))
        {
            if (isDragging) EndDrag();
            isPointerDown = false;
        }
    }

    void TryPointerDown()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity) && hit.transform == transform)
        {
            isPointerDown = true;
            pointerDownTime = Time.time;
            pointerDownScreenPos = Input.mousePosition;

            initialDistanceToCam = Vector3.Distance(transform.position, cam.transform.position);
            currentPlaneDistance = initialDistanceToCam;
            approachVelocity = 0f;

            // Drag plane faces the camera (stable ray intersection)
            dragPlane = new Plane(-cam.transform.forward,
                                  cam.transform.position + cam.transform.forward * currentPlaneDistance);
        }
    }

    void TryStartDraggingAfterHold()
    {
        float held = Time.time - pointerDownTime;
        float pixelDelta = Vector2.Distance(pointerDownScreenPos, (Vector2)Input.mousePosition);
        if (held >= minHoldTime && pixelDelta >= dragDeadzonePixels)
        {
            isDragging = true;
            if (orbitScript != null) orbitScript.enabled = false;          // stop orbit from snapping back
            if (collisionScript != null) collisionScript.enabled = false;  // stop avoidance from pushing away
        }
    }

    // Drag plane slides toward the camera over time (to let you bring the planet closer)
    void UpdateApproachPlane()
    {
        float targetDist = Mathf.Max(minApproachDistance, 0.5f);
        approachVelocity += approachAccel * Time.deltaTime;
        currentPlaneDistance = Mathf.MoveTowards(
            currentPlaneDistance, targetDist,
            approachUnitsPerSec * approachVelocity * Time.deltaTime);

        dragPlane = new Plane(-cam.transform.forward,
                              cam.transform.position + cam.transform.forward * currentPlaneDistance);
    }

    void UpdateDragPosition()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (dragPlane.Raycast(ray, out float enter))
        {
            Vector3 targetWorld = ray.GetPoint(enter);

            // Clamp to a radius around the camera so it doesn't fly away
            Vector3 dirFromCam = targetWorld - cam.transform.position;
            float dist = dirFromCam.magnitude;
            if (dist > maxDragRadius)
                targetWorld = cam.transform.position + dirFromCam.normalized * maxDragRadius;

            // Move along the plane toward the target
            transform.position = Vector3.MoveTowards(transform.position, targetWorld, dragSpeed * Time.deltaTime);

            // Gentle radial pull toward the camera (insurance to help cross the threshold)
            Vector3 camToPlanet = transform.position - cam.transform.position;
            float pull = 2.0f * Time.deltaTime; // tune 1.5–3.0 for stronger pull
            if (camToPlanet.magnitude > minApproachDistance)
            {
                transform.position -= camToPlanet.normalized * pull;
            }
        }
    }

    void EndDrag()
    {
        isDragging = false;

        // Check once more on release
        float d = Vector3.Distance(transform.position, transitionTarget.position);
        if (d <= enterDistance)
        {
            if (showDebug) Debug.Log($"[PlanetInteraction] Enter on release. d={d:F2} <= {enterDistance}", this);
            StartTransition();
        }
        else
        {
            if (showDebug) Debug.Log($"[PlanetInteraction] Not close enough on release. d={d:F2} > {enterDistance}", this);
            // Re-enable motion scripts so the planet returns to its orbit
            if (orbitScript != null) orbitScript.enabled = true;
            if (collisionScript != null) collisionScript.enabled = true;
        }
    }

    void StartTransition()
    {
        if (isTransitioning) return;
        isTransitioning = true;

        // Keep disabled so nothing pulls the planet away during the transition
        if (orbitScript != null) orbitScript.enabled = false;
        if (collisionScript != null) collisionScript.enabled = false;

        StartCoroutine(DoTransition());
    }

    IEnumerator DoTransition()
    {
        // Small scale-out effect before loading the next scene
        Vector3 s0 = transform.localScale;
        for (float t = 0; t < transitionDelay; t += Time.deltaTime)
        {
            float p = t / Mathf.Max(transitionDelay, 0.0001f);
            transform.localScale = Vector3.Lerp(s0, Vector3.zero, p);
            yield return null;
        }

        SceneManager.LoadScene(voiceChannelSceneName);
    }

    // Visualize the enter distance in Scene view when selected
    void OnDrawGizmosSelected()
    {
        if (!showDebug) return;

        Transform t = transitionTarget != null ? transitionTarget :
                      (Camera.main != null ? Camera.main.transform : null);
        if (t == null) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(t.position, enterDistance);
    }
}
