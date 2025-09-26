using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneManagerScript : MonoBehaviour
{
    [Header("Scene Settings")]
    [Tooltip("Name of the main scene to return to")]
    public string mainSceneName = "SampleScene";
    
    [Header("Debug")]
    public bool showDebugInfo = true;

    void Start()
    {
        if (showDebugInfo)
        {
            Debug.Log($"[SceneManager] Current scene: {SceneManager.GetActiveScene().name}");
            Debug.Log($"[SceneManager] Ready to return to: {mainSceneName}");
        }
    }

    // This method will be called when the button is clicked
    public void ReturnToMainScene()
    {
        if (showDebugInfo)
            Debug.Log($"[SceneManager] Returning to main scene: {mainSceneName}");
        
        // Load the main scene
        SceneManager.LoadScene(mainSceneName);
    }
    
    // Alternative method if you know the scene index
    public void ReturnToMainSceneByIndex()
    {
        if (showDebugInfo)
            Debug.Log("[SceneManager] Returning to main scene by index (0)");
        
        // Load the first scene (usually index 0)
        SceneManager.LoadScene(0);
    }
    
    // Method to quit the application (useful for testing)
    public void QuitApplication()
    {
        if (showDebugInfo)
            Debug.Log("[SceneManager] Quitting application");
        
        // This works in builds, not in editor
        Application.Quit();
        
        // For editor testing
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}