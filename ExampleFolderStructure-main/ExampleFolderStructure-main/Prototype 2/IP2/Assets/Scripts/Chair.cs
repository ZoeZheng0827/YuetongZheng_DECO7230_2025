using UnityEngine;

[DisallowMultipleComponent]
public class Chair : MonoBehaviour
{
    [Header("Seat Settings")]
    [Tooltip("Optional: where the player should snap to when sitting. If null, a child will be created automatically.")]
    public Transform sitPoint;

    [SerializeField]
    private bool isOccupied = false;

    [Header("Auto Settings (fallback when sitPoint is null)")]
    [Tooltip("Vertical offset used when sitPoint is missing.")]
    public float sitHeight = 0.5f;

    [Tooltip("Local offset used when sitPoint is missing or auto-created.")]
    public Vector3 sitOffset = Vector3.zero;

    private void Reset()
    {
        EnsureCollider();
    }

    private void Awake()
    {
        EnsureSitPoint();
        EnsureCollider();
    }

    private void EnsureSitPoint()
    {
        if (sitPoint == null)
        {
            var go = new GameObject("SitPoint");
            go.transform.SetParent(transform, false);
            // important: lift by sitHeight so the snap point is not inside the mesh
            go.transform.localPosition = sitOffset + Vector3.up * sitHeight;
            go.transform.localRotation = Quaternion.identity;
            sitPoint = go.transform;
        }
    }

    private void EnsureCollider()
    {
        if (!TryGetComponent<Collider>(out _))
        {
            var col = gameObject.AddComponent<BoxCollider>();
            col.isTrigger = false;
        }
    }

    public Vector3 GetSitPosition()
    {
        return sitPoint != null
            ? sitPoint.position
            : transform.TransformPoint(sitOffset + Vector3.up * sitHeight);
    }

    public Quaternion GetSitRotation()
    {
        return sitPoint != null ? sitPoint.rotation : transform.rotation;
    }

    public bool IsOccupied() => isOccupied;

    public void SetOccupied(bool occupied)
    {
        isOccupied = occupied;
        UpdateVisualState();
    }

    private void UpdateVisualState()
    {
        // place to toggle materials/VFX if desired
        if (isOccupied)
            Debug.Log($"Chair '{name}' is occupied");
        else
            Debug.Log($"Chair '{name}' is free");
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = isOccupied ? Color.red : Color.green;

        var pos = (sitPoint != null)
            ? sitPoint.position
            : transform.TransformPoint(sitOffset + Vector3.up * sitHeight);

        Gizmos.DrawWireSphere(pos, 0.3f);

        Gizmos.color = Color.cyan;
        var rot = (sitPoint != null) ? sitPoint.rotation : transform.rotation;
        Gizmos.DrawRay(pos, rot * Vector3.forward * 0.6f);
    }
}
