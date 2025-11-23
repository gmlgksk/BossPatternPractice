#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class Title_Manager : MonoBehaviour
{
    [Header("자식오브젝트")]
    GameObject buttons;
    Transform background;
    Transform title;

    [Header("버튼클릭")]
    bool ref_ClickStart;
    bool ref_ClickExit;

    [Header("타이틀 트랜지션 설정")]
    [SerializeField] float titleStartDelay = 1f;      // 타이틀 시작 지연 시간
    [SerializeField] float titleMoveTime = 2f;        // 타이틀 이동 시간
    [SerializeField] float titleMoveDistance = 100f;  // 타이틀 이동 거리
    [Header("배경 트랜지션 설정")]
    [SerializeField] float bgStartDelay = 0.2f;       // 배경 시작 지연 시간    
    [SerializeField] float bgDownTime = 1f;           // 배경 아래 이동 시간
    [SerializeField] float bgDownDistance = 50f;      // 배경 아래 이동 거리
    [SerializeField] float bgUpTime = 1f;             // 배경 위 이동 시간
    [SerializeField] float bgUpDistance = 200f;       // 배경 위 이동 거리
    [Header("씬 전환 설정")]
    [SerializeField] float sceneLoadDelay = 5f;       // 씬 전환 지연 시간

    bool isTransitioning = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        // buttons
        buttons     = transform.GetChild(0).gameObject;
        background  = transform.GetChild(1);
        title       = transform.GetChild(2);
    }

    // Update is called once per frame
    void Update()
    {
        // 전환 중이면 버튼 입력 무시
        if (isTransitioning) return;

        //버튼선택 감시
        ref_ClickStart = buttons.GetComponent<Title_Buttons>().start;
        ref_ClickExit  = buttons.GetComponent<Title_Buttons>().exit;

        //버튼마다 작동하는 코드
        if (ref_ClickStart)
        {
            StartGame();
        }
        if (ref_ClickExit)
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

    void StartGame()
    {
        isTransitioning = true;
        StartCoroutine(TitleTransition());
        StartCoroutine(BackgroundTransition());
        StartCoroutine(SceneLoadCoroutine());
    }

    IEnumerator TitleTransition()
    {
        // UI 숨기기
        buttons.SetActive(false);
        
        // 타이틀 위로 이동
        yield return new WaitForSeconds(titleStartDelay);
        yield return StartCoroutine(MoveObject(title, Vector3.up * titleMoveDistance, titleMoveTime));
        title.gameObject.SetActive(false);
    }

    IEnumerator BackgroundTransition()
    {
        // 배경 아래로 이동
        yield return new WaitForSeconds(bgStartDelay);
        yield return StartCoroutine(MoveObject(background, Vector3.down * bgDownDistance, bgDownTime));
        
        // 배경 위로 이동
        yield return StartCoroutine(MoveObject(background, Vector3.up * bgUpDistance, bgUpTime));
    }

    IEnumerator SceneLoadCoroutine()
    {
        yield return new WaitForSeconds(sceneLoadDelay);
        SceneManager.LoadScene(1);
    }

    IEnumerator MoveObject(Transform target, Vector3 offset, float duration)
    {
        Vector3 startPos = target.position;
        Vector3 endPos = startPos + offset;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            target.position = Vector3.Lerp(startPos, endPos, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }
        
        target.position = endPos;
    }
}
