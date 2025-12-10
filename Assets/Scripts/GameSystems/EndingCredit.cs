using UnityEditor.SearchService;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EndingCredit : MonoBehaviour
{
    public float scrollSpeed     = 10f;
    public float scrollStartTime = 2f;
    public float scrollEndY      = 1000f;

    float elapsedTime = 0f;

    void Update()
    {
        CreditUP();
    }

    public void CreditUP()
    {
        // 시작 대기 시간
        if (elapsedTime < scrollStartTime)
        {
            elapsedTime += Time.deltaTime;
            return;
        }
        Debug.Log($"posY = {transform.position.y}, endY = {scrollEndY}");
        // 아직 목표 y에 도달하지 않았으면 위로 이동
        if (transform.position.y < scrollEndY)
        {
            transform.position += Vector3.up * scrollSpeed * Time.deltaTime;
        }
        else
            LoadFirstScene();
        // 도달했으면 아무 것도 안 함 (여기서 크레딧 종료 처리 넣어도 됨)
    }

    public void LoadFirstScene()
    {
        SceneManager.LoadScene(0);
    }
}
