using System.Collections;
using UnityEngine;

public class BossGunner_HP_System : HP_System
{
    [Header("===== [ Boss_Gunner Options ] =====")]
    [Header("[ Dead ]")]
    [SerializeField] private float launchForce;
    [SerializeField] private Vector2 launchDir = new(1f,0f);
    private TimeController tc;
    private Animator anim;
    private Rigidbody2D rb;
    private PatternManager pm;
    private SpriteRenderer sr;
    private Color originalColor;
    [Header("[ Damaged ]")]
    
    [SerializeField] private bool isDamegedable=true;
    [SerializeField] private float blinkDuration = 0.2f; // 점멸 유지 시간
    [SerializeField] private int blinkCount = 2;         // 점멸 횟수

    protected override void Awake()
    {
        tc = GetComponent<TimeController>();
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        originalColor = sr.color;
        hp_current  = hp_max;
        pm = GetComponent<PatternManager>();
        anim = GetComponent<Animator>();
    }

    protected override void Handle_HP()
    {
        // 비워둠
    }
    protected override void Health_Init()
    {
        hp_current = hp_max;
        isDamegedable = true;
        anim.SetTrigger("init");
    }
    


    protected override void Reaction_heal()
    { // 회복 효과
    
    }
    protected override void Reaction_hurt()
    { // 피격 효과
        if (isDamegedable == false) return;

        isDamegedable = false;
        StartCoroutine(DamagedEffectCoroutine());
    }
    protected override void Reaction_Die()
    { // 사망효과
        isDamegedable = false;

        pm.StopPattern();
        rb.MoveRotation(0f);
        anim.SetTrigger("dead");
        pm.StopPattern_Distance();
        tc.SlowTimeEffectSmooth();
        Launch(launchForce,launchDir);
    }





    public IEnumerator DamagedEffectCoroutine()
    {
        for (int i = 0; i < blinkCount; i++)
        {
            sr.color = Color.red;                    // 하얀색으로 변경
            yield return new WaitForSeconds(blinkDuration / 2);
            sr.color = originalColor;                  // 원래 색상으로 복귀
            yield return new WaitForSeconds(blinkDuration / 2);
        }
        isDamegedable = true;
    }
    public void Launch(float force, Vector2 dir)
    {
        // 현재 회전을 기준으로 우측 위 방향 계산
        Vector2 forceDir = dir.normalized;
        rb.AddForce(forceDir * force, ForceMode2D.Impulse);
    }
}
