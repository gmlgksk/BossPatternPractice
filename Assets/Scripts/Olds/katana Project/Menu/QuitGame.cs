using UnityEngine;

public class QuitGame : MonoBehaviour
{
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
