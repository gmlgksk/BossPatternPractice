using System.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

public class Laser : MonoBehaviour
{
    [SerializeField] private quaternion rotation; // 메인 회전값. 이 코드에서 사용x

    [Header("레이저 설정")]
    [SerializeField] private Transform attackPoint; // 레이저가 발사될 위치
    [SerializeField] private Vector2 attackDir; // 레이저 방향

    [SerializeField] private GameObject laserObject; // 레이저 오브젝트
    [SerializeField] private Transform scale; // 레이저 크기
    [SerializeField] private float laserWidth = 0.5f;
    [SerializeField] private LayerMask laserHitLayers;

    [Header("이펙트 설정")]
    [SerializeField] private ObjectPool_laserDust laserDust;
    [SerializeField] private GameObject hitEffect;
    [SerializeField] private float nextSpawnTime = 0f;
    [SerializeField] private float interval = 0.2f;

    void Start()
    {
        laserObject.SetActive(false);
        attackPoint.position = new Vector2(attackPoint.position.x, attackPoint.position.y);
        attackDir = Vector2.left;
        scale = laserObject.GetComponent<Transform>();
    }

    void Update()
    {
    }
    public IEnumerator LaserATK(float laserAimeDuration,float laserFireDuration,float laserEndDuration)
    {
        yield return StartCoroutine(LaserAimeFor(laserAimeDuration));
        yield return StartCoroutine(LaserFireFor(laserFireDuration));
        yield return StartCoroutine(LaserEndFor(laserEndDuration));
    }

    public void LaserDisable()
    {
        laserObject.SetActive(false);
    }

    public IEnumerator LaserAimeFor(float laserAimeDuration)
    {
        
        // 2) 레이저 발사 시작
        scale.localScale = new Vector2(scale.localScale.x, 0f); // 두께 0으로 시작
        // laserObject.transform.localScale = scale;
        laserObject.SetActive(true);

        var laserSprite = laserObject.GetComponent<SpriteRenderer>();
        laserSprite.color = Color.red;
        // 크기 세팅 
        float elapsed = 0f;

        while (elapsed < laserAimeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / laserAimeDuration;

            // 0 → laserMaxWidth 로 선형 보간
            float currentWidth = Mathf.Lerp(0f, laserWidth * 0.5f, t);

            // // 스케일 적용 (X축은 길이 유지, Y축만 두께 변화)
            // RaycastHit2D hit = Physics2D.Raycast(attackPoint.position, -attackPoint.right, 50, laserHitLayers);
            // scale.localScale = new Vector2(hit.distance, currentWidth);
            scale.localScale = new Vector2(50, currentWidth);

            // laserObject.transform.localPosition = new Vector2(-hit.distance / 2f, 0);
            laserObject.transform.localPosition = new Vector2(-50 / 2f, 0);


            yield return null;
        }

        // 최종 보정
        scale.localScale = new Vector2(scale.localScale.x, laserWidth);
        laserSprite.color = Color.yellow;
    }
    public bool laserOn = false;
    public IEnumerator LaserFireFor(float laserFireDuration)
    {
        float elapsed = 0f;

        while (elapsed < laserFireDuration)
        {
            elapsed += Time.deltaTime;
            laserOn = true;

            RaycastHit2D hit = Physics2D.Raycast(attackPoint.position, -attackPoint.right, 50, laserHitLayers);
            // 충돌체크 - 벽
            if (hit.collider.includeLayers == laserHitLayers)
            {
                scale.localScale = new Vector2 (hit.distance,scale.localScale.y);
                scale.localPosition = new Vector2 (-hit.distance / 2f, 0);
            }
            if (hit.collider.TryGetComponent(out HP_System playerHp))
            {
                Debug.Log("너 피 있어");
                playerHp.Health_Reduce();
            }
            
            if (elapsed >= nextSpawnTime)
            {
                GameObject effect = Instantiate(hitEffect, hit.point, Quaternion.identity);
                Destroy(effect, 0.5f); // 1초 뒤 삭제
                nextSpawnTime += interval;
            }

            yield return null;
        }
        laserOn = false;
        nextSpawnTime = 0f;
    }
    
    public IEnumerator LaserEndFor(float laserEndDuration)
    {
        float elapsed = 0f;

        while (elapsed < laserEndDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / laserEndDuration;

            // 0 → laserMaxWidth 로 선형 보간
            float currentWidth = Mathf.Lerp(laserWidth, laserWidth * 0.5f, t);

            // 스케일 적용 (X축은 길이 유지, Y축만 두께 변화)
            scale.localScale = new Vector2(scale.localScale.x, currentWidth);

            yield return null;
        }

        laserObject.SetActive(false);
    }
    

}
