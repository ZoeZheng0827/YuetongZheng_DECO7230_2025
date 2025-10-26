using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Auto-arranges named “planet” objects in a ring and gives them a gentle vertical bobbing.
/// Works both in Play Mode and Edit Mode (ExecuteAlways), and is safe for Player builds.
/// </summary>
[ExecuteAlways]
public class PlanetAutoLayout : MonoBehaviour
{
    // Names exactly as they appear in the Hierarchy
    public string[] planetNames = new string[]
    {
        "[BuildingBlock] Jupiter",
        "[BuildingBlock] Saturn",
        "[BuildingBlock] Neptune",
        "[BuildingBlock] Uranus",
        "[BuildingBlock] Earth",
        "[BuildingBlock] Mars"
    };

    [Header("Layout around origin")]
    public Vector3 origin = Vector3.zero;

    [Tooltip("Base Y position for all planets (lifted above ground)")]
    public float yHeight = 4f;

    [Tooltip("Inner/outer radius of the ring from the origin")]
    public Vector2 radiusRange = new Vector2(10.80f, 15.85f);

    [Tooltip("Small random XZ offset so positions are not perfectly uniform")]
    public float planarJitter = 0.09f;

    [Header("Distinct sizes")]
    public float jupiterSize = 3.40f;
    public float saturnSize  = 3.20f;
    public float neptuneSize = 2.80f;
    public float uranusSize  = 2.60f;
    public float earthSize   = 2.40f;
    public float marsSize    = 2.20f;

    [Header("Bobbing (up-down floating)")]
    [Tooltip("Vertical bobbing amplitude")]
    public float bobAmplitude = 0.05f;
    [Tooltip("Vertical bobbing speed (radians per second)")]
    public float bobSpeed = 0.8f;

    [Header("Safety")]
    [Tooltip("Minimum angle separation (degrees) applied as random jitter")]
    public float minAngleDeg = 12f;

    [Tooltip("Temporarily set rigidbodies to kinematic while moving to avoid physics kicks")]
    public bool temporarilyKinematic = true;

    // Internals
    Dictionary<string, float> sizeByName;
    readonly List<Transform> placed = new List<Transform>();
    readonly List<float> baseY = new List<float>();
    readonly List<float> phase = new List<float>();

    void OnEnable()
    {
        BuildSizeMap();
    }

    void Start()
    {
        ApplyLayout();
    }

#if UNITY_EDITOR
    // Handy context menu to re-layout in the editor
    [ContextMenu("Apply Layout")]
    void ContextApply() => ApplyLayout();
#endif

    void Update()
    {
        // Animate only if we have placed objects
        if (placed.Count == 0) return;

        // Use Time.time in Play Mode; use realtimeSinceStartup in Edit Mode (build-safe).
        float t = Application.isPlaying ? Time.time : Time.realtimeSinceStartup;

        for (int i = 0; i < placed.Count; i++)
        {
            Transform tr = placed[i];
            if (!tr) continue;

            Vector3 p = tr.position;
            p.y = baseY[i] + Mathf.Sin(t * bobSpeed + phase[i]) * bobAmplitude;
            tr.position = p;
        }
    }

    void BuildSizeMap()
    {
        sizeByName = new Dictionary<string, float>()
        {
            { "[BuildingBlock] Jupiter", jupiterSize },
            { "[BuildingBlock] Saturn",  saturnSize  },
            { "[BuildingBlock] Neptune", neptuneSize },
            { "[BuildingBlock] Uranus",  uranusSize  },
            { "[BuildingBlock] Earth",   earthSize   },
            { "[BuildingBlock] Mars",    marsSize    }
        };
    }

    void ApplyLayout()
    {
        placed.Clear();
        baseY.Clear();
        phase.Clear();

        if (planetNames == null || planetNames.Length == 0) return;
        BuildSizeMap();

        int n = planetNames.Length;
        float baseStep = 360f / Mathf.Max(1, n);

        for (int i = 0; i < n; i++)
        {
            string name = planetNames[i];
            GameObject go = GameObject.Find(name);
            if (go == null) continue;

            // Distribute around a circle with small angular jitter
            float angleDeg = i * baseStep + Random.Range(-minAngleDeg * 0.25f, minAngleDeg * 0.25f);
            float angleRad = angleDeg * Mathf.Deg2Rad;

            // Interpolate radius across the list (inner -> outer)
            float t = (n == 1) ? 0f : (float)i / (n - 1);
            float radius = Mathf.Lerp(radiusRange.x, radiusRange.y, t);

            // Position on XZ plane plus small planar jitter
            Vector3 pos = new Vector3(
                origin.x + Mathf.Cos(angleRad) * radius + Random.Range(-planarJitter, planarJitter),
                yHeight,
                origin.z + Mathf.Sin(angleRad) * radius + Random.Range(-planarJitter, planarJitter)
            );

            // Temporarily disable physics while teleporting
            Rigidbody rb = go.GetComponent<Rigidbody>();
            bool prevKinematic = false;
            if (rb && temporarilyKinematic)
            {
                prevKinematic = rb.isKinematic;
                rb.isKinematic = true;
            }

            go.transform.position = pos;

            // Apply distinct scale per planet name (fallback to earth-ish size)
            float size = sizeByName.TryGetValue(name, out var s) ? s : 0.24f;
            go.transform.localScale = Vector3.one * size;

            if (rb && temporarilyKinematic) rb.isKinematic = prevKinematic;

            // Track for bobbing
            placed.Add(go.transform);
            baseY.Add(pos.y);
            phase.Add(Random.Range(0f, Mathf.PI * 2f));
        }
    }
}
