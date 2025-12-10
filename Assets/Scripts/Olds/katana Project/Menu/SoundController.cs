using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class SoundController : MonoBehaviour
{
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private AudioMixer mixer;
    [SerializeField] private float 로그기울기;
    [SerializeField] private float 전체음량감소량;

    private const string exposedParam = "BGMVolume"; // ✅ Mixer에 노출한 파라미터 이름

    void Start()
    {
        로그기울기=80f;
        전체음량감소량=10f;
        // 저장된 사운드 값으로 슬라이더 초기화
        volumeSlider.value = SettingsManager.Instance.volumeValue;

        // 초기 볼륨 적용
        ApplyVolume(volumeSlider.value);

        // 슬라이더 조작 시 적용 & 저장
        volumeSlider.onValueChanged.AddListener(value =>
        {
            ApplyVolume(value);
            SettingsManager.Instance.volumeValue = value;
        });

        // 씬 로드시 다시 적용
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void ApplyVolume(float value)
    {
        float volume = Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 로그기울기 - 전체음량감소량;
        mixer.SetFloat(exposedParam, volume);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyVolume(volumeSlider.value);
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}
