using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ZoomController : MonoBehaviour
{
    [SerializeField] private Slider zoomSlider;

    void Start()
    {
        // 저장된 줌 값으로 슬라이더 초기화
        zoomSlider.value = SettingsManager.Instance.zoomValue;

        // 초기 카메라 줌 강제 적용
        ApplyZoom(zoomSlider.value);

        // 슬라이더 조작 시 카메라 줌 & 값 저장
        zoomSlider.onValueChanged.AddListener(value =>
        {
            ApplyZoom(value);
            SettingsManager.Instance.zoomValue = value;
        });

        // 씬이 바뀌었을 때 새 카메라에 줌 값 적용
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void ApplyZoom(float value)
    {
        if (Camera.main != null)
        {
            // 2 ~ 10 사이 값을 슬라이더(0~1)에 맞춰 보간해서 적용
            Camera.main.orthographicSize = Mathf.Lerp(2f, 10f, value);
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 새 씬이 로드되면 다시 카메라에 적용
        ApplyZoom(zoomSlider.value);
    }

    private void OnDestroy()
    {
        // 씬 이벤트 중복 방지
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}
