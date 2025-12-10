using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class Bootstrap : MonoBehaviour
{
    private static bool isInitialized = false;

    [SerializeField] private string skipSceneName = "Main Menu";
    [SerializeField] private string pauseCanvasPath = "Prefabs/PauseCanvas";

    void Awake()
    {
        if (isInitialized)
        {
            Destroy(gameObject);
            return;
        }

        isInitialized = true;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;

        CreatePauseCanvas();
        CreateEventSystemIfNeeded();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == skipSceneName) return;

        // ✅ 씬 전환 후에도 EventSystem 존재 확인
        if (EventSystem.current == null)
        {
            Debug.Log("[Bootstrap] 새 씬에서 EventSystem이 없어 다시 생성합니다.");
            CreateEventSystemIfNeeded();
        }
    }

    private void CreatePauseCanvas()
    {
        if (GameObject.Find("PauseCanvas") == null)
        {
            var prefab = Resources.Load<GameObject>(pauseCanvasPath);
            if (prefab != null)
            {
                var instance = Instantiate(prefab);
                instance.name = "PauseCanvas";
                DontDestroyOnLoad(instance);
            }
        }
    }

    private void CreateEventSystemIfNeeded()
    {
        if (EventSystem.current == null)
        {
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            DontDestroyOnLoad(es);
            Debug.Log("[Bootstrap] EventSystem 생성 완료");
        }
    }
}
