#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;

public class Title_Buttons : MonoBehaviour
{
    [Header("클릭 상태")]
    public bool start   = false;
    public bool exit    = false;
    // 1) 게임 시작 버튼 클릭 시 호출
    public void OnClickStartButton()
    {
        start = true;
    }

    // 2) 게임 종료 버튼 클릭 시 호출
    public void OnClickExitButton()
    {
        exit = true;
    }
}
