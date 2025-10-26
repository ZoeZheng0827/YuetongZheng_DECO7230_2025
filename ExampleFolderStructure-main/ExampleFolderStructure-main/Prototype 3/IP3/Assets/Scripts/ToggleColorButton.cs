using UnityEngine;

public class ToggleColorButton : MonoBehaviour
{
    // Renderer whose material color will be toggled
    public Renderer targetRenderer;

    // Colors to toggle between
    public Color onColor = new Color(0.2f, 0.85f, 0.2f); // green
    public Color offColor = new Color(0.6f, 0.6f, 0.6f); // gray

    // Current state
    public bool isOn = true;

    private MaterialPropertyBlock _mpb;

    void Reset()
    {
        // Auto-assign renderer if not set
        if (!targetRenderer) targetRenderer = GetComponent<Renderer>();
    }

    void Awake()
    {
        if (!targetRenderer) targetRenderer = GetComponent<Renderer>();
        _mpb = new MaterialPropertyBlock();
        Apply();
    }

    // Called from Building Blocks interaction event (Poke / Ray / Select)
    public void Toggle()
    {
        isOn = !isOn;
        Apply();
    }

    private void Apply()
    {

        if(isOn){
            targetRenderer.material.color = onColor;
        }else{
             targetRenderer.material.color = offColor;
        }

        // targetRenderer.GetPropertyBlock(_mpb);
        // _mpb.SetColor("_Color", isOn ? onColor : offColor);
        // targetRenderer.SetPropertyBlock(_mpb);
    }
}
