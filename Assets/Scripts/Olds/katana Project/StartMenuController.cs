#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartMenuController : MonoBehaviour
{
    // 1) 게임 시작 버튼 클릭 시 호출
    public void OnClickStartButton()
    {
        // "GameScene"이라는 씬을 로드 (씬 이름은 프로젝트에 맞게 변경)
        SceneManager.LoadScene(1);
    }

    // 2) 게임 종료 버튼 클릭 시 호출
    public void OnClickQuitButton()
    {
        // 에디터 상에서 바로 종료가 되지 않을 수 있으므로,
        // 실제 빌드된 환경에서는 Application.Quit()가 동작함
        Application.Quit();

        // 참고) Unity Editor에서 확인하고 싶다면 아래 코드 사용:
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}
