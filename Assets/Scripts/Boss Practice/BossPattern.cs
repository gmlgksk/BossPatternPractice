using System.Collections;
using UnityEngine;

public class BossPattern : MonoBehaviour
{
    [Header("텔레포트 설정")]
    
    [SerializeField] private Transform greenRoom;
    [SerializeField] private Transform teleportPoint;
    [SerializeField] private float disappearTime = 0.5f;

    [Header("레이저 설정")]
    [SerializeField] private GameObject laserBeam; // 레이저 오브젝트
    [SerializeField] private Transform laserOrigin; // 레이저가 발사될 위치
    [SerializeField] private  Vector2 lazerDir; // 레이저 방향
    [SerializeField] private  Transform scale; // 레이저 크기
    [SerializeField] private float laserAimeDuration; // 조준시간
    [SerializeField] private float laserFireTime = 2f; // 레이저 발사 지속 시간
    [SerializeField] private float laserEndDuration = 1f; // 레이저가 사라지는 시간

    [SerializeField] private float laserWidth = 0.5f;
    [SerializeField] private LayerMask laserHitLayers;
    [SerializeField] private GameObject hitEffect;
    

    [Header("캐릭터 설정")]
    [SerializeField] private SpriteRenderer sr;
    [SerializeField] private Collider2D col;
    [SerializeField] private Rigidbody2D rb;

    [Header("이펙트 설정")]
    [SerializeField] private float nextSpawnTime = 0f;  
    [SerializeField] private float interval = 0.2f;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        rb = GetComponent<Rigidbody2D>();

        laserBeam.SetActive(false);
        lazerDir = -laserOrigin.right;
        scale = laserBeam.GetComponent<Transform>();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void StartAttackPattern_1()
    {
        StartCoroutine(AttackPattern_1());
    }
    public IEnumerator AttackPattern_1()
    {
        yield return StartCoroutine(Teleport(teleportPoint));
        yield return new WaitForSeconds(0.2f);
        yield return StartCoroutine(LaserAttack_1());
    }

    public void AttackPattern_1_Test()
    {
        StartCoroutine(Teleport(teleportPoint));
        StartCoroutine(LaserAttack_1());
    }



    //텔레포트 0:대기실,1:오른쪽땅
    //StartTeleport는 단일 테스트용. 패턴에 삽입할 함수는 Teleport(int point)
    // public void StartTeleport()
    // {
    //     StartCoroutine(Teleport(1));
    // }

    public IEnumerator Teleport(Transform point)
    {
        // 1) 사라짐
        sr.enabled = false;          // 보스 스프라이트 숨김
        if (col != null) col.enabled = false; // 충돌도 잠시 끔
        rb.bodyType = RigidbodyType2D.Static;
        transform.position = greenRoom.position;
        // transform.position = teleportPoint[0].position;


        // 0.5초 대기
        yield return new WaitForSeconds(disappearTime);

        // 2) 위치 이동
        transform.position = point.position;

        // 3) 다시 나타남
        rb.bodyType = RigidbodyType2D.Dynamic;
        sr.enabled = true;
        if (col != null) col.enabled = true;

        yield return null;
    }

    // 레이저 공격 단일 테스트용 함. 실제 적용함수는 LaserAttack_1
    // public void StartLaserAttack()
    // {
    //     StartCoroutine(LaserAttack_1());
    // }
    // public void StartLaserAttack_Endless()
    // {
    //     StartCoroutine(LaserAttack_1_Endless());
    // }
    // ============================
    // private IEnumerator LaserAttack_1_Endless()
    // {
    //     // 1) 레이저 조준
    //     yield return StartCoroutine(LaserAime());
    //     // 2) 레이저 발사
    //     yield return StartCoroutine(LaserFire(1000f));
    // }


    // 패턴이 적용된 함수
    private IEnumerator LaserAttack_1()
    {
        // 1) 레이저 조준
        yield return StartCoroutine(LaserAime());
        // 2) 레이저 발사
        yield return StartCoroutine(LaserFire(laserFireTime));
        // 3) 레이저 종료
        yield return StartCoroutine(LaserEnd());

    }
    

    private IEnumerator LaserAime()
    {
        // 보스 기준 바라보는 방향
        Vector2 lazerDir = -laserOrigin.right;
        // 2) 레이저 발사 시작
        scale.localScale = new Vector2(scale.localScale.x,0f); // 두께 0으로 시작
        // laserBeam.transform.localScale = scale;
        laserBeam.SetActive(true);

        var laserSprite = laserBeam.GetComponent<SpriteRenderer>();
        laserSprite.color = Color.red;
        // 크기 세팅 
        float elapsed = 0f;

        while (elapsed < laserAimeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / laserAimeDuration;

            // 0 → laserMaxWidth 로 선형 보간
            float currentWidth = Mathf.Lerp(0f, laserWidth * 0.5f, t);

            // 스케일 적용 (X축은 길이 유지, Y축만 두께 변화)
            lazerDir = -laserOrigin.right;
            RaycastHit2D hit = Physics2D.Raycast(laserOrigin.position, lazerDir, 25, laserHitLayers);
            // scale = laserBeam.transform.localScale;
            scale.localScale = new Vector2 (hit.distance,currentWidth);
            // laserBeam.transform.localScale = scale;


            laserBeam.transform.localPosition = new Vector2(-hit.distance / 2f, 0);

            yield return null;
        }

        // 최종 보정
        scale.localScale = new Vector2 (scale.localScale.x, laserWidth);
        laserSprite.color = Color.yellow;
    }
    
    private IEnumerator LaserFire(float laserFireTime)
    {
        float elapsed = 0f;

        while (elapsed < laserFireTime)
        {
            elapsed += Time.deltaTime;

            lazerDir = -laserOrigin.right;
            RaycastHit2D hit = Physics2D.Raycast(laserOrigin.position, lazerDir, 25, laserHitLayers);
            // 충돌체크 - 벽
            if (hit.collider != null)
            {
                scale.localScale = new Vector2 (hit.distance,scale.localScale.y);
                scale.localPosition = new Vector2 (-hit.distance / 2f, 0);
            }

            if (elapsed >= nextSpawnTime)
            {
                GameObject effect = Instantiate(hitEffect, hit.point, Quaternion.identity);
                Destroy(effect, 0.5f); // 1초 뒤 삭제
                nextSpawnTime += interval;
            }

            yield return null;
        }
        nextSpawnTime = 0f;
    }
    
    private IEnumerator LaserEnd()
    {
        float elapsed = 0f;
        Vector2 scale;

        while (elapsed < laserEndDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / laserEndDuration;

            // 0 → laserMaxWidth 로 선형 보간
            float currentWidth = Mathf.Lerp(laserWidth, laserWidth * 0.5f, t);

            // 스케일 적용 (X축은 길이 유지, Y축만 두께 변화)
            scale = laserBeam.transform.localScale;
            scale.y = currentWidth;
            laserBeam.transform.localScale = scale;

            yield return null;
        }

        laserBeam.SetActive(false);
    }
}
