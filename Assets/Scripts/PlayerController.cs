using System.Collections;
using NUnit.Framework;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

// 최소 구성: 구체 콜라이더 사용
[RequireComponent(typeof(PlayerInput))]
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
    [SerializeField] private float groundCheckWide = .5f;

    [Header("내부 상태 보조")]
    [SerializeField] private float inputX;        // 연속 입력
    [SerializeField] private Vector2 rawInput;          // 원시 입력
    [SerializeField] private int jumpCount;
    [SerializeField] private float lastGroundTime;
    [SerializeField] private float lastJumpPress;
    [SerializeField] private float wallLockTimer;
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
        mouseDirScript  = GetComponent<MouseDirectionFromPlayer>();
        originGravity   = GetComponent<Rigidbody2D>().gravityScale;
        if (attackObject != null)
            attackAnim = attackObject.GetComponentInChildren<AttackAnimation>();

        jumpCount       = maxJumpCount;
    
    }

    // ===================== 입력 =====================
    public void OnMove(InputAction.CallbackContext ctx)
    {
        if (Current == ActionState.Dead) return;

        if (wallLockTimer > 0f) return;
        rawInput   = ctx.ReadValue<Vector2>();
        inputX = Mathf.Clamp(rawInput.x, -1f, 1f);
        reqMove = Mathf.Abs(inputX) > eps;

        var kb = Keyboard.current;
        if (kb != null)
        {
            bool leftDown   = kb.aKey.wasPressedThisFrame;
            bool rightDown  = kb.dKey.wasPressedThisFrame;
            bool leftUp     = kb.aKey.wasReleasedThisFrame;
            bool rightUp    = kb.dKey.wasReleasedThisFrame;

            // ======= 여기서 "입력 변화" 체크 =======
            bool anyNewPress = leftDown || rightDown;
            bool anyRelease  = leftUp   || rightUp;

            if ((anyNewPress || anyRelease) && Current == ActionState.Move && onSlope /* && !isJumping */)
            {
                // rb.linearVelocityY = 0;
            }
            // ===================================

            // 이 프레임에 새로 눌린 키 기준으로 lastKeyDir 갱신
            if (leftDown)  lastKeyDir = -1;
            if (rightDown) lastKeyDir =  1;

            // 둘 다 떼었으면, 움직임만 멈추고 faceDir은 마지막 방향 유지
            if (!kb.aKey.isPressed && !kb.dKey.isPressed)
            {
                reqMove = false;
                inputX  = 0f;
            }
        }

        // 실제 바라보는 방향은 lastKeyDir로
        if (reqMove)
        {
            faceDir = lastKeyDir;
        }
        
    }
    private IEnumerator IgnorePlatform()
    {
        gameObject.layer = LayerMask.NameToLayer("Platform_Ignore");
        yield return new WaitForSeconds(0.2f);
        gameObject.layer = LayerMask.NameToLayer("Player");

    }

    private bool prevLeftKey;
    private bool prevRightKey;

    public void SetMoveDir()
    {
        if (wallLockTimer > 0f) return;

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
        if (ctx.performed) 
        {
            if(onPlatform&&inputX==0) 
                StartCoroutine(IgnorePlatform());
            else 
                reqDash = true;
        }
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
        if (Current == ActionState.Attack)
            return;

        if (Current == ActionState.WallSlide)
        {
            HandleWallSlide(Time.fixedDeltaTime);
            return;
        }

        float dt = Time.fixedDeltaTime;
        // 대시 로직 편입
        if (Current == ActionState.Dash)
        {
            HandleGroundMove(faceDir,dashSpeed,dt);
            return;
        }

        if (Current == ActionState.Jump 
            || Current == ActionState.WallJump
            || Current == ActionState.Fall)
        {
            HandleAirMove(dt);   // 기존 공중 이동 함수 그대로
            return;
        }

        // === 여기부터는 "지면 위"에서만 적용 ===
        CheckSlope();            // 레이로 법선 / 슬로프 여부 계산
        HandleGroundMove(inputX,moveSpeed,dt);
        
    }


    [Header("[ Air Control ]")]
    [SerializeField] float airAccel  = 200f;
    [SerializeField] float airDeccel = 200f;

    void HandleAirMove(float dt)
    {
        // 🔹 벽점프 락 동안에는 X속도를 유지 (중력만 작용)
        if (wallLockTimer > 0f)
            return;

        float targetX = inputX * moveSpeed * airControl;
        float curX    = rb.linearVelocityX;

        bool hasInput = Mathf.Abs(inputX) > eps;
        float rate    = hasInput ? airAccel : airDeccel;

        float newX = Mathf.MoveTowards(curX, targetX, rate * dt);

        rb.linearVelocity = new Vector2(newX, rb.linearVelocityY);
    }

    void HandleGroundMove(float x, float speed, float dt)
    {
        float absInput = Mathf.Abs(x);

        // === 1) 기본 접선(슬로프 방향) 계산 ===
        Vector2 baseTangent = GetSlopeTangent(groundNormal); // (지면 기준 오른쪽 향하는 벡터)
        Vector2 tangent = baseTangent;

        // 입력이 있을 때만, 입력 부호로 방향 결정
        if (absInput > eps)
            tangent = baseTangent * Mathf.Sign(x);

        // === 2) 현재 속도를 접선 방향으로 투영 ===
        Vector2 vel = rb.linearVelocity;
        float speedOnTangent = Vector2.Dot(vel, tangent); // 접선 방향 스칼라 속도

        // === 3) 목표 속도 설정 ===
        float targetSpeed;

        if (absInput > eps)
        {
            // 입력 있을 때: 항상 +moveSpeed 쪽으로 (방향은 tangent가 이미 들고 있음)
            targetSpeed = speed;
        }
        else
        {
            // 입력 없으면 0으로 감속
            targetSpeed = 0f;
        }

        // 가속/감속 비율
        float rate = (absInput > eps) ? accel : deccel;

        // === 4) 스칼라 속도를 보간 ===
        float newSpeedOnTangent = Mathf.MoveTowards(speedOnTangent, targetSpeed, rate * dt);

        // === 5) 최종 속도 벡터 구성 ===
        Vector2 finalVel = tangent * newSpeedOnTangent;

        // 지면에선 법선 방향 속도는 0으로 정리 (튀는 거 방지)
        rb.linearVelocity = finalVel;
    }


    [Header("[ Slope ]")]
    [SerializeField] private Vector2 slopeCheck;      // 발밑 기준 위치
    [SerializeField] private float slopeCheckDistance = 0.5f; // 레이 길이
    [SerializeField] private float maxSlopeAngle = 45f; // 허용하는 최대 경사각
    [SerializeField] private LayerMask whatIsSlope; // 허용하는 최대 경사각

    private Vector2 groundNormal = Vector2.up;
    private float slopeAngle;
    [SerializeField] private bool onSlope;
    void CheckSlope()
    {
        Vector3 frontSlopeOffset = new Vector2(slopeCheck.x * faceDir, slopeCheck.y);
        Vector3 backSlopeOffset = new Vector2(-slopeCheck.x * faceDir, slopeCheck.y);

        RaycastHit2D hitFront = Physics2D.Raycast(
            transform.position + frontSlopeOffset,
            Vector2.down,
            slopeCheckDistance,
            whatIsSlope
        );
        RaycastHit2D hitBack = Physics2D.Raycast(
            transform.position + backSlopeOffset,
            Vector2.down,
            slopeCheckDistance,
            whatIsSlope
        );

        if (hitFront || hitBack)
        {
            
            groundNormal =  hitFront? hitFront.normal:
                            hitBack?  hitBack.normal:
                            groundNormal;
            slopeAngle   = Vector2.Angle(groundNormal, Vector2.up);
            onSlope = (slopeAngle >= maxSlopeAngle-10 && slopeAngle <= maxSlopeAngle) || (slopeAngle <= -maxSlopeAngle+10 && slopeAngle >= -maxSlopeAngle);
            rb.gravityScale = onSlope == true ?0 :originGravity;
        }
        else
        {
            rb.gravityScale = originGravity;
            groundNormal = Vector2.up;
            slopeAngle   = 0f;
            onSlope      = false;
        }
    }

    // normal 기준으로 오른쪽 방향 접선 구하기
    Vector2 GetSlopeTangent(Vector2 normal)
    {
        // (0,1) 기준이면 (1,0) 이 나오는 패턴
        return new Vector2(normal.y, -normal.x).normalized;
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























[Header("OneWay Platform")]
[SerializeField] private LayerMask WhatIsPlatform;
[SerializeField] private bool onPlatform;


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

    // ===================== 1) 대전제 판정 =============== =====
    private void GroundCheck(){
        // if (!isGround && Current == ActionState.Fall && rb.linearVelocityY == 0)
        // {
        //     isGround =true;
        //     return;
        // }
        if (GroundCheckBy3Rays(whatIsGround,groundCheckWide)) 
        {
            isGround    = true;
            onPlatform  = false;
        }
        else if (GroundCheckBy3Rays(WhatIsPlatform,groundCheckWide)
                && rb.linearVelocityY == 0)
        {
            isGround    = true;
            onPlatform  = true;
        }
        else
        {
            isGround    = false;
            onPlatform  = false;
        }
    }
    public bool GroundCheckBy3Rays(LayerMask targetLayer,float wide)
    {
        Vector3 xOffset = new Vector2(wide,0);
        return Physics2D.Raycast(transform.position + xOffset, Vector2.down, groundCheckDistance, targetLayer)
            || Physics2D.Raycast(transform.position - xOffset, Vector2.down, groundCheckDistance, targetLayer)
            || Physics2D.Raycast(transform.position, Vector2.down, groundCheckDistance, targetLayer);

    }
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
        // if (wallLockTimer > 0f)
        // {
        //     isWall = false;
        //     isAir  = true;
        //     return;
        // }
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
                wallLockTimer = wallJumpControlLock;

                

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
        if(wallLockTimer>0)
            wallLockTimer -= Time.deltaTime;
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
        Vector3 xOffset = new Vector2(groundCheckWide,0);
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position + xOffset, transform.position + xOffset + Vector3.down * groundCheckDistance);
        Gizmos.DrawLine(transform.position - xOffset, transform.position - xOffset + Vector3.down * groundCheckDistance);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * groundCheckDistance);

        // Wall Check Ray
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + transform.right * wallCheckDistance * faceDir);

        // Attack 
        if(attackPoint)
            Gizmos.DrawWireSphere(attackPoint.position,attackRadius);
        
        // Slope Check
        Gizmos.color = Color.yellow ;
        Vector3 slopeOffsetFront = new Vector2(slopeCheck.x * faceDir, slopeCheck.y);
        Vector3 slopeOffsetBack = new Vector2(-slopeCheck.x * faceDir, slopeCheck.y);
        Gizmos.DrawLine(transform.position + slopeOffsetFront, transform.position + slopeOffsetFront + (Vector3)(Vector2.down * slopeCheckDistance));
        Gizmos.DrawLine(transform.position + slopeOffsetBack, transform.position + slopeOffsetBack + (Vector3)(Vector2.down * slopeCheckDistance));
        
    }
}
