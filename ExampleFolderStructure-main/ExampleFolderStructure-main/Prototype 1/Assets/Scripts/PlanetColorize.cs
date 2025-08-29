using UnityEngine;

public class PlanetColorizer : MonoBehaviour
{
    public Gradient colorRamp; // optional; leave empty to use random HSV

    void Start()
    {
        var mr = GetComponent<MeshRenderer>();
        if (mr != null && mr.sharedMaterial != null)
        {
            // clone to avoid editing the shared asset
            mr.material = new Material(mr.sharedMaterial);

            Color c = (colorRamp != null)
                ? colorRamp.Evaluate(Random.value)
                : Random.ColorHSV(0f, 1f, 0.6f, 1f, 0.5f, 1f);

            if (mr.material.HasProperty("_Color"))
                mr.material.color = c;                      // Built-in/Standard
            if (mr.material.HasProperty("_BaseColor"))
                mr.material.SetColor("_BaseColor", c);      // URP Lit
        }
    }
}
