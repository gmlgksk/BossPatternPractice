using System.Collections;
using UnityEngine;

public class BossPattern2 : MonoBehaviour
{
    [Header("점프 설정")]
    [SerializeField] private AnimationCurve jumpHeightCurve; // 예: 0→1→0 형태의 곡선
    [SerializeField] private float jumpDuration = 2.0f;
    [SerializeField] private float maxJumpHeight = 5.0f;

    [Header("이동 코스")]
    [SerializeField] private Transform[] movePoints; // 이동할 포인트들을 인스펙터에 지정

    private int currentPointIndex = 0;

    public void StartJumpPattern()
    {
        StartCoroutine(JumpCourseRoutine());
    }


    

    private IEnumerator JumpCourseRoutine()
    {

        // 모든 포인트를 순차적으로 점프 이동
        while (currentPointIndex < movePoints.Length)
        {
            Vector3 targetPos = movePoints[currentPointIndex].position;
            yield return StartCoroutine(JumpAttack(targetPos)); // 인자 있는 코루틴 정상 호출
            currentPointIndex++;

            // 착지 후 잠시 대기 (선택)
            yield return new WaitForSeconds(0.5f);
        }

        currentPointIndex = 0; // 다음 패턴 재시작을 위해 초기화
    }

    private IEnumerator JumpAttack(Vector3 targetPos)
    {
        Vector3 startPosition = transform.position;
        float elapsedTime = 0f;

        while (elapsedTime < jumpDuration)
        {
            float t = elapsedTime / jumpDuration;

            // x,z 평면 이동 (선형 보간)
            Vector3 horizontal = Vector3.Lerp(
                startPosition,
                new Vector3(targetPos.x, startPosition.y, targetPos.z),
                t
            );

            // y축은 커브 기반
            float height = jumpHeightCurve.Evaluate(t) * maxJumpHeight;
            transform.position = new Vector3(horizontal.x, startPosition.y + height, horizontal.z);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // 목표 위치로 정확히 정렬
        transform.position = new Vector3(targetPos.x, startPosition.y, targetPos.z);
    }
}
