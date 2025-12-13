using System.Collections;
using System.Security.Cryptography;
using UnityEngine;

public class StageTransition : MonoBehaviour
{

    private CanvasGroup canvasGroup;
    public float duration = 1f;
    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    [ContextMenu("Fade Out 테스트")]
    public void FadeOut() 
    {
        StartCoroutine(FadeOutCorutine(duration));
    }
    public IEnumerator FadeOutCorutine(float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            canvasGroup.alpha = Mathf.Lerp(0, 1, t / duration);
            t += Time.deltaTime;
            yield return null;
        }
        canvasGroup.alpha = 1;
    }


    
}
