using UnityEngine;
using System.Collections;
public class Shooter : MonoBehaviour
{
    enum State { Idle, Shooting, Reloading, Chasing }
    private State currentState = State.Idle;

    public GameObject bulletPrefab;
    public Transform bulletSpawnPoint;
    public float bulletSpeed = 10f;
    public GameObject player;
    public float moveSpeed = 2f;

    private Animator anim;
    private Rigidbody2D rb;
    [SerializeField] private InSightCheck sightCheck;

    public bool isDead = false;
    private bool isLookingLeft = true; // 좌우 방향 저장 변수

    private Vector2 lastKnownPlayerPosition;
    private bool inSight => sightCheck.inMySight;


    [Header("감정")]
    private Transform watchYou;

    void Awake()
    {

        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        currentState = State.Idle;
        isLookingLeft = transform.localScale.x > 0 ? true : false;
        watchYou = transform.Find("BrainPoint").Find("Kill Mode");

        Debug.Log(rb.mass);  // Awake나 Update에서 체크

        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
        }
    }

    void FixedUpdate()
    {
        if (isDead) return;

        isLookingLeft = transform.localScale.x > 0;
        bool inSight = sightCheck.inMySight;

        // ✅ 플레이어 감지 중일 때 위치 저장
        if (inSight)
        {
            lastKnownPlayerPosition = player.transform.position;
        }

        switch (currentState)
        {
            case State.Idle:
                if (inSight)
                {
                    currentState = State.Shooting;
                    anim.SetBool("shoot", true);
                    EmotionOut(watchYou);
                }
                break;

            case State.Shooting:
                rb.linearVelocity = Vector2.zero;

                AnimatorStateInfo animInfo = anim.GetCurrentAnimatorStateInfo(0);
                if (animInfo.IsName("Enemy Shoot") && animInfo.normalizedTime < 1.0f)
                    break;

                if (!inSight)
                {
                    currentState = State.Chasing;
                    anim.SetBool("shoot", false);
                    anim.SetBool("walk", true);
                }
                break;

            case State.Chasing:
                if (inSight)
                {
                    currentState = State.Shooting;
                    MoveStop();
                    anim.SetBool("shoot", true);
                }
                else
                {
                    UpdateDirection();
                    FollowPlayer();
                }
                break;
        }

        // ✅ 디버그: 마지막 감지 위치로 라인 그리기
        Debug.DrawLine(bulletSpawnPoint.position, lastKnownPlayerPosition, Color.red);
    }

    // bool IsBackTurnedToPlayer()
    // {
    //     float dx = player.transform.position.x - transform.position.x;
    //     return (transform.localScale.x == 1 && dx > 0) ||
    //         (transform.localScale.x == -1 && dx < 0);
    // }
    void EmotionOut(Transform emotion) // 감정표현 허브. 추후 소리같은 요소 추가가 용이함.
    {
        StartCoroutine(ShowEmotion(emotion));
    }

    IEnumerator ShowEmotion(Transform emotion) // 코루틴 함수.
    {
        emotion.gameObject.SetActive(true);
        yield return new WaitForSeconds(0.5f);
        emotion.gameObject.SetActive(false);
    }
    void MoveStop()
    {
        rb.linearVelocity = Vector2.zero;
        anim.SetBool("walk", false);
    }
    void UpdateDirection()
    {
        if (player == null)
        {
            Debug.LogWarning("Player is NULL");
            return;
        }

        float dirX = player.transform.position.x - transform.position.x;

        isLookingLeft = dirX < 0;

        if (isLookingLeft)
        {
            Debug.Log($"Player is LEFT / dirX: {dirX}");
            transform.localScale = new Vector3(1, 1, 1);
        }
        else
        {
            Debug.Log($"Player is RIGHT / dirX: {dirX}");
            transform.localScale = new Vector3(-1, 1, 1);
        }
    }

    void FollowPlayer()
    {
        // inSight == false일 때, 기억된 위치로 이동
        float dir = lastKnownPlayerPosition.x - transform.position.x;
        rb.linearVelocityX =  Mathf.Sign(dir) * moveSpeed;

        // 적이 마지막 위치에 거의 도달하면 멈추기 (오차 보정)
        if (Vector2.Distance(transform.position, lastKnownPlayerPosition) < 3f)
        {
            MoveStop();
        }
    }

    

    void Shoot()
    {
        // 플레이어가 아닌 마지막 위치로 방향 설정
        Vector2 dir = (lastKnownPlayerPosition - (Vector2)bulletSpawnPoint.position).normalized;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        Quaternion rot = Quaternion.Euler(0, 0, angle);

        GameObject bullet = Instantiate(bulletPrefab, bulletSpawnPoint.position, rot);
        Destroy(bullet, 10f);

        Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
        bulletRb.linearVelocity = dir * bulletSpeed;

        Bullet b = bullet.GetComponent<Bullet>();
        b.shooter = gameObject;
        b.speed = bulletSpeed;
    }

    

    public void Die()
    {
        if (isDead) return;
        isDead = true;

        anim.SetTrigger("die");

        Transform sight = transform.Find("Sight");
        if (sight) sight.gameObject.SetActive(false);

        rb.linearVelocity = Vector2.zero;
    }
}
