using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public GameObject shooter; // 총알을 발사한 적
    public GameObject target; // 총알이 향해야 하는 목표 (기본적으로는 null)
    public float speed;
    public string groundLayerName;

    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        groundLayerName="Ground";
    }

    void Update()
    {
        // 목표가 있을 때 목표를 향해 총알 이동
        if (target != null)
        {
            Vector2 direction = ((Vector2)target.transform.position - rb.position).normalized;
            rb.linearVelocity = direction * speed; // 목표를 향한 속도 설정
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        //1.
        // 유지 - 패링
        //2.
        // 파괴 - 슈터(슈터를 파괴하는 코드 추가)
        //3.
        // 파괴 - 플레이어, 벽, 바닥
        //4.
        // 통과 - 그 외 ex) 총알, 다른 슈터, 가구 등
        
        if (collision.CompareTag("Attack"))
        {// 1. 패링 성공
            Debug.Log("플레이어 패링!");
            // 현재 속도를 반대로 바꾸고, 목표를 슈터로 설정
            rb.linearVelocity = -rb.linearVelocity; // 방향 반전
            target = shooter; // 원래 슈터로 다시 목표 설정
        }
        else if (collision.gameObject == shooter)
        {// 2. 원래 발사한 슈터에 도달한 경우
            Debug.Log("원래슈터에게 돌아감: " + collision.gameObject.name);
            Destroy(gameObject); // 총알 파괴

            // 슈터에 피해를 주는경우 수 있는 로직 추가 
            Shooter enemy = collision.GetComponent<Shooter>();
            if (enemy != null)
            {
                enemy.Die();
                Debug.Log("슈터 데미지 받음!");
            }
        }
        else if(collision.CompareTag("Player") )
        {// 3. 주인공이 맞음
            Debug.Log("플레이어 총맞음 : "+ collision.gameObject.name);

            PlayerDie player = collision.GetComponent<PlayerDie>();

            player.Die();
        }

        else if(collision.gameObject.layer==LayerMask.NameToLayer(groundLayerName))
        {
            Destroy(gameObject); // 총알 파괴
        }
        else{}
    }
}
