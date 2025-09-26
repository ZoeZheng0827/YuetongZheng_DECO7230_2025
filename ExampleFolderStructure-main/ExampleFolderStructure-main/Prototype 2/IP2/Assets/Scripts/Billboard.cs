using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Camera playerCamera;
    
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
        if (playerCamera != null)
        {
            transform.LookAt(transform.position + playerCamera.transform.rotation * Vector3.forward,
                           playerCamera.transform.rotation * Vector3.up);
        }
    }
}