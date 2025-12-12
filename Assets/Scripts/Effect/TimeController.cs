using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class TimeFreezeController : MonoBehaviour
{
    [Header("시간 조절")]
    [SerializeField] private float slowTimeScale = 0.2f;
    [SerializeField] private float transitionSpeed = 2f;

    [Header("회색 효과")]
    [SerializeField] private Volume globalVolume;

    [Header("시간 정지 에너지")]
    [SerializeField] private float maxFreezeTime = 5f;         // 최대 지속 시간
    [SerializeField] private float freezeConsumeRate = 1f;     // 초당 소모량
    [SerializeField] private float freezeRecoverRate = 0.5f;   // 초당 회복량 (선택)

    private float currentFreezeTime;

    private float targetTimeScale = 1f;
    private float targetVolumeWeight = 0f;
    public Key slowKey = Key.LeftShift;

    private bool isSlowing = false;

    private void Awake()
    {
        currentFreezeTime = maxFreezeTime;
    }

    void Update()
    {
        bool shiftHeld = Keyboard.current[slowKey].isPressed;

        if (shiftHeld && currentFreezeTime > 0f)
        {
            ActivateTimeSlow();
            currentFreezeTime -= Time.unscaledDeltaTime * freezeConsumeRate;

            if (currentFreezeTime <= 0f)
            {
                currentFreezeTime = 0f;
                DeactivateTimeSlow(); // 강제 해제
            }
        }
        else
        {
            DeactivateTimeSlow();

            // 천천히 회복
            if (currentFreezeTime < maxFreezeTime)
                currentFreezeTime += Time.unscaledDeltaTime * freezeRecoverRate;

            if (currentFreezeTime > maxFreezeTime)
                currentFreezeTime = maxFreezeTime;
        }

        Time.timeScale = Mathf.Lerp(Time.timeScale, targetTimeScale, Time.unscaledDeltaTime * transitionSpeed);
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        if (globalVolume != null)
            globalVolume.weight = Mathf.Lerp(globalVolume.weight, targetVolumeWeight, Time.unscaledDeltaTime * transitionSpeed);
    }

    public void ActivateTimeSlow()
    {
        globalVolume.gameObject.SetActive(true);
        targetTimeScale = slowTimeScale;
        targetVolumeWeight = 1f;
        isSlowing = true;
    }

    public void DeactivateTimeSlow()
    {
        targetTimeScale = 1f;
        targetVolumeWeight = 0f;
        isSlowing = false;
    }

    public float GetCurrentFreezeRatio()
    {
        return currentFreezeTime / maxFreezeTime; // 게이지 UI용
    }
}
