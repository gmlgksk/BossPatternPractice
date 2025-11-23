using System.Collections;
using UnityEngine;

public class HP : MonoBehaviour
{
    // 참조
    private TimeController tc;
    
    private Animator anim;

    private Rigidbody2D rb;
    private PatternManager pm;

    [Header("수치")]
    [SerializeField] private int maxHp=2;
    [SerializeField] private int hp;
    [SerializeField] private bool isDead=false;
    [SerializeField] private bool isDamegedable=true;
    [SerializeField] private string DamegedableTag = "Attack";
    [SerializeField] private float launchForce;
    [SerializeField] private Vector2 launchDir = new(1f,0f);

    private void Start()
    {
        hp = maxHp;
        pm = GetComponent<PatternManager>();
        anim = GetComponent<Animator>();
    }
    void Update()
    {

    }
    private void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag(DamegedableTag)) return;

        StartDameged();
    }

    public void Init()
    {
        hp = maxHp;
        isDead = false;
        isDamegedable = true;
        anim.SetTrigger("init");
    }


    public void StartDameged()
    {
        if (isDead == true || isDamegedable == false) return;

        StartCoroutine(DamagedCoroutine());
    }
    public IEnumerator DamagedCoroutine()
    {
        if (hp >= 1) hp -= 1;
        isDamegedable = false;
        Debug.Log($"피격! 현재체력={hp}");
        if (hp == 0) Dead();
        yield return StartCoroutine(BlinkCoroutine());

        isDamegedable = true;
    }

    public void Dead()
    {
        isDead = true;
        isDamegedable = false;
        Debug.Log("죽었다!");

        anim.SetTrigger("dead");
        pm.StopPattern_Distance();
        tc.SlowTimeEffectSmooth();
        Launch(launchForce,launchDir);
    }
    

    // 힘의 크기를 매개변수로 전달받아, 현재 방향 기준 우상단으로 힘을 준다
    public void Launch(float force, Vector2 dir)
    {
        // 현재 회전을 기준으로 우측 위 방향 계산
        Vector2 forceDir = dir.normalized;
        rb.AddForce(forceDir * force, ForceMode2D.Impulse);
    }


    [SerializeField] private float blinkDuration = 0.2f; // 점멸 유지 시간
    [SerializeField] private int blinkCount = 2;         // 점멸 횟수

    private SpriteRenderer sr;
    private Color originalColor;
    private void Awake()
    {
        tc = GetComponent<TimeController>();
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        originalColor = sr.color;
    }
    public void StartBlink()
    {
        StartCoroutine(BlinkCoroutine());
    }
    
    private IEnumerator BlinkCoroutine()
    {
        for (int i = 0; i < blinkCount; i++)
        {
            sr.color = Color.red;                    // 하얀색으로 변경
            yield return new WaitForSeconds(blinkDuration / 2);
            sr.color = originalColor;                  // 원래 색상으로 복귀
            yield return new WaitForSeconds(blinkDuration / 2);
        }
    }
}
