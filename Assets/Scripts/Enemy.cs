using UnityEngine;
using UnityEngine.EventSystems;

public class Enemy : MonoBehaviour
{
    // 1. 순찰  :좌우로 움직임
    // 2. 추적  :플레이어 위치로 이동.일정거리까지 오면 중단.
    // 3. 공격  :
    [Header("움직임 요소")]
    Rigidbody2D rb;
    Animator anim;
    [SerializeField] bool moveOn;
    [SerializeField] int moveDir;
    [SerializeField] float moveSpeed;

    [Header("추적 세부사항")]
    [SerializeField] GameObject target;
    [SerializeField] bool findTarget;
    [SerializeField] float identifyTargetTime;
    [SerializeField] float targetDistance;



    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
    }


}
