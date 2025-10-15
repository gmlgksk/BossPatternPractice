using System.Collections;
using UnityEngine;

public class LinearMove : MonoBehaviour
{
    public IEnumerator MoveTo(Vector3 targetPos, AnimationCurve Y_Curve,float duration,float maxHeight)
    {
        Vector3 startPosition = transform.position;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration;

            // x,z 평면 이동 (선형 보간)
            Vector3 horizontal = Vector3.Lerp(
                startPosition,
                targetPos,
                t
            );

            // y축은 커브 기반
            float height = Y_Curve.Evaluate(t) * maxHeight;


            transform.position = new Vector3(horizontal.x, horizontal.y + height, horizontal.z);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // 목표 위치로 정확히 정렬
        transform.position = new Vector3(targetPos.x, targetPos.y, targetPos.z);
    }
}
