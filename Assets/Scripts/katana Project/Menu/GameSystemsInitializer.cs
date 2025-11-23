using UnityEngine;
using UnityEngine.SceneManagement;

public class GameSystemsInitializer : MonoBehaviour
{
    private static GameSystemsInitializer instance;

    [SerializeField] private string skipSceneName = "StartScene"; // 첫 씬 이름
    [SerializeField] private GameObject pauseCanvasPrefab;

    private void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;

        InitializeManagers();
    }

    private void InitializeManagers()
    {
        // 매니저들을 자식으로 생성
        if (GetComponent<PauseController>() == null)
            gameObject.AddComponent<PauseController>();

        if (GetComponent<SoundController>() == null)
            gameObject.AddComponent<SoundController>();

        // 기타 매니저도 여기에 추가 가능
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == skipSceneName) return;

        // PauseCanvas가 이미 있으면 생성 안 함
        if (GameObject.Find("PauseCanvas") == null)
        {
            if (pauseCanvasPrefab == null)
                pauseCanvasPrefab = Resources.Load<GameObject>("Prefabs/PauseCanvas");

            if (pauseCanvasPrefab != null)
            {
                Instantiate(pauseCanvasPrefab);
            }
            else
            {
                Debug.LogWarning("PauseCanvas 프리팹을 Resources/Prefabs에 넣었는지 확인하세요.");
            }
        }
    }
}
