using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class Teleport : MonoBehaviour
{
    [Header("텔레포트 설정")]
    [SerializeField] private Transform greenRoom;

    [Header("캐릭터 설정 (자동)")]
    [SerializeField] private SpriteRenderer sr;
    [SerializeField] private Collider2D col;
    [SerializeField] private Rigidbody2D rb;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {

    }
    
    public IEnumerator TeleportTo(Vector2 point, float disappearTime)
    {
        // 1) 사라짐
        sr.enabled = false;          // 보스 스프라이트 숨김
        if (col != null) col.enabled = false; // 충돌도 잠시 끔
        rb.bodyType = RigidbodyType2D.Static;
        transform.position = greenRoom.position;
        // transform.position = teleportPoint[0].position;


        // 대기
        yield return new WaitForSeconds(disappearTime);

        // 2) 위치 이동
        transform.position = point;

        // 3) 다시 나타남
        rb.bodyType = RigidbodyType2D.Dynamic;
        sr.enabled = true;
        if (col != null) col.enabled = true;

        yield return null;
    }
}
