using System.Collections;
using UnityEngine;

public class BossPattern : MonoBehaviour
{
    [Header("텔레포트 설정")]
    [SerializeField] private Transform[] teleportPoint;
    [SerializeField] private float disappearTime = 0.5f;

    [Header("레이저 설정")]
    [SerializeField] private Transform laserOrigin; // 레이저가 발사될 위치
    [SerializeField] private GameObject laserBeam; // 레이저 오브젝트
    [SerializeField] private float aimeDuration; // 조준시간
    [SerializeField] private float laserFireTime = 2f; // 레이저 발사 지속 시간
    [SerializeField] private float endDuration = 1f; // 레이저 발사 지속 시간

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
        laserBeam.SetActive(false);
        var ps = hitEffect.GetComponent<ParticleSystem>();
        ps.Clear();
        ps.Stop();
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void StartAttack_1()
    {
        StartCoroutine(AttackPattern_1());
    }
    public IEnumerator AttackPattern_1()
    {
        yield return StartCoroutine(TeleportToPosition());
        yield return new WaitForSeconds(0.5f);
        yield return StartCoroutine(LaserAttackRoutine());
    }





    //텔레포트
    public void Teleport()
    {
        StartCoroutine(TeleportToPosition());
    }

    public IEnumerator TeleportToPosition()
    {
        // 1) 사라짐
        sr.enabled = false;          // 보스 스프라이트 숨김
        if (col != null) col.enabled = false; // 충돌도 잠시 끔
        rb.bodyType = RigidbodyType2D.Static;
        transform.position = teleportPoint[0].position;


        // 0.5초 대기
        yield return new WaitForSeconds(disappearTime);

        // 2) 위치 이동
        transform.position = teleportPoint[1].position;

        // 3) 다시 나타남
        rb.bodyType = RigidbodyType2D.Dynamic;
        sr.enabled = true;
        if (col != null) col.enabled = true;

        yield return null;
    }
    //레이저 공격

    //1. 레이저 나타남 (0->1)
    //1-1. 레이저 충돌
    //      플레이어에게 데미지 : 레이져 빔 크기
    //      벽충돌 : 일직선으로 코드에서 확인.
    //2. 레이저 사라짐 (1->0, 깜빡임)

    // ===== 레이저 공격 =====
    public void StartLaserAttack()
    {
        StartCoroutine(LaserAttackRoutine());
    }
    public void StartLaserAttack_Endless()
    {
        StartCoroutine(LaserAttackRoutine_Endless());
    }
    private IEnumerator LaserAttackRoutine()
    {
        // 1) 레이저 조준
        yield return StartCoroutine(LaserAime());
        // 2) 레이저 발사
        yield return StartCoroutine(LaserFire(laserFireTime));
        // 3) 레이저 종료
        yield return StartCoroutine(LaserEnd());
        
    }
    private IEnumerator LaserAttackRoutine_Endless()
    {
        // 1) 레이저 조준
        yield return StartCoroutine(LaserAime());
        // 2) 레이저 발사
        yield return StartCoroutine(LaserFire(1000f));
        // 3) 레이저 종료
        // laserBeam.SetActive(false);
    }
    

    private IEnumerator LaserAime()
    {
        // 보스 기준 바라보는 방향
        Vector2 lazerDir = -laserOrigin.right;


        // 2) 레이저 발사 시작
        Vector3 scale = laserBeam.transform.localScale;
        scale.y = 0f; // 두께 0으로 시작
        laserBeam.transform.localScale = scale;
        laserBeam.SetActive(true);

        var laserSprite = laserBeam.GetComponent<SpriteRenderer>();
        laserSprite.color = Color.red;
        // 크기 세팅 
        float elapsed = 0f;

        while (elapsed < aimeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / aimeDuration;

            // 0 → laserMaxWidth 로 선형 보간
            float currentWidth = Mathf.Lerp(0f, laserWidth * 0.5f, t);

            // 스케일 적용 (X축은 길이 유지, Y축만 두께 변화)
            lazerDir = -laserOrigin.right;
            RaycastHit2D hit = Physics2D.Raycast(laserOrigin.position, lazerDir, 25, laserHitLayers);
            scale = laserBeam.transform.localScale;
            scale.x = hit.distance;
            scale.y = currentWidth;
            laserBeam.transform.localScale = scale;


            laserBeam.transform.localPosition = new Vector2(-hit.distance / 2f, 0);

            yield return null;
        }

        // 최종 보정
        scale = laserBeam.transform.localScale;
        scale.y = laserWidth;
        laserBeam.transform.localScale = scale;
        laserSprite.color = Color.yellow;
    }
    private IEnumerator LaserFire(float laserFireTime)
    {

        float elapsed = 0f;
        // 보스 기준 바라보는 방향
        Vector2 lazerDir = -laserOrigin.right;
        // GameObject effect = Instantiate(hitEffect, teleportPoint[0].position, Quaternion.identity);
        // var particle = effect.GetComponent<ParticleSystem>();
        var scale = laserBeam.transform.localScale;
        // particle.Clear();
        // particle.Stop();


        while (elapsed < laserFireTime)
        {
            elapsed += Time.deltaTime;

            lazerDir = -laserOrigin.right;
            RaycastHit2D hit = Physics2D.Raycast(laserOrigin.position, lazerDir, 25, laserHitLayers);
            // 충돌체크 - 벽
            if (hit.collider != null)
            {
                // ShowHitEffect(effect, hit, particle);

                scale.x = hit.distance;
                laserBeam.transform.localScale = scale;
                laserBeam.transform.localPosition = new Vector2(-hit.distance / 2f, 0);

            }
            // else { particle.Stop(); }

            if (elapsed >= nextSpawnTime)
            {
                GameObject effect = Instantiate(hitEffect, hit.point, Quaternion.identity);
                Destroy(effect, 0.5f); // 1초 뒤 삭제
                nextSpawnTime += interval;
            }
            yield return null;
        }
        nextSpawnTime = 0f;
        // particle.Stop();
        // Destroy(effect, 1f); // 1초 뒤 삭제
    }
    private void ShowHitEffect(GameObject effect, RaycastHit2D hit, ParticleSystem particle)
    {
        effect.transform.position = hit.point;
        particle.Play();
    }
    private IEnumerator LaserEnd()
    {
        float elapsed = 0f;
        Vector2 scale;

        while (elapsed < endDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / endDuration;

            // 0 → laserMaxWidth 로 선형 보간
            float currentWidth = Mathf.Lerp(laserWidth , laserWidth * 0.5f, t);

            // 스케일 적용 (X축은 길이 유지, Y축만 두께 변화)
            scale = laserBeam.transform.localScale;
            scale.y = currentWidth;
            laserBeam.transform.localScale = scale;

            yield return null;
        }
        
        laserBeam.SetActive(false);
    }
}
