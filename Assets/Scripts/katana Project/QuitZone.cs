using UnityEngine;
using UnityEngine.SceneManagement;

public class QuitZone : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("플레이어 감지됨! 씬 이동!");

            int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
            int totalScenes = SceneManager.sceneCountInBuildSettings;

            if (currentSceneIndex + 1 < totalScenes)
            {
                // 다음 씬이 존재하면 다음 씬으로 이동
                SceneManager.LoadScene(currentSceneIndex + 1);
            }
            else
            {
                // 다음 씬이 없으면 첫 번째 씬으로 돌아감
                SceneManager.LoadScene(0);
            }
        }
    }
}
