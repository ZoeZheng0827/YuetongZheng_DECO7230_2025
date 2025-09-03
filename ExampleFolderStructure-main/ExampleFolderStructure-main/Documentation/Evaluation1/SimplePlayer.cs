using UnityEngine;

public class SimplePlayer : MonoBehaviour
{
    [Header("Player State")]
    public bool isMuted = false;
    public bool isHandRaised = false;
    public string playerName = "Player";
    
    [Header("Visual Components")]
    public GameObject body;
    public GameObject head;
    public GameObject normalMouth;
    public GameObject zipperMouth;
    public GameObject raisedHand;
    public GameObject nameTag;
    
    [Header("Materials")]
    public Material normalMaterial;
    public Material mutedMaterial;
    public Material talkingMaterial;
    
    private Renderer bodyRenderer;
    private Renderer headRenderer;
    private TextMesh nameText;
    private bool isTalking = false;
    
    void Start()
    {
        SetupPlayer();
        UpdateVisualState();
    }
    
    void SetupPlayer()
    {
        // Create body if not assigned
        if (body == null)
        {
            body = CreateBodyPart("Body", PrimitiveType.Capsule, Vector3.zero, new Vector3(0.8f, 1f, 0.8f));
        }
        
        // Create head if not assigned
        if (head == null)
        {
            head = CreateBodyPart("Head", PrimitiveType.Sphere, new Vector3(0, 1f, 0), Vector3.one * 0.4f);
        }
        
        // Create normal mouth
        if (normalMouth == null)
        {
            normalMouth = CreateMouth("NormalMouth", new Vector3(0, 0.9f, 0.18f), Color.black);
        }
        
        // Create zipper mouth
        if (zipperMouth == null)
        {
            zipperMouth = CreateZipperMouth("ZipperMouth", new Vector3(0, 0.9f, 0.18f));
            zipperMouth.SetActive(false);
        }
        
        // Create raised hand
        if (raisedHand == null)
        {
            raisedHand = CreateBodyPart("RaisedHand", PrimitiveType.Cube, new Vector3(0.5f, 1.5f, 0), Vector3.one * 0.15f);
            raisedHand.SetActive(false);
        }
        
        // Create name tag
        CreateNameTag();
        
        // Get renderers
        bodyRenderer = body.GetComponent<Renderer>();
        headRenderer = head.GetComponent<Renderer>();
        
        // Set default materials
        if (normalMaterial == null)
        {
            normalMaterial = CreateMaterial(Color.gray);
        }
        if (mutedMaterial == null)
        {
            mutedMaterial = CreateMaterial(Color.gray);
        }
        if (talkingMaterial == null)
        {
            talkingMaterial = CreateMaterial(Color.green);
        }
    }
    
    GameObject CreateBodyPart(string name, PrimitiveType type, Vector3 localPos, Vector3 scale)
    {
        GameObject part = GameObject.CreatePrimitive(type);
        part.name = name;
        part.transform.SetParent(transform);
        part.transform.localPosition = localPos;
        part.transform.localScale = scale;
        return part;
    }
    
    GameObject CreateMouth(string name, Vector3 localPos, Color color)
    {
        GameObject mouth = GameObject.CreatePrimitive(PrimitiveType.Cube);
        mouth.name = name;
        mouth.transform.SetParent(head.transform);
        mouth.transform.localPosition = localPos;
        mouth.transform.localScale = new Vector3(0.3f, 0.1f, 0.05f);
        
        Renderer renderer = mouth.GetComponent<Renderer>();
        renderer.material = CreateMaterial(color);
        
        return mouth;
    }
    
    GameObject CreateZipperMouth(string name, Vector3 localPos)
    {
        // Create zipper base
        GameObject zipperBase = CreateMouth(name, localPos, Color.gray);
        
        // Make zipper mouth more visible
        zipperBase.transform.localScale = new Vector3(0.4f, 0.15f, 0.1f);
        
        // Create zipper line (teeth)
        GameObject zipperLine = GameObject.CreatePrimitive(PrimitiveType.Cube);
        zipperLine.name = "ZipperLine";
        zipperLine.transform.SetParent(zipperBase.transform);
        zipperLine.transform.localPosition = Vector3.zero;
        zipperLine.transform.localScale = new Vector3(0.8f, 0.3f, 1.2f);
        
        Renderer lineRenderer = zipperLine.GetComponent<Renderer>();
        lineRenderer.material = CreateMaterial(Color.black);
        
        // Create zipper pull (more visible)
        GameObject zipperPull = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        zipperPull.name = "ZipperPull";
        zipperPull.transform.SetParent(zipperBase.transform);
        zipperPull.transform.localPosition = new Vector3(0.3f, 0, 0);
        zipperPull.transform.localScale = Vector3.one * 0.8f;
        
        Renderer pullRenderer = zipperPull.GetComponent<Renderer>();
        pullRenderer.material = CreateMaterial(Color.yellow);
        
        // Add collider for clicking zipper
        BoxCollider zipperCollider = zipperBase.AddComponent<BoxCollider>();
        zipperCollider.isTrigger = true;
        zipperCollider.size = new Vector3(2f, 2f, 2f); // Larger click area
        
        return zipperBase;
    }
    
    void CreateNameTag()
    {
        GameObject nameTagObj = new GameObject("NameTag");
        nameTagObj.transform.SetParent(transform);
        nameTagObj.transform.localPosition = new Vector3(0, 2f, 0);
        
        nameText = nameTagObj.AddComponent<TextMesh>();
        nameText.text = playerName;
        nameText.fontSize = 6; // Much smaller font size
        nameText.color = Color.white;
        nameText.anchor = TextAnchor.MiddleCenter;
        nameText.alignment = TextAlignment.Center;
        
        nameTag = nameTagObj;
        
        // Make name tag always face camera
        nameTagObj.AddComponent<Billboard>();
    }
    
    Material CreateMaterial(Color color)
    {
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = color;
        return mat;
    }
    
    void Update()
    {
        // No more random flashing - characters only change when explicitly triggered
    }
    
    public void SetMuted(bool muted)
    {
        isMuted = muted;
        if (muted)
        {
            isTalking = false;
            isHandRaised = false;
        }
        UpdateVisualState();
        ShowMuteEffect(muted);
    }
    
    public void ToggleHandRaised()
    {
        if (!isMuted)
        {
            isHandRaised = !isHandRaised;
            if (isHandRaised)
            {
                StartTalking();
            }
            else
            {
                StopTalking();
            }
            UpdateVisualState();
        }
    }
    
    void StartTalking()
    {
        if (!isMuted)
        {
            isTalking = true;
            UpdateVisualState();
        }
    }
    
    void StopTalking()
    {
        isTalking = false;
        UpdateVisualState();
    }
    
    void UpdateVisualState()
    {
        // Update mouth
        if (normalMouth != null && zipperMouth != null)
        {
            normalMouth.SetActive(!isMuted);
            zipperMouth.SetActive(isMuted);
        }
        
        // Update hand
        if (raisedHand != null)
        {
            raisedHand.SetActive(isHandRaised);
        }
        
        // Update materials
        Material currentMaterial = normalMaterial;
        if (isMuted)
            currentMaterial = mutedMaterial;
        else if (isTalking)
            currentMaterial = talkingMaterial;
        
        if (bodyRenderer != null)
            bodyRenderer.material = currentMaterial;
        if (headRenderer != null)
            headRenderer.material = currentMaterial;
        
        // Update name tag color
        if (nameText != null)
        {
            if (isMuted)
                nameText.color = Color.red;
            else if (isTalking)
                nameText.color = Color.green;
            else
                nameText.color = Color.white;
        }
    }
    
    void ShowMuteEffect(bool muted)
    {
        if (muted)
        {
            // Create floating "MUTED" text
            ShowFloatingText("MUTED!", Color.red, 2f);
            
            // Make zipper animation
            StartCoroutine(ZipperAnimation());
        }
        else
        {
            ShowFloatingText("Unmuted", Color.green, 1.5f);
        }
    }
    
    void ShowFloatingText(string text, Color color, float duration)
    {
        GameObject floatingTextObj = new GameObject("FloatingText");
        floatingTextObj.transform.position = transform.position + Vector3.up * 2.5f;
        
        TextMesh floatingText = floatingTextObj.AddComponent<TextMesh>();
        floatingText.text = text;
        floatingText.fontSize = 15; // Smaller floating text too
        floatingText.color = color;
        floatingText.anchor = TextAnchor.MiddleCenter;
        
        floatingTextObj.AddComponent<Billboard>();
        
        // Animate floating text
        StartCoroutine(FloatingTextAnimation(floatingTextObj, duration));
    }
    
    System.Collections.IEnumerator FloatingTextAnimation(GameObject textObj, float duration)
    {
        Vector3 startPos = textObj.transform.position;
        Vector3 endPos = startPos + Vector3.up * 1f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            
            textObj.transform.position = Vector3.Lerp(startPos, endPos, progress);
            
            TextMesh text = textObj.GetComponent<TextMesh>();
            Color color = text.color;
            color.a = 1f - progress;
            text.color = color;
            
            yield return null;
        }
        
        Destroy(textObj);
    }
    
    System.Collections.IEnumerator ZipperAnimation()
    {
        if (zipperMouth != null)
        {
            Transform zipperPull = zipperMouth.transform.Find("ZipperPull");
            if (zipperPull != null)
            {
                Vector3 startPos = zipperPull.localPosition;
                Vector3 endPos = startPos + Vector3.right * 0.3f;
                
                float duration = 0.5f;
                float elapsed = 0f;
                
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float progress = elapsed / duration;
                    
                    zipperPull.localPosition = Vector3.Lerp(startPos, endPos, progress);
                    yield return null;
                }
            }
        }
    }
    
    void OnMouseDown()
    {
        if (!isMuted)
        {
            ToggleHandRaised();
        }
    }
}