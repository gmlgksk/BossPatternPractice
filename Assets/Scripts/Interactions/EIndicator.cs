using UnityEngine;
using DG.Tweening;

public class EIndicator : MonoBehaviour
{
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Vector2 originalPos;
    private bool isShowing = false;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();
        originalPos = rectTransform.anchoredPosition;

        // 처음엔 숨김
        canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }

    public void Show()
    {
        if (isShowing) return; // 중복 방지

        isShowing = true;
        gameObject.SetActive(true);
        canvasGroup.alpha = 0f;
        rectTransform.anchoredPosition = originalPos - new Vector2(0, 1f);

        canvasGroup.DOFade(1f, 0.8f).SetEase(Ease.OutQuad);
        rectTransform.DOAnchorPos(originalPos, 0.8f).SetEase(Ease.OutQuad);
    }

    public void Hide()
    {
        if (!isShowing) return;

        isShowing = false;
        var fade = canvasGroup.DOFade(0f, 0.2f).SetEase(Ease.InQuad);
        fade.OnKill(() => gameObject.SetActive(false)); // 안전하게 끄기
    }
}
