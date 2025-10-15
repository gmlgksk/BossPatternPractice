using System.Collections;
using Unity.Mathematics;
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
    [SerializeField] private float originGravityScale;

    [Header("이동 위치")]
    [SerializeField] private Transform R_Down_Point;
    [SerializeField] private Transform L_Up_Point;
    [SerializeField] private Transform Center_Point;
    
    [Header("패턴#1 가로공격")]
    [Header("패턴#2 세로연속공격")]
    [Header("패턴#3 회전공격")]
    [SerializeField] private AnimationCurve spin_ready;
    [SerializeField] private AnimationCurve spin_end;
    
    [SerializeField] private float spin_z_rd = -30;
    [SerializeField] private float spin_z_at = 150;
    [SerializeField] private float spin_z_ld = 360;


    void Start()
    {
        tp = GetComponent<Teleport>();
        ls = GetComponent<Laser>();
        mv = GetComponent<LinearMove>();
        rb = GetComponent<Rigidbody2D>();
        originGravityScale = rb.gravityScale;
    }

    // Update is called once per frame
    void Update()
    {
        // Quaternion.Euler(0,0,rotationZ); ;
    }

    // 패턴 #1 - 와이드공격
    public void WideATK()
    {
        StartCoroutine(WideATKCoroutine());
    }
    public IEnumerator WideATKCoroutine()
    {
        yield return StartCoroutine(tp.TeleportTo(R_Down_Point.position, 0.5f));
        yield return StartCoroutine(ls.LaserATK(0.5f, 1f, 0.4f));
    }
    // 패턴 #3 - 하단공격
    public void RainATK()
    {
        StartCoroutine(RainATKCoroutine());
    }
    public IEnumerator RainATKCoroutine()
    {
        rb.gravityScale = 0f;
        rotationZ = 90;
        transform.rotation = Quaternion.Euler(0, 0, rotationZ);
        int reps = 5;
        Vector2 rain_TP_Point = L_Up_Point.position;


        for (int i = 0; i < reps; i++)
        {
            yield return StartCoroutine(tp.TeleportTo(rain_TP_Point, 0f));
            yield return StartCoroutine(ls.LaserATK(0.1f, 0.1f, 0f));
            yield return rain_TP_Point += Vector2.right * 3;
        }
        rotationZ = 0;
        transform.rotation = Quaternion.Euler(0, 0, rotationZ);

        rb.gravityScale = originGravityScale;
    }

    // 패턴 #3 - 회전공격/ 이동함수.cs 필요
    public void SpinATK()
    {
        StartCoroutine(SpinATKCoroutine());
    }
    public IEnumerator SpinATKCoroutine()
    {
        // 공격방향 우측
        rb.gravityScale = 0f;

        float duration = 1f;
        float multi_Y = 1f;
        

        // 1. 이동 및 회전
        StartCoroutine(mv.MoveTo(Center_Point.position, spin_ready, duration, multi_Y));
        yield return StartCoroutine(RotateByOverDuration(spin_z_rd, duration));
        Flip();
        
        // 2. 공격 및 회전
        float attackTime = .5f;

        StartCoroutine(ls.LaserATK(0f, attackTime, 0f));
        yield return StartCoroutine(RotateByOverDuration(spin_z_at, attackTime));

        // 3. 공격종료 및 착지
        Flip();

        yield return StartCoroutine(ls.LaserEndFor(0f));
        StartCoroutine(mv.MoveTo(R_Down_Point.position, spin_end, duration, multi_Y));
        yield return StartCoroutine(RotateByOverDuration(spin_z_ld, duration));

        rb.gravityScale = originGravityScale;
    }

    public void Flip() => transform.Rotate(0f, 180f, 0f, Space.Self);

    // public IEnumerator RotateTo(float targetZ,float duration)
    // {
    //     float elapsedTime = 0f;
    //     Quaternion startRot = Quaternion.Euler(0, transform.eulerAngles.y, transform.eulerAngles.z);
    //     Quaternion endRot = Quaternion.Euler(0, transform.eulerAngles.y, targetZ);

    //     while (elapsedTime < duration)
    //     {
    //         float t = elapsedTime / duration;
    //         transform.rotation = Quaternion.Lerp(startRot, endRot, t);

    //         elapsedTime += Time.deltaTime;
    //         yield return null;
    //     }
    //     transform.rotation = endRot;
    // }
    // public IEnumerator RotateTo(float targetZ, float duration)
    // {
    //     float elapsedTime = 0f;

    //     Angle start = Angle.Degrees(transform.eulerAngles.z);
    //     Angle end   = Angle.Degrees(targetZ);

    //     while (elapsedTime < duration)
    //     {
    //         float t = elapsedTime / duration;
    //         Angle current = Angle.Degrees( Mathf.LerpAngle(start, end, t));
    //         transform.rotation = Quaternion.Euler(0, transform.eulerAngles.y, current.Degrees);

    //         elapsedTime += Time.deltaTime;
    //         yield return null;
    //     }

    //     transform.rotation = Quaternion.Euler(0, transform.eulerAngles.y, end);
    // }
    // public IEnumerator RotateTo(float targetZ, float duration)
    // {
    //     float startZ = transform.eulerAngles.z;
    //     float elapsedTime = 0f;

    //     while (elapsedTime < duration)
    //     {
    //         float t = elapsedTime / duration;
    //         // 각도를 선형으로 보간
    //         float currentZ = Mathf.Lerp(startZ, targetZ, t);
    //         transform.rotation = Quaternion.Euler(0, transform.eulerAngles.y, currentZ);

    //         elapsedTime += Time.deltaTime;
    //         yield return null;
    //     }

    //     transform.rotation = Quaternion.Euler(0, transform.eulerAngles.y, targetZ);
    // }

    // public IEnumerator RotateTo(float targetZ, float duration)
    // {
    //     float startZ = transform.eulerAngles.z;
    //     float elapsedTime = 0f;

    //     while (elapsedTime < duration)
    //     {
    //         float t = elapsedTime / duration;
    //         // 각도를 선형으로 보간
    //         float currentZ = Mathf.Lerp(startZ, targetZ, t);
    //         rb.rotation = currentZ;

    //         elapsedTime += Time.deltaTime;
    //         yield return null;
    //     }

    //     rb.rotation = targetZ;
    // }
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