using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Complete microphone grab script with icon display
/// Just attach this single script to your Mic Building Block
/// </summary>
public class CompleteMicGrabScript : MonoBehaviour
{
    [Header("Icon Settings")]
    public float iconDistance = 1.5f; // Distance from camera to icon
    public float iconSize = 0.5f; // Icon scale size
    public float iconAlpha = 0.8f; // Icon transparency (0-1)
    public float fadeInDuration = 0.3f;
    public float fadeOutDuration = 0.5f;
    
    private Transform playerCamera;
    private GameObject micIconInstance;
    private bool isGrabbed = false;
    private bool wasGrabbed = false;
    private Coroutine fadeCoroutine;
    private Component grabbableComponent;
    
    void Start()
    {
        // Auto-find camera
        FindPlayerCamera();
        
        // Find grabbable component
        FindGrabbableComponent();
        
        // Create microphone icon
        CreateMicIcon();
        
        Debug.Log("Complete Mic Grab Script initialized on: " + gameObject.name);
    }
    
    void FindPlayerCamera()
    {
        if (playerCamera == null)
        {
            // Priority search for CenterEyeAnchor (common in Meta Quest)
            GameObject centerEye = GameObject.Find("CenterEyeAnchor");
            if (centerEye != null)
            {
                playerCamera = centerEye.transform;
                Debug.Log("Found CenterEyeAnchor camera");
                return;
            }
            
            // Search for main camera
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                playerCamera = mainCam.transform;
                Debug.Log("Found Main Camera");
                return;
            }
            
            // Search for any camera
            Camera anyCam = FindObjectOfType<Camera>();
            if (anyCam != null)
            {
                playerCamera = anyCam.transform;
                Debug.Log("Found Camera: " + anyCam.name);
                return;
            }
            
            Debug.LogError("No camera found! Please assign playerCamera manually.");
        }
    }
    
    void FindGrabbableComponent()
    {
        // Try different possible grabbable component names
        grabbableComponent = GetComponent("OVRGrabbable");
        if (grabbableComponent != null)
        {
            Debug.Log("Found OVRGrabbable component on " + gameObject.name);
            return;
        }
        
        grabbableComponent = GetComponent("Grabbable");
        if (grabbableComponent != null)
        {
            Debug.Log("Found Grabbable component on " + gameObject.name);
            return;
        }
        
        grabbableComponent = GetComponent("XRGrabbable");
        if (grabbableComponent != null)
        {
            Debug.Log("Found XRGrabbable component on " + gameObject.name);
            return;
        }
        
        // If no grabbable found, warn user
        Debug.LogWarning("No grabbable component found on " + gameObject.name + "! Make sure this object is grabbable.");
    }
    
    void CreateMicIcon()
    {
        // Create microphone icon directly
        micIconInstance = CreateMicrophoneIcon();
        
        // Set initial icon state
        micIconInstance.SetActive(false);
        SetIconTransparency(0f);
    }
    
    GameObject CreateMicrophoneIcon()
    {
        // Create Canvas
        GameObject canvasGO = new GameObject("MicIcon_Canvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 100; // Make sure it displays in front
        
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10f;
        
        // Create background circle
        GameObject bgGO = new GameObject("MicIcon_Background");
        bgGO.transform.SetParent(canvasGO.transform);
        
        Image bgImage = bgGO.AddComponent<Image>();
        RectTransform bgRect = bgGO.GetComponent<RectTransform>();
        bgRect.sizeDelta = new Vector2(120, 120);
        bgRect.localPosition = Vector3.zero;
        
        // Create circular background
        Texture2D bgTexture = CreateCircleTexture(60, Color.black);
        bgImage.sprite = Sprite.Create(bgTexture, new Rect(0, 0, 120, 120), new Vector2(0.5f, 0.5f));
        bgImage.color = new Color(0f, 0f, 0f, 0.3f); // Semi-transparent black background
        
        // Create microphone icon
        GameObject micGO = new GameObject("MicIcon_Microphone");
        micGO.transform.SetParent(canvasGO.transform);
        
        Image micImage = micGO.AddComponent<Image>();
        RectTransform micRect = micGO.GetComponent<RectTransform>();
        micRect.sizeDelta = new Vector2(80, 80);
        micRect.localPosition = Vector3.zero;
        
        // Create microphone shape texture
        Texture2D micTexture = CreateMicrophoneTexture();
        micImage.sprite = Sprite.Create(micTexture, new Rect(0, 0, micTexture.width, micTexture.height), new Vector2(0.5f, 0.5f));
        micImage.color = new Color(1f, 1f, 1f, iconAlpha);
        
        return canvasGO;
    }
    
    Texture2D CreateCircleTexture(int radius, Color color)
    {
        int diameter = radius * 2;
        Texture2D texture = new Texture2D(diameter, diameter, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[diameter * diameter];
        
        Vector2 center = new Vector2(radius, radius);
        
        for (int x = 0; x < diameter; x++)
        {
            for (int y = 0; y < diameter; y++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                if (distance <= radius)
                {
                    pixels[y * diameter + x] = color;
                }
                else
                {
                    pixels[y * diameter + x] = Color.clear;
                }
            }
        }
        
        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }
    
    Texture2D CreateMicrophoneTexture()
    {
        int width = 80;
        int height = 80;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[width * height];
        
        // Initialize to transparent
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.clear;
        }
        
        Color micColor = Color.white;
        
        // Draw microphone body (ellipse)
        Vector2 center = new Vector2(width * 0.5f, height * 0.6f);
        float radiusX = width * 0.25f;
        float radiusY = height * 0.35f;
        
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float dx = (x - center.x) / radiusX;
                float dy = (y - center.y) / radiusY;
                
                if (dx * dx + dy * dy <= 1.0f)
                {
                    pixels[y * width + x] = micColor;
                }
            }
        }
        
        // Draw microphone stand (rectangle)
        int standWidth = (int)(width * 0.1f);
        int standHeight = (int)(height * 0.25f);
        int standX = (width - standWidth) / 2;
        int standY = (int)(height * 0.1f);
        
        for (int x = standX; x < standX + standWidth; x++)
        {
            for (int y = standY; y < standY + standHeight; y++)
            {
                if (x >= 0 && x < width && y >= 0 && y < height)
                {
                    pixels[y * width + x] = micColor;
                }
            }
        }
        
        // Draw base (ellipse)
        Vector2 baseCenter = new Vector2(width * 0.5f, height * 0.15f);
        float baseRadiusX = width * 0.3f;
        float baseRadiusY = height * 0.08f;
        
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float dx = (x - baseCenter.x) / baseRadiusX;
                float dy = (y - baseCenter.y) / baseRadiusY;
                
                if (dx * dx + dy * dy <= 1.0f)
                {
                    pixels[y * width + x] = micColor;
                }
            }
        }
        
        // Draw microphone grille (horizontal lines)
        for (int i = 0; i < 3; i++)
        {
            int lineY = (int)(center.y - radiusY * 0.5f + i * radiusY * 0.33f);
            int lineStartX = (int)(center.x - radiusX * 0.7f);
            int lineEndX = (int)(center.x + radiusX * 0.7f);
            
            for (int x = lineStartX; x <= lineEndX; x++)
            {
                if (x >= 0 && x < width && lineY >= 0 && lineY < height)
                {
                    // Check if inside ellipse
                    float dx = (x - center.x) / radiusX;
                    float dy = (lineY - center.y) / radiusY;
                    if (dx * dx + dy * dy <= 0.8f) // Slightly smaller range
                    {
                        pixels[lineY * width + x] = new Color(0.3f, 0.3f, 0.3f, 1f);
                    }
                }
            }
        }
        
        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }
    
    void Update()
    {
        if (grabbableComponent == null) return;
        
        // Check grab state using reflection to be compatible with different grabbable types
        bool isCurrentlyGrabbed = IsObjectGrabbed();
        
        if (isCurrentlyGrabbed && !wasGrabbed)
        {
            // Just got grabbed
            OnObjectGrabbed();
        }
        else if (!isCurrentlyGrabbed && wasGrabbed)
        {
            // Just got released
            OnObjectReleased();
        }
        
        wasGrabbed = isCurrentlyGrabbed;
        
        // If icon is active and grabbed, continuously update position to follow camera
        if (isGrabbed && micIconInstance != null && micIconInstance.activeInHierarchy)
        {
            PositionIcon();
        }
    }
    
    bool IsObjectGrabbed()
    {
        if (grabbableComponent == null) return false;
        
        // Try to get the "isGrabbed" property using reflection
        var property = grabbableComponent.GetType().GetProperty("isGrabbed");
        if (property != null)
        {
            return (bool)property.GetValue(grabbableComponent);
        }
        
        // Try to get the "IsGrabbed" property
        property = grabbableComponent.GetType().GetProperty("IsGrabbed");
        if (property != null)
        {
            return (bool)property.GetValue(grabbableComponent);
        }
        
        // Try to get the "grabbed" field
        var field = grabbableComponent.GetType().GetField("grabbed");
        if (field != null)
        {
            return (bool)field.GetValue(grabbableComponent);
        }
        
        // If nothing found, return false
        return false;
    }
    
    private void OnObjectGrabbed()
    {
        Debug.Log("Mic grabbed! Showing icon...");
        if (!isGrabbed)
        {
            isGrabbed = true;
            ShowMicIcon();
        }
    }
    
    private void OnObjectReleased()
    {
        Debug.Log("Mic released! Hiding icon...");
        if (isGrabbed)
        {
            isGrabbed = false;
            HideMicIcon();
        }
    }
    
    void ShowMicIcon()
    {
        if (micIconInstance == null) return;
        
        // Stop previous animation
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);
            
        micIconInstance.SetActive(true);
        PositionIcon();
        
        // Fade in animation
        fadeCoroutine = StartCoroutine(FadeIcon(0f, iconAlpha, fadeInDuration));
    }
    
    void HideMicIcon()
    {
        if (micIconInstance == null) return;
        
        // Stop previous animation
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);
            
        // Fade out animation
        fadeCoroutine = StartCoroutine(FadeIconAndHide(iconAlpha, 0f, fadeOutDuration));
    }
    
    void PositionIcon()
    {
        if (micIconInstance == null || playerCamera == null) return;
        
        // Position icon in front of camera
        Vector3 iconPosition = playerCamera.position + playerCamera.forward * iconDistance;
        micIconInstance.transform.position = iconPosition;
        micIconInstance.transform.rotation = playerCamera.rotation;
        micIconInstance.transform.localScale = Vector3.one * iconSize;
    }
    
    void SetIconTransparency(float alpha)
    {
        if (micIconInstance == null) return;
        
        // Get all Image components and set transparency
        Image[] images = micIconInstance.GetComponentsInChildren<Image>();
        foreach (Image img in images)
        {
            Color color = img.color;
            color.a = alpha;
            img.color = color;
        }
        
        // If there are other UI components that also need transparency
        CanvasGroup canvasGroup = micIconInstance.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = alpha;
        }
    }
    
    IEnumerator FadeIcon(float fromAlpha, float toAlpha, float duration)
    {
        float elapsedTime = 0f;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float currentAlpha = Mathf.Lerp(fromAlpha, toAlpha, elapsedTime / duration);
            SetIconTransparency(currentAlpha);
            yield return null;
        }
        
        SetIconTransparency(toAlpha);
    }
    
    IEnumerator FadeIconAndHide(float fromAlpha, float toAlpha, float duration)
    {
        yield return StartCoroutine(FadeIcon(fromAlpha, toAlpha, duration));
        micIconInstance.SetActive(false);
    }
    
    void OnDestroy()
    {
        if (micIconInstance != null)
        {
            Destroy(micIconInstance);
        }
    }
}