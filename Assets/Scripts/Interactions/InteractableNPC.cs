using UnityEngine;
using System.Collections;
public class InteractableNPC : MonoBehaviour, IInteractable
{
    [Header("말풍선")]
    public string[] dialogues;
    public GameObject speechBubble;
    public TMPro.TextMeshProUGUI textField;
    private int currentComentLine = 0;

    [Header("E키")]
    public EIndicator eIndicator;
    public GameObject eBubble;
    public float detectRadius = 2.0f;
    

    private Coroutine hideCoroutine; // 숨기기용 코루틴

    void Update()
    {
        Collider2D col = Physics2D.OverlapCircle(transform.position, detectRadius, LayerMask.GetMask("Player"));
        
        if (speechBubble.activeSelf)
        {
            eBubble.SetActive(false);
        }
        else if(col != null)
        {
            eIndicator.Show();
        }
        else
        {
            eIndicator.Hide();
        }
    }

    public void Interact()
    {
        if (speechBubble != null && textField != null)
        {
            speechBubble.SetActive(true);
            textField.text = dialogues[currentComentLine];

            currentComentLine = (currentComentLine + 1) % dialogues.Length;

            // 만약 이전에 코루틴이 실행 중이면 중지
            if (hideCoroutine != null)
                StopCoroutine(hideCoroutine);

            // 새로 코루틴 시작
            hideCoroutine = StartCoroutine(HideSpeechBubble());
        }
    }

    private IEnumerator HideSpeechBubble()
    {
        yield return new WaitForSeconds(1f); // 1초 기다림
        speechBubble.SetActive(false);
    }
}