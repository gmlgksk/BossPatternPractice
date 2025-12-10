using UnityEngine;
using System.Collections;

public class TimeController : MonoBehaviour
{
    [Header("슬로우 모션 설정")]
    [SerializeField] private float targetSlowScale = 0.3f;  // 최소 시간 배율
    [SerializeField] private float transitionTime = 0.5f;   // 점점 느려지거나 빨라지는 데 걸리는 시간
    [SerializeField] private float holdDuration = 1.5f;     // 느려진 상태 유지 시간 (실제 초 기준)

    private bool isSlowing = false;

    public void SlowTimeEffectSmooth()
    {
        if (!isSlowing)
            StartCoroutine(SlowMotionCoroutine());
    }

    private IEnumerator SlowMotionCoroutine()
    {
        isSlowing = true;
        float originalScale = Time.timeScale;

        // 1️⃣ 점점 느려지기
        float t = 0f;
        while (t < transitionTime)
        {
            t += Time.unscaledDeltaTime;
            Time.timeScale = Mathf.Lerp(originalScale, targetSlowScale, t / transitionTime);
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
            yield return null;
        }

        // 2️⃣ 느려진 상태 유지
        Time.timeScale = targetSlowScale;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
        yield return new WaitForSecondsRealtime(holdDuration);

        // 3️⃣ 원래 속도로 복귀
        t = 0f;
        while (t < transitionTime)
        {
            t += Time.unscaledDeltaTime;
            Time.timeScale = Mathf.Lerp(targetSlowScale, originalScale, t / transitionTime);
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
            yield return null;
        }

        Time.timeScale = originalScale;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        isSlowing = false;
    }
}