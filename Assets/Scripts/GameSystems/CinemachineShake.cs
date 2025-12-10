using System.Collections;
using UnityEngine;
using Unity.Cinemachine;

public class CinemachineShake : MonoBehaviour
{
    public static CinemachineShake Instance { get; private set; }

    private CinemachineCamera cmCamera;
    private CinemachineBasicMultiChannelPerlin noise;
    private Coroutine currentShake;

    private void Awake()
    {
        // ===== 싱글톤 =====
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // ===== CinemachineCamera 찾기 =====
        cmCamera = GetComponent<CinemachineCamera>();
        if (cmCamera == null)
        {
            Debug.LogError("[CinemachineShake] CinemachineCamera 컴포넌트가 필요합니다.");
            enabled = false;
            return;
        }

        // ===== Noise 스테이지의 컴포넌트 가져오기 =====
        // Inspector에서 Noise → Basic Multi Channel Perlin 으로 설정해 둬야 여기서 캐스팅 가능
        CinemachineComponentBase noiseBase =
            cmCamera.GetCinemachineComponent(CinemachineCore.Stage.Noise);

        noise = noiseBase as CinemachineBasicMultiChannelPerlin;
        if (noise == null)
        {
            Debug.LogError(
                "[CinemachineShake] Noise 스테이지에 CinemachineBasicMultiChannelPerlin이 설정되어 있지 않습니다.\n" +
                "CinemachineCamera > Noise 에서 Basic Multi Channel Perlin을 선택해 주세요."
            );
            enabled = false;
            return;
        }
    }

    public void Shake(float duration, float amplitude, float frequency)
    {
        if (currentShake != null)
            StopCoroutine(currentShake);

        currentShake = StartCoroutine(ShakeRoutine(duration, amplitude, frequency));
    }

    private IEnumerator ShakeRoutine(float duration, float amplitude, float frequency)
    {
        float elapsed = 0f;

        // 시작할 때 일단 완전히 꺼둔다 (혹시라도 남아 있던 값 제거)
        noise.AmplitudeGain = 0f;
        noise.FrequencyGain = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float damper = 1f - t;

            noise.AmplitudeGain = amplitude * damper;
            noise.FrequencyGain = frequency;

            yield return null;
        }

        // ★ 항상 0으로 복귀 (기본 흔들림 없음)
        noise.AmplitudeGain = 0f;
        noise.FrequencyGain = 0f;
    }

}
