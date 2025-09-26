using UnityEngine;

public class ZipperDragSystem : MonoBehaviour
{
    [Header("Zipper Drag Settings")]
    public float dragThreshold = 0.5f;
    public float zipperSpeed = 2f;
    
    private Camera playerCamera;
    private bool isDragging = false;
    private GameObject draggedZipper = null;
    private SimplePlayer targetPlayer = null;
    private Vector3 startMousePos;
    private Vector3 zipperStartPos;
    private float dragDistance = 0f;
    
    void Start()
    {
        playerCamera = Camera.main;
        if (playerCamera == null)
        {
            playerCamera = FindObjectOfType<Camera>();
        }
    }
    
    void Update()
    {
        HandleZipperDrag();
    }
    
    void HandleZipperDrag()
    {
        if (Input.GetMouseButtonDown(0))
        {
            StartZipperDrag();
        }
        else if (Input.GetMouseButton(0) && isDragging)
        {
            ContinueZipperDrag();
        }
        else if (Input.GetMouseButtonUp(0) && isDragging)
        {
            EndZipperDrag();
        }
    }
    
    void StartZipperDrag()
    {
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, 50f))
        {
            // Check if clicked on zipper mouth
            if (hit.collider.name == "ZipperMouth" || hit.collider.transform.parent?.name == "ZipperMouth")
            {
                GameObject zipperMouth = hit.collider.name == "ZipperMouth" ? hit.collider.gameObject : hit.collider.transform.parent.gameObject;
                SimplePlayer player = zipperMouth.GetComponentInParent<SimplePlayer>();
                
                if (player != null)
                {
                    isDragging = true;
                    draggedZipper = zipperMouth;
                    targetPlayer = player;
                    startMousePos = Input.mousePosition;
                    dragDistance = 0f;
                    
                    Transform zipperPull = zipperMouth.transform.Find("ZipperPull");
                    if (zipperPull != null)
                    {
                        zipperStartPos = zipperPull.localPosition;
                    }
                    
                    Debug.Log("Started dragging zipper on " + player.playerName);
                    
                    // Visual feedback
                    ShowZipperDragFeedback(true);
                }
            }
            // Check if clicked on normal mouth to unmute
            else if (hit.collider.name == "NormalMouth" || hit.collider.transform.parent?.name == "NormalMouth")
            {
                GameObject normalMouth = hit.collider.name == "NormalMouth" ? hit.collider.gameObject : hit.collider.transform.parent.gameObject;
                SimplePlayer player = normalMouth.GetComponentInParent<SimplePlayer>();
                
                if (player != null && !player.isMuted)
                {
                    player.ToggleHandRaised();
                }
            }
        }
    }
    
    void ContinueZipperDrag()
    {
        if (draggedZipper != null && targetPlayer != null)
        {
            // Calculate drag distance
            Vector3 currentMousePos = Input.mousePosition;
            Vector3 mouseDelta = currentMousePos - startMousePos;
            
            // Convert to world space drag distance
            dragDistance = mouseDelta.magnitude / Screen.height * 2f; // Normalize to screen size
            
            // Move zipper pull based on drag
            Transform zipperPull = draggedZipper.transform.Find("ZipperPull");
            if (zipperPull != null)
            {
                float pullMovement = Mathf.Clamp(dragDistance, 0f, 1f);
                Vector3 newPos = zipperStartPos;
                newPos.x += pullMovement * 0.8f; // Move zipper pull to the right
                zipperPull.localPosition = newPos;
            }
            
            // Visual feedback based on drag progress
            float progress = Mathf.Clamp01(dragDistance / dragThreshold);
            UpdateZipperDragVisuals(progress);
        }
    }
    
    void EndZipperDrag()
    {
        if (draggedZipper != null && targetPlayer != null)
        {
            // Check if dragged far enough to trigger mute
            if (dragDistance >= dragThreshold)
            {
                if (!targetPlayer.isMuted)
                {
                    // Mute the player
                    targetPlayer.SetMuted(true);
                    CompleteZipperAnimation();
                    Debug.Log("Zipped " + targetPlayer.playerName + "'s mouth!");
                }
                else
                {
                    // Unmute the player
                    targetPlayer.SetMuted(false);
                    CompleteUnzipperAnimation();
                    Debug.Log("Unzipped " + targetPlayer.playerName + "'s mouth!");
                }
            }
            else
            {
                // Reset zipper position if didn't drag far enough
                ResetZipperPosition();
            }
            
            ShowZipperDragFeedback(false);
        }
        
        // Reset drag state
        isDragging = false;
        draggedZipper = null;
        targetPlayer = null;
        dragDistance = 0f;
    }
    
    void UpdateZipperDragVisuals(float progress)
    {
        if (draggedZipper != null)
        {
            // Change zipper color based on progress
            Renderer zipperRenderer = draggedZipper.GetComponent<Renderer>();
            if (zipperRenderer != null)
            {
                Color baseColor = Color.gray;
                if (progress > 0.7f)
                {
                    baseColor = Color.red; // Almost ready to mute
                }
                else if (progress > 0.3f)
                {
                    baseColor = Color.yellow; // Getting there
                }
                
                zipperRenderer.material.color = baseColor;
            }
        }
    }
    
    void ShowZipperDragFeedback(bool show)
    {
        if (targetPlayer != null)
        {
            // Add glow effect to the player being targeted
            Renderer[] renderers = targetPlayer.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                if (show)
                {
                    renderer.material.color = Color.Lerp(renderer.material.color, Color.cyan, 0.3f);
                }
                else
                {
                    // Reset color based on player state
                    Material targetMaterial = targetPlayer.isMuted ? targetPlayer.mutedMaterial : targetPlayer.normalMaterial;
                    if (targetMaterial != null)
                    {
                        renderer.material = targetMaterial;
                    }
                }
            }
        }
    }
    
    void CompleteZipperAnimation()
    {
        if (draggedZipper != null)
        {
            StartCoroutine(AnimateZipperClose());
        }
    }
    
    void CompleteUnzipperAnimation()
    {
        if (draggedZipper != null)
        {
            StartCoroutine(AnimateZipperOpen());
        }
    }
    
    void ResetZipperPosition()
    {
        if (draggedZipper != null)
        {
            Transform zipperPull = draggedZipper.transform.Find("ZipperPull");
            if (zipperPull != null)
            {
                StartCoroutine(AnimateZipperReset(zipperPull));
            }
        }
    }
    
    System.Collections.IEnumerator AnimateZipperClose()
    {
        Transform zipperPull = draggedZipper.transform.Find("ZipperPull");
        if (zipperPull != null)
        {
            Vector3 startPos = zipperPull.localPosition;
            Vector3 endPos = zipperStartPos + Vector3.right * 0.8f;
            
            float duration = 0.3f;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                
                zipperPull.localPosition = Vector3.Lerp(startPos, endPos, progress);
                yield return null;
            }
        }
        
        // Show completion effect
        if (targetPlayer != null)
        {
            ShowFloatingText("ZIPPED!", Color.red);
        }
    }
    
    System.Collections.IEnumerator AnimateZipperOpen()
    {
        Transform zipperPull = draggedZipper.transform.Find("ZipperPull");
        if (zipperPull != null)
        {
            Vector3 startPos = zipperPull.localPosition;
            Vector3 endPos = zipperStartPos;
            
            float duration = 0.3f;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                
                zipperPull.localPosition = Vector3.Lerp(startPos, endPos, progress);
                yield return null;
            }
        }
        
        // Show completion effect
        if (targetPlayer != null)
        {
            ShowFloatingText("UNZIPPED!", Color.green);
        }
    }
    
    System.Collections.IEnumerator AnimateZipperReset(Transform zipperPull)
    {
        Vector3 startPos = zipperPull.localPosition;
        Vector3 endPos = zipperStartPos;
        
        float duration = 0.2f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            
            zipperPull.localPosition = Vector3.Lerp(startPos, endPos, progress);
            yield return null;
        }
    }
    
    void ShowFloatingText(string text, Color color)
    {
        if (targetPlayer != null)
        {
            GameObject floatingTextObj = new GameObject("FloatingText");
            floatingTextObj.transform.position = targetPlayer.transform.position + Vector3.up * 2.5f;
            
            TextMesh floatingText = floatingTextObj.AddComponent<TextMesh>();
            floatingText.text = text;
            floatingText.fontSize = 15;
            floatingText.color = color;
            floatingText.anchor = TextAnchor.MiddleCenter;
            
            floatingTextObj.AddComponent<Billboard>();
            
            StartCoroutine(FloatingTextAnimation(floatingTextObj, 2f));
        }
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
}