using UnityEngine;
public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }
    
    public float zoomValue = 0.5f;
    public float volumeValue = 0.5f;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}


