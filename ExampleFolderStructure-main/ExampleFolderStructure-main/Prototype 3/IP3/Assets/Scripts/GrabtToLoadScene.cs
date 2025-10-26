using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using Oculus.Interaction;

/// <summary>
/// Detects when object is grabbed and moved in VR, then loads next scene.
/// Fixed to work with Meta Quest's Oculus Interaction SDK.
/// </summary>
public class GrabToLoadScene : MonoBehaviour
{
    [Header("Scene")]
    public string targetSceneName = "Scene 2";
    public float transitionDelay = 0.6f;

    [Header("Detection (movement based)")]
    public float detectionDelay = 10f;
    public float checkInterval = 0.05f;
    public float distanceThreshold = 0.25f;
    public float speedThreshold = 0.6f;
    public int requiredConsecutiveHits = 4;
    public bool ignoreY = true;

    [Header("Explosion Effect")]
    public int explosionParticleCount = 300;
    public Vector2 particleSizeRange = new Vector2(0.03f, 0.10f);
    public Vector2 particleSpeedRange = new Vector2(3f, 8f);
    public Gradient explosionColors;

    [Header("Debug")]
    public bool showDebugInfo = true;

    private bool hasTriggered = false;
    private bool canDetect = false;
    private bool isGrabbed = false;
    private float lastSampleTime;
    private Vector3 lastPos;
    private int hitStreak = 0;
    private Rigidbody rb;
    private Grabbable grabbable;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        grabbable = GetComponent<Grabbable>();
        
        if (grabbable != null)
        {
            // Subscribe to Meta Quest grab events
            grabbable.WhenPointerEventRaised += HandlePointerEvent;
            if (showDebugInfo) Debug.Log($"[GrabToLoadScene] Grabbable found and subscribed on {name}");
        }
        else
        {
            Debug.LogWarning($"[GrabToLoadScene] No Grabbable component found on {name}. Add Grabbable for VR interaction!");
        }

        lastPos = transform.position;
        lastSampleTime = Time.time;

        InitializeExplosionColors();
        StartCoroutine(EnableDetectionAfterDelay());
        
        if (showDebugInfo)
            Debug.Log($"[GrabToLoadScene] Movement detection ready on {name}. Detection starts after {detectionDelay:F1}s.");
    }

    void HandlePointerEvent(PointerEvent pointerEvent)
    {
        // Track when object is grabbed or released
        if (pointerEvent.Type == PointerEventType.Select)
        {
            isGrabbed = true;
            if (showDebugInfo) Debug.Log("[GrabToLoadScene] Object GRABBED");
        }
        else if (pointerEvent.Type == PointerEventType.Unselect)
        {
            isGrabbed = false;
            if (showDebugInfo) Debug.Log("[GrabToLoadScene] Object RELEASED");
        }
    }

    void InitializeExplosionColors()
    {
        if (explosionColors == null || explosionColors.colorKeys.Length == 0)
        {
            explosionColors = new Gradient();
            var ck = new GradientColorKey[2];
            ck[0] = new GradientColorKey(new Color(0.67f, 0f, 1f), 0f);
            ck[1] = new GradientColorKey(Color.black, 1f);
            var ak = new GradientAlphaKey[2];
            ak[0] = new GradientAlphaKey(1f, 0f);
            ak[1] = new GradientAlphaKey(1f, 1f);
            explosionColors.SetKeys(ck, ak);
        }
    }

    IEnumerator EnableDetectionAfterDelay()
    {
        yield return new WaitForSeconds(detectionDelay);
        canDetect = true;
        lastPos = transform.position;
        lastSampleTime = Time.time;
        hitStreak = 0;
        if (showDebugInfo) Debug.Log("[GrabToLoadScene] Detection ENABLED - grab and move the object!");
    }

    void Update()
    {
        if (!canDetect || hasTriggered) return;

        // Only check movement if object is currently grabbed
        if (!isGrabbed)
        {
            // Reset streak when not grabbed
            if (hitStreak > 0)
            {
                hitStreak = 0;
                if (showDebugInfo) Debug.Log("[GrabToLoadScene] Not grabbed - streak reset");
            }
            return;
        }

        if (Time.time - lastSampleTime < checkInterval) return;

        Vector3 current = transform.position;
        Vector3 a = current;
        Vector3 b = lastPos;

        if (ignoreY) { a.y = 0f; b.y = 0f; }
        float dist = Vector3.Distance(a, b);

        float speed = 0f;
        float dt = Time.time - lastSampleTime;
        if (dt > 0f) speed = dist / dt;

        bool movedEnough = (dist >= distanceThreshold) || (speed >= speedThreshold);

        if (movedEnough)
        {
            hitStreak++;
            if (showDebugInfo) 
                Debug.Log($"[GrabToLoadScene] Movement hit {hitStreak}/{requiredConsecutiveHits} (dist={dist:F3}m, speed={speed:F2}m/s)");
            
            if (hitStreak >= requiredConsecutiveHits)
            {
                Trigger();
            }
        }
        else
        {
            if (hitStreak > 0 && showDebugInfo) 
                Debug.Log("[GrabToLoadScene] Movement too slow - streak reset");
            hitStreak = 0;
        }

        lastPos = current;
        lastSampleTime = Time.time;
    }

    public void Trigger()
    {
        if (hasTriggered) return;
        hasTriggered = true;

        if (showDebugInfo) 
            Debug.Log($"[GrabToLoadScene] ✓ TRIGGERED! Loading '{targetSceneName}' in {transitionDelay:F2}s");
        
        SpawnExplosion();
        StartCoroutine(LoadAfterDelay());
    }

    IEnumerator LoadAfterDelay()
    {
        yield return new WaitForSeconds(transitionDelay);

        if (Application.CanStreamedLevelBeLoaded(targetSceneName))
        {
            if (showDebugInfo) Debug.Log($"[GrabToLoadScene] Loading scene: {targetSceneName}");
            SceneManager.LoadScene(targetSceneName);
        }
        else
        {
            Debug.LogError($"[GrabToLoadScene] Scene '{targetSceneName}' not found! Add it to Build Settings (File > Build Settings > Add Open Scenes)");
        }
    }

    void SpawnExplosion()
    {
        Vector3 center = transform.position;
        for (int i = 0; i < explosionParticleCount; i++)
        {
            GameObject p = GameObject.CreatePrimitive(PrimitiveType.Cube);
            p.name = $"ExplosionParticle_{i}";
            p.transform.position = center + Random.insideUnitSphere * 0.1f;

            float size = Random.Range(particleSizeRange.x, particleSizeRange.y);
            p.transform.localScale = Vector3.one * size;

            var r = p.GetComponent<Renderer>();
            if (r != null)
            {
                var mat = new Material(Shader.Find("Standard"));
                Color c = explosionColors.Evaluate(Random.Range(0f, 1f));
                c = Color.Lerp(c, Color.black, Random.Range(0.3f, 0.7f));
                mat.color = c;
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", c * 0.5f);
                r.material = mat;
            }

            var rbParticle = p.AddComponent<Rigidbody>();
            rbParticle.useGravity = false;
            Vector3 dir = Random.onUnitSphere;
            float speed = Random.Range(particleSpeedRange.x, particleSpeedRange.y);
            rbParticle.velocity = dir * speed;

            p.AddComponent<ExplosionParticleFader>();
        }
    }

    void OnDestroy()
    {
        if (grabbable != null)
        {
            grabbable.WhenPointerEventRaised -= HandlePointerEvent;
        }
    }
}

public class ExplosionParticleFader : MonoBehaviour
{
    private float lifetime = 1.4f;
    private float t;
    private Material mat;
    private Color start;

    void Start()
    {
        var rend = GetComponent<Renderer>();
        if (rend)
        {
            mat = rend.material;
            start = mat.color;
        }
    }

    void Update()
    {
        t += Time.deltaTime;
        float k = Mathf.Clamp01(t / lifetime);
        transform.localScale = Vector3.Lerp(transform.localScale, Vector3.zero, k);
        if (mat != null)
        {
            Color c = start;
            c.a = 1f - k;
            mat.color = c;
        }
        if (k >= 1f) Destroy(gameObject);
    }
}