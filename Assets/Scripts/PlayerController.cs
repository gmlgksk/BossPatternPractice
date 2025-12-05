using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

// 최소 구성: 구체 콜라이더 사용
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CapsuleCollider2D))]
[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(Animator))]
public class PlayerController : Entity
{
    [SerializeField] private string debugSummary;
    // ===== 대전제 (불리언) =====
    public bool isGround { get; private set; }
    public bool isWall   { get; private set; }
    public bool isAir    { get; private set; }

    // ===== 상태 (전부 enum) =====
    public enum ActionState { Idle, Move, Jump,Fall, Dash, WallSlide, WallJump, Attack, Dead }// Fall은 기능을 뺀 jump, 기본상태이다.
    public ActionState Current { get; private set; } = ActionState.Idle;

    // ===== 컴포넌트 =====



    // ===== 기본 파라미터 =====

    [Header("Anim")]
    [SerializeField] private float animSpeed = 1f;
    
    [Header("Move")]
    [SerializeField] private float accel = 60f;
    [SerializeField] private float deccel = 70f;
    [SerializeField] private float airControl = 0.8f;
    [SerializeField] private float maxFallSpeed = -20f;

    [Header("Jump")]
    [SerializeField] private float jumpForce = 12f;
    [SerializeField] private int   maxJumpCount = 2;
    [SerializeField] private float coyoteTime = 0.1f;
    [SerializeField] private float jumpBuffer = 0.1f;

    [Header("Wall")]
    [SerializeField] Vector2 wallJumpDir = new Vector2(1.0f, 1.1f);
    [SerializeField] private float wallJumpForce = 12f;
    [SerializeField] private float wallJumpControlLock = 0.15f;
    [SerializeField] private float wallSlideSlowTime = 1f; // 최대 속도까지 도달하는 시간
    [SerializeField] private float wallSlideMaxSpeed = -10f; // 음수(아래 방향)로 두는 걸 추천
    [SerializeField] private float wallSlideHoldTime  = 0.1f; // 잠깐 멈추는 시간
    float wallSlideElapsed = 0f; // 벽 슬라이드 경과 시간
    float wallSlideAnchorY = 0f;   // '멈춰 있는' 동안 유지할 Y 위치
    bool  wallSlideHolding = false; // 지금 정지 구간인지 여부

    [Header("Dash")]
    [SerializeField] private float dashSpeed = 18f;

    [Header("Detect")]
    [SerializeField] private LayerMask whatIsGround;
    [SerializeField] private LayerMask WhatIsWall;
    [SerializeField] private float groundCheckDistance = 1.6f;
    [SerializeField] private float wallCheckDistance = 1f;

    [Header("내부 상태 보조")]
    [SerializeField] private float inputX;        // 연속 입력
    [SerializeField] private float rawX;          // 원시 입력
    [SerializeField] private int jumpCount;
    [SerializeField] private float lastGroundTime;
    [SerializeField] private float lastJumpPress;
    [SerializeField] private float wallLock;
    [SerializeField] private float originGravity;

    [Header("Attack")]  
    [SerializeField] float attackStateTime=1.5f;
    [SerializeField] float attackLimitTime=1f;
    [SerializeField] float attackRemainTime;
    [SerializeField] private float attackForce=8;
    public GameObject attackObject;
    private AttackAnimation attackAnim;
    [Header("Attack details")]
    [SerializeField]protected float attackRadius = 3.5f;
    [SerializeField]protected Transform attackPoint;
    [SerializeField]protected LayerMask whatIsTarget;
    

    [Header("입력 요청 (우선순위용)")]
    [SerializeField] bool reqAttack;
    [SerializeField] bool reqJump;
    [SerializeField] bool reqDash;
    [SerializeField] bool reqMove; // “이동 의도” 플래그


    // ===== 수치 상수 =====
    const float eps = 0.01f;
    private int lastKeyDir=0;
    private MouseDirectionFromPlayer mouseDirScript;




























    protected override void Awake()
    {
        base.Awake();
        rb              = GetComponent<Rigidbody2D>();
        anim            = GetComponent<Animator>();
        mouseDirScript  = GetComponent<MouseDirectionFromPlayer>();
        originGravity   = GetComponent<Rigidbody2D>().gravityScale;
        if (attackObject != null)
            attackAnim = attackObject.GetComponentInChildren<AttackAnimation>();

        jumpCount       = maxJumpCount;
    }

    // ===================== 입력 =====================
    public void OnMove(InputAction.CallbackContext ctx)
    {
        
        rawX = ctx.ReadValue<Vector2>().x;
        inputX = Mathf.Clamp(rawX, -1f, 1f);
        reqMove = Mathf.Abs(inputX) > eps;

        // === 키보드 기준으로 마지막 방향 갱신 ===
        var kb = Keyboard.current;
        if (kb != null)
        {
            // 이 프레임에 새로 눌린 키 기준으로 갱신
            if (kb.aKey.wasPressedThisFrame) lastKeyDir = -1;
            if (kb.dKey.wasPressedThisFrame) lastKeyDir = 1;

            // 둘 다 누르고 있을 때도 lastKeyDir 유지
            // 둘 중 하나만 눌려 있으면 그쪽으로 덮어써도 OK

            // 둘 다 떼었으면, 움직임만 멈추고 faceDir은 마지막 방향 유지
            if (!kb.aKey.isPressed && !kb.dKey.isPressed)
            {
                reqMove = false;
                inputX = 0f;
            }
        }

        // 실제 바라보는 방향은 lastKeyDir로
        if (reqMove)
        {
            faceDir = lastKeyDir;
        }
    }
    private bool prevLeftKey;
    private bool prevRightKey;

    public void SetMoveDir()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        bool left  = kb.aKey.isPressed;
        bool right = kb.dKey.isPressed;

        bool leftDown  = left  && !prevLeftKey;
        bool rightDown = right && !prevRightKey;

        // 이전 프레임 상태 갱신
        prevLeftKey  = left;
        prevRightKey = right;

        // === 1) 둘 다 눌려 있는 경우 ===
        if (left && right)
        {
            // 이번 프레임에 새로 눌린 키가 있으면 그 방향을 마지막 키로
            if (leftDown)  lastKeyDir = -1;
            if (rightDown) lastKeyDir =  1;

            faceDir = lastKeyDir;
            reqMove = true;
            inputX  = faceDir;   // -1 또는 1
        }
        // === 2) 왼쪽만 눌린 경우 ===
        else if (left)
        {
            lastKeyDir = -1;
            faceDir = -1;
            reqMove = true;
            inputX  = -1f;
        }
        // === 3) 오른쪽만 눌린 경우 ===
        else if (right)
        {
            lastKeyDir =  1;
            faceDir =  1;
            reqMove = true;
            inputX  =  1f;
        }
        // === 4) 둘 다 안 눌린 경우 ===
        else
        {
            reqMove = false;
            inputX  = 0f;
            // faceDir 은 마지막 바라보던 방향 유지
        }
    }
    public void OnJump(InputAction.CallbackContext ctx)
    {
        if (ctx.started)
        {
            reqJump = true;
            lastJumpPress = Time.time;
        }
    }

    public void OnDash(InputAction.CallbackContext ctx)
    {
        if (ctx.performed) reqDash = true;
    }

    public void OnAttack(InputAction.CallbackContext ctx)
    {
        if (ctx.performed) reqAttack = true;
    }

    public void Handle_Animations()
    {
        anim.SetFloat("yVelocity",rb.linearVelocityY);
        anim.SetBool("isGround",isGround);
        anim.SetBool("isWall", isWall);
    }
    void Handle_Movement()
    {
        if (Current == ActionState.Dash || Current == ActionState.Attack)
            return;

        if (Current == ActionState.WallSlide)
        {
            HandleWallSlide(Time.fixedDeltaTime);
            return; // 벽슬라이드 중엔 아래쪽 처리(수평이동) 스킵
        }
        // 🔹 락 중엔 수평속도 손대지 않음 (계속 같은 속도로 날아감)
        if (wallLock > 0f || (inputX ==0&&!isGround))   return;
        if (Current == ActionState.WallSlide)           return;
        if (Current == ActionState.Attack)              return;

        float controlRate = (isAir && !isWall) ? airControl : 1f;
        float targetX = inputX * moveSpeed * controlRate;

        float curX = rb.linearVelocityX;
        float rate = (Mathf.Abs(targetX) > 0.01f) ? accel : deccel;

        rb.linearVelocityX = Mathf.MoveTowards(curX, targetX, rate * Time.fixedDeltaTime);
    }

    void HandleWallSlide(float dt)
    {
        // 위로 튀는 중이면 무시
        if (rb.linearVelocityY > 0f) {
            wallSlideElapsed = 0f;
            return;
        }
        else wallSlideAnchorY = rb.position.y; // 붙은 순간의 Y 를 기억

        wallSlideElapsed += dt;

        // 🔹 1) 정지 구간: 완전히 멈추고, 위치도 고정
        if (wallSlideElapsed < wallSlideHoldTime)
        {
            // Y 속도 0으로 강제
            rb.linearVelocityY = 0f;
            // Y 위치를 아예 고정해서 살살 내려가는 것도 막기
            rb.position = new Vector2(rb.position.x, wallSlideAnchorY);
            return;
        }
        

        // 2) holdTime 이후부터 slowTime 동안 서서히 wallSlideMaxSpeed로 보간
        float t = (wallSlideElapsed - wallSlideHoldTime) / wallSlideSlowTime;
        t = Mathf.Clamp01(t);   // 0 ~ 1

        float targetY = wallSlideMaxSpeed;  // ex) -4f
        float newY = Mathf.Lerp(0f, targetY, t);

        rb.linearVelocityY = newY;
    }


























    // ===================== 메인 루프 =====================
    protected override void Update() {
        if(isDie)
        {
            Enter(ActionState.Dead);
            return;
        }

        base.Update();

        SetMoveDir();
        anim.speed = animSpeed;
        SenseBigState();                 // 1) 대전제 판정
        var desired = DecideNextState(); // 2) 요구 상태 결정(우선순위 반영)
        TryTransition(desired);          // 3) 현재/요구/대전제 관계 판단 → 전환

        TickTimers();

        debugSummary = $"{Current} | G:{isGround} W:{isWall} A:{isAir} | Jump:{jumpCount}";
        Debug.Log(debugSummary);
    }

    void FixedUpdate()
    {
        Handle_Animations();
        Handle_Movement();
        ClampFall();
    }

    // ===================== 1) 대전제 판정 =====================
    private void GroundCheck()=>isGround = Physics2D.Raycast(transform.position, Vector2.down, groundCheckDistance, whatIsGround);
    private void WallCheck()=>isWall = Physics2D.Raycast(transform.position, Vector2.right * faceDir, wallCheckDistance, WhatIsWall);
    void SenseBigState()
    {
        // Ground 우선
        GroundCheck();
        if (isGround)
        {
            isWall = false;
            isAir  = false;
            lastGroundTime = Time.time;
            
            return;
        }

        // 그 다음 Wall
        WallCheck();
        if (isWall)
        {
            isAir = false;
            jumpCount += maxJumpCount > jumpCount ? 1 : 0;
            return;
        }

        // 마지막 Air
        isAir = true;
    }

    // ===================== 2) 요구 상태 결정 =====================
    ActionState DecideNextState()
    {
        // 우선순위: Attack > Jump/WallJump > Dash > Move
        if (reqAttack && attackRemainTime<=0) return ActionState.Attack;

        if (reqJump && jumpCount > 0)
        {
            if (isGround)
                return ActionState.Jump;  
            if (isWall)
                return ActionState.WallJump;  
        } 
        if (isWall && rb.linearVelocityY<=0) 
        {
            return ActionState.WallSlide;
        }

        if (reqDash && isGround) {
            return ActionState.Dash;
        }
        // 패시브 기본값 (대전제 안에서 자연스럽게)
        if (isGround) return reqMove ? ActionState.Move : ActionState.Idle;
        if (isWall)   return ActionState.WallSlide;
        else return ActionState.Fall; // isAir
    }

    // ===================== 3) 전환 판단 및 실행 =====================
    void TryTransition(ActionState next)
    {
        reqAttack = false; reqJump = false; reqDash = false;


        if (Current == ActionState.Dead) return;
        if (Current == ActionState.Attack) return;
        
        if (next == ActionState.Dead)
        {
            Enter(ActionState.Dead);
            return;
        }
        if (Current == ActionState.Dash) return;

        if (!CanEnter(next)) return;

        if (next != Current) Enter(next);

    }
    bool CanEnter(ActionState next)
    {
        // 현재/요구/대전제 관계 규칙을 한 곳에 정리
        // 관계 중심 : 다른 상태와 우선순위 경쟁
        switch (next)
        {
            case ActionState.Dead:
                return true;

            case ActionState.Attack:
                return Current != ActionState.Dead;

            case ActionState.Jump:
                // 실패: Dead/Attack/대시 중
                if (Current == ActionState.Dead || Current == ActionState.Attack) return false;
                // 전제: Ground or Coyote or 남은점프>0
                // bool canCoyote = (Time.time - lastGroundTime) <= coyoteTime;
                // return isGround || canCoyote || jumpCount > 0;
                return true;
            
            case ActionState.Fall:
                if (Current == ActionState.Dead ) return false;
                return true;

            case ActionState.WallJump:
                // 실패: Dead/Attack/대시 중
                if (Current == ActionState.Dead || Current == ActionState.Attack || Current == ActionState.Dash) return false;
                // 전제: isWall -> TryTransition()에서 점검했음
                return true;

            case ActionState.Dash:
                // 실패: Dead/Attack/쿨다운
                if (Current == ActionState.Dash) return false;
                if (Current == ActionState.Dead || Current == ActionState.Attack) return false;
                if (Current == ActionState.Jump || Current == ActionState.Fall) return false;

                return true;

            case ActionState.Move:
                // 실패: Dead/Attack/대시
                if (Current == ActionState.Dead || Current == ActionState.Attack || Current == ActionState.Dash) return false;
                return isGround && reqMove;

            case ActionState.Idle:
                if (Current == ActionState.Dead || Current == ActionState.Attack || Current == ActionState.Dash) return false;
                return isGround;

            case ActionState.WallSlide:
                if (Current == ActionState.Dead || Current == ActionState.Attack || Current == ActionState.Dash) return false;
                return true;

            default:
                return false;
        }
    }
    void Enter(ActionState next)
    {
        // 상태 나갈 때 정리(필요 최소만)
        Exit(Current);
        Current = next;
        rb.gravityScale = originGravity;
        if (next == ActionState.WallSlide)
        {
            wallSlideElapsed = 0f;
        }
        // 들어가며 애니/플래그/즉시동작
        switch (Current)
        {
            case ActionState.Dead:
                anim.SetBool("isDead", true);
                rb.linearVelocity = Vector2.zero;
                break;

            case ActionState.Attack:
            
                attackRemainTime = attackLimitTime;        // 잠금 시간
                DamageTargets();
                rb.gravityScale = originGravity/4;
                if (mouseDirScript.MouseDirection.x>0 && faceDir == -1 ||
                    mouseDirScript.MouseDirection.x<0 && faceDir == 1)
                    Flip();
                Vector2 WorldMouseDir=mouseDirScript.MouseDirection * faceDir;
                attackObject.SetActive(true);
                attackAnim.Play();
                attackObject.transform.right = WorldMouseDir;
                // 공격반동 설정
                rb.linearVelocity /=3;
                rb.AddForce(mouseDirScript.MouseDirection * attackForce,ForceMode2D.Impulse);
                anim.SetBool("isRun", false);
                anim.SetTrigger("isAttack");
                break;

            case ActionState.Jump:
                jumpCount = Mathf.Max(0, jumpCount - 1);
                rb.linearVelocityY = 0f;
                rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
                break;

            case ActionState.Fall:
                break;

            case ActionState.WallJump:
                // 1) 벽 반대 방향을 바라보도록 뒤집기
                faceDir *= -1;
                Flip();
                anim.SetBool("isWall", false);

                // 2) 잠깐 입력 잠그기 (수평 속도 유지용)
                wallLock = wallJumpControlLock;

                // 3) 점프 카운트 소비
                jumpCount = Mathf.Max(0, jumpCount - 1);

                // 4) 기존 속도 리셋 (원한다면 전체 리셋도 가능)
                rb.linearVelocity = Vector2.zero;
                // 혹은 수평만 유지/리셋하고 싶다면:
                // rb.linearVelocityY = 0f;

                // 5) faceDir을 이용해 “바깥쪽 + 위쪽”으로 점프 벡터 구성
                Vector2 dir = new Vector2(faceDir * wallJumpDir.x, wallJumpDir.y);
                dir.Normalize(); // 혹은 wallJumpDir을 애초에 정규화해서 써도 OK
                isWall = false;
                rb.AddForce(dir * wallJumpForce, ForceMode2D.Impulse);

                Current = ActionState.Jump;
                break;

            case ActionState.Dash:
                rb.linearVelocityX = faceDir * dashSpeed;
                anim.SetTrigger("isRoll");
                break;

            case ActionState.Move:
                jumpCount = maxJumpCount;
                anim.SetBool("isRun", true);
                break;

            case ActionState.Idle:
                jumpCount = maxJumpCount;
                anim.SetBool("isRun", false);
                break;

            case ActionState.WallSlide:
                rb.gravityScale /=2;
                
                jumpCount = Mathf.Max(jumpCount + 1,maxJumpCount);
                break;
            }
    }
    void Exit(ActionState prev)
    {
        switch (prev)
        {
            case ActionState.Move:
                anim.SetBool("isRun", false);
                break;
            case ActionState.Attack:
                
                break;
            
            // Attack/Dash/Jump/WallJump 등은 타이머로 자연 종료
        }
    }


    public void ExitCurrentState()
    {
        if (isAir) Enter(ActionState.Fall) ;
        else if (isWall) Enter(ActionState.WallSlide);
        else if (isGround) Enter(ActionState.Idle);

    }

    public void DamageTargets()
    {
        Collider2D[] enemyColliders = Physics2D.OverlapCircleAll(attackPoint.position, attackRadius, whatIsTarget);
        
        foreach (Collider2D enemy in enemyColliders)
        {
            // 적 엔티티
            HP_System entityTarget = enemy.GetComponent<HP_System>();
            entityTarget.Health_Reduce();
             // 총알, 가구
        }
       
    }


    // ===================== 보조(타이머/애니/이동) =====================
    void TickTimers()
    {
        // --- Attack 타이머 ---
        if (attackRemainTime > 0f)
        {
            attackRemainTime -= Time.deltaTime;
            // if (attackRemain <= 0f && Current == ActionState.Attack)
            // {
            //     // 끝나면 대전제 패시브로
            //     Enter( isGround ? ActionState.Idle
            //         : isWall   ? ActionState.WallSlide
            //         : ActionState.Fall);
            // }
        }

        // --- WallJump 이동제한 타이머 ---
        float prev = wallLock;         // 해제 순간(하강 에지) 검출용 백업
        if (wallLock > 0f) wallLock -=Time.deltaTime;

        // ★ '락 > 0' → '락 ≤ 0'이 된 바로 그 프레임에만 1회 동기화
        // bool justUnlocked = (prev > 0f && wallLock <= 0f) && Current == ActionState.WallJump;
        bool justUnlocked = prev > 0f && wallLock <= 0f;
        if (justUnlocked
            && Mathf.Abs(inputX) > 0.01f           // 입력이 있을 때만
            && Current != ActionState.Dash         // 우선순위 액션 방해 방지
            && Current != ActionState.Attack)
        {
            faceDir = (inputX > 0f) ? 1 : -1;      // ← 여기서 단 한 번만 입력 방향으로 갱신
        }
    }

    protected override void Handle_Flip()
    {
        if (faceDir == 0) return;
        if (Current == ActionState.Attack) return;
        Flip();
    }
    protected override void Flip()
    {
        // faceDir은 이미 SetMoveDir / Attack / WallJump 에서 결정했다는 가정
        var s = transform.localScale;
        s.x = Mathf.Abs(s.x) * faceDir;   // 1 또는 -1
        transform.localScale = s;
    }





    void ClampFall()
    {
        if (rb.linearVelocityY < maxFallSpeed) { rb.linearVelocityY = maxFallSpeed; }
    }

    
    void OnDrawGizmosSelected()
    {
        // Ground Check Ray
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position - transform.up * groundCheckDistance);

        // Wall Check Ray
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + transform.right * wallCheckDistance * faceDir);

        // Attack 
        if(attackPoint)
            Gizmos.DrawWireSphere(attackPoint.position,attackRadius);
    }
}
