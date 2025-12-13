using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class PatternManager : MonoBehaviour
{
    [Header("스크립트 (자동)")]
    [SerializeField] private Teleport tp;
    [SerializeField] private Laser ls;
    [SerializeField] private LinearMove mv;

    [Header("오브젝트 정보")]
    [SerializeField] private float rotationZ;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Animator anim;
    [SerializeField] private float originGravityScale;

    [Header("이동 위치")]
    [SerializeField] private Transform R_Down_Point;
    [SerializeField] private Transform L_Up_Point;
    [SerializeField] private Transform Center_Point;

    [Header("패턴#1 가로공격")]
    [SerializeField] private float tpTime_WideATK;
    [SerializeField] private float lsAimeTime_WideATK;
    [SerializeField] private float lsFireTime_WideATK;
    [SerializeField] private float lsEndTime_WideATK;
    [Header("패턴#2 세로연속공격")]
    [SerializeField] private float tpTime_RainATK = 0f;
    [SerializeField] private float lsAimeTime_RainATK = 0.1f;
    [SerializeField] private float lsFireTime_RainATK = 0.1f;
    [SerializeField] private float lsEndTime_RainATK = 0f;
    [SerializeField] private Transform []rain_TP_Points;
    [SerializeField] private int reps ;
    [Header("패턴#3 회전공격")]
    [SerializeField] private AnimationCurve spin_ready;
    [SerializeField] private AnimationCurve spin_end;
    [SerializeField] private float spin_z_rd = -30;
    [SerializeField] private float spin_z_at = -160;
    [SerializeField] private float spin_z_ld = -360;
    [SerializeField] private float duration = 1f;
    [SerializeField] private float maxY = 1f;
    [SerializeField] private float attackTime = .5f;

    // 조건 #1 - 플레이어 체크
    [Header("플레이어 체크")]
    [SerializeField] private Transform PlayerPos;

    // 다중 실행 가드
    [Header("플레이어 인식 범위")]
    Coroutine _loop;
    bool _running;
    [SerializeField] private float RainDis = 5f;
    [SerializeField] private float SpinDis = 10f;
    [SerializeField] private float WideDis = 20f;
    float patternInterval = .4f;
    float RainDis2, SpinDis2, WideDis2;

    private bool loopIsRunning = false;
    private Func<IEnumerator>[] patterns;
    private float loopInterval = 0.5f;
    public float startDistance = 10f;
    
    void Start()
    {
        tp = GetComponent<Teleport>();
        ls = GetComponent<Laser>();
        mv = GetComponent<LinearMove>();
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        originGravityScale = rb.gravityScale;

        patterns = new Func<IEnumerator>[] { WideATKCoroutine, RainATKCoroutine, SpinATKCoroutine };


        RainDis2 = RainDis * RainDis;
        SpinDis2 = SpinDis * SpinDis;
        WideDis2 = WideDis * WideDis;
    }

    // Update is called once per frame
    void Update()
    {
        // Quaternion.Euler(0,0,rotationZ); ;
        if (PlayerDisCheck()<startDistance)
            StartPattern_Random();
        
    }

    public void StartPattern_Random()
    {
        if (loopIsRunning == true) return;

        StartCoroutine(Pattern_Random(loopInterval)); // 버튼 클릭 시 실행됨
        loopIsRunning = true;
    }

    public void StopPattern()
    {
        rb.gravityScale = originGravityScale;
        ls.LaserDisable();
        StopAllCoroutines(); // 또는 특정 코루틴을 변수로 저장해 중지 가능
        StopPattern_Distance();
    }

    
    public IEnumerator Pattern_Random(float loopInterval)
    {
        int last = -1;

        while (true)
        {
            // 연속 중복 방지
            int idx;
            do { idx = UnityEngine.Random.Range(0, patterns.Length); }
            while (idx == last && patterns.Length > 1);

            last = idx;

            // 선택된 패턴 실행
            yield return StartCoroutine(patterns[idx]());

            // 패턴 사이 텀(선택)
            if (loopInterval > 0f)
                yield return new WaitForSeconds(loopInterval);
        }
    }




    public float PlayerDisCheck()
    {
        Vector2 d = (Vector2)PlayerPos.position - (Vector2)transform.position;
        float sqr = d.sqrMagnitude;
        Debug.Log($"플레이어 거리 : {sqr}");
        return sqr;
    }
    // 조건 #2 - 수행할 패턴 판단
    // 너무 가까울 때, 적당히 멀때, 아주 멀때.

    
    public void StartPattern_Distance()
    {
        if (_loop != null) return;          // 중복 실행 방지
        _loop = StartCoroutine(Pattern_PlayerDistanceCheck());
    }

    public void StopPattern_Distance()
    {
        if (_loop != null) StopCoroutine(_loop);
        _loop = null;
        _running = false;
    }
    
    public IEnumerator Pattern_PlayerDistanceCheck()
    {
        if (_running) yield break;          // 재진입 방지
        _running = true;

        while (true)
        {
            int sector = 0;
            float distance = PlayerDisCheck();
            if (distance < RainDis2) sector = 1;
            else if (distance < SpinDis2) sector = 2;
            else if (distance < WideDis2) sector = 3;
            else sector = 0;

            Debug.Log($"[Boss] dist={distance:F2}, sector={sector}");

            switch (sector)
            {
                case 1:
                    yield return StartCoroutine(RainATKCoroutine());
                    break;
                case 2:
                    yield return StartCoroutine(SpinATKCoroutine());
                    break;
                case 3:
                    yield return StartCoroutine(WideATKCoroutine());
                    break;
                default:
                    break;
            }

            yield return new WaitForSeconds(patternInterval);
        }
    }

    // 패턴 #1 - 와이드공격
    
    public void WideATK()
    {
        StartCoroutine(WideATKCoroutine());
    }
    public IEnumerator WideATKCoroutine()
    {
        yield return StartCoroutine(tp.TeleportTo(R_Down_Point.position, tpTime_WideATK));
        anim.SetTrigger("shot");
        yield return StartCoroutine(ls.LaserATK(lsAimeTime_WideATK, lsFireTime_WideATK, lsEndTime_WideATK));
        
        yield return null;
    }
    // 패턴 #2 - 수직공격
    
    public void RainATK()
    {
        StartCoroutine(RainATKCoroutine());
    }
    public IEnumerator RainATKCoroutine()
    {
        rb.gravityScale = 0f;
        rotationZ = 90;
        transform.Rotate(0, 0, rotationZ);
        // transform.rotation = Quaternion.Euler(0, 0, rotationZ);
        Vector2 startPosition = rain_TP_Points[0].position;
        Vector2 distance = rain_TP_Points[1].position - rain_TP_Points[0].position;

        for (int i = 0; i < reps; i++)
        {
            yield return StartCoroutine(tp.TeleportTo(startPosition + distance*i/(reps-1), tpTime_RainATK));
            yield return StartCoroutine(ls.LaserATK(lsAimeTime_RainATK, lsFireTime_RainATK, lsEndTime_RainATK));
        }
        transform.Rotate(0, 0, -rotationZ);
        // transform.rotation = Quaternion.Euler(0, 0, rotationZ);
         yield return StartCoroutine(tp.TeleportTo(R_Down_Point.position, 1f));
        rb.gravityScale = originGravityScale;

        yield return null;
    }

    // 패턴 #3 - 회전공격/ 이동함수.cs 필요
    public void SpinATK()
    {
        StartCoroutine(SpinATKCoroutine());
    }
    public IEnumerator SpinATKCoroutine()
    {
        // 공격방향 우측
        yield return rb.gravityScale = 0f;

        // 1. 이동 및 회전
        StartCoroutine(mv.MoveTo(Center_Point.position, spin_ready, duration, maxY));
        yield return StartCoroutine(RotateByOverDuration(spin_z_rd, duration));
        Flip();

        // 2. 공격 및 회전
        StartCoroutine(ls.LaserATK(0f, attackTime, 0f));
        yield return StartCoroutine(RotateByOverDuration(spin_z_at, attackTime));

        // 3. 공격종료 및 착지

        yield return StartCoroutine(ls.LaserEndFor(0f));
        Flip();
        StartCoroutine(mv.MoveTo(R_Down_Point.position, spin_end, duration, maxY));
        yield return StartCoroutine(RotateByOverDuration(spin_z_ld, duration));

        rb.gravityScale = originGravityScale;

        yield return null;
    }
    // 패턴 #4 - 점프공격/ 이동함수.cs 필요
    public void JumpATK()
    {
        StartCoroutine(JumpATKCoroutine());
    }
    public IEnumerator JumpATKCoroutine()
    {

        // 공격방향 우측
        yield return rb.gravityScale = 0f;

        // 1. 이동 및 회전
        StartCoroutine(mv.MoveTo(Center_Point.position, spin_ready, duration, maxY));
        yield return StartCoroutine(RotateByOverDuration(spin_z_rd, duration));
        Flip();

        // 2. 공격 및 회전
        StartCoroutine(ls.LaserATK(0f, attackTime, 0f));
        yield return StartCoroutine(RotateByOverDuration(spin_z_at, attackTime));

        // 3. 공격종료 및 착지

        yield return StartCoroutine(ls.LaserEndFor(0f));
        Flip();
        StartCoroutine(mv.MoveTo(R_Down_Point.position, spin_end, duration, maxY));
        yield return StartCoroutine(RotateByOverDuration(spin_z_ld, duration));

        rb.gravityScale = originGravityScale;
        yield return null;
    }
    


    public void Flip() => transform.Rotate(0f, 180f, 0f, Space.Self);
    public IEnumerator RotateByOverDuration(float addAngle, float duration)
    {
        float start = rb.rotation;          // 시작 각도(절대)
        // float end   = start + addAngle;     // 목표 절대 각도(누적 허용: 360, 720 등)
        float end   = addAngle;     // 목표 절대 각도(누적 허용: 360, 720 등)
        float elapsed = 0f;

        // duration이 0 또는 매우 작을 때 바로 세팅
        if (duration <= Mathf.Epsilon)
        {
            rb.MoveRotation(end);
            yield break;
        }

        // FixedUpdate 주기에 맞춰 진행
        while (elapsed < duration)
        {
            yield return new WaitForFixedUpdate();
            elapsed += Time.fixedDeltaTime;

            float t = Mathf.Clamp01(elapsed / duration);
            // 선형 진행(원하면 t에 EaseInOut 등 곡선 적용 가능)
            float current = Mathf.Lerp(start, end, t);
            Debug.Log(current);
            // MoveRotation은 물리적으로 부드럽게 누적 적용됨
            rb.MoveRotation(current);
        }
        // 마지막 프레임에 정확히 목표 각도로 정렬
        rb.rotation = end%360; 
    }
}