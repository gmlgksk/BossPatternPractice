using UnityEngine;
using UnityEngine.InputSystem;

// 최소 구성: 구체 콜라이더 사용
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CapsuleCollider2D))]
[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(Animator))]
public class PlayerController : MonoBehaviour
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
    Rigidbody2D rb;
    Animator anim;


    // ===== 기본 파라미터 =====

    [Header("Anim")]
    [SerializeField] float animSpeed = 1f;
    
    [Header("Move")]
    [SerializeField] float moveSpeed = 8f;
    [SerializeField] float accel = 60f;
    [SerializeField] float deccel = 70f;
    [SerializeField] float airControl = 0.8f;
    [SerializeField] float maxFallSpeed = -20f;

    [Header("Jump")]
    [SerializeField] float jumpForce = 12f;
    [SerializeField] int   maxJumpCount = 2;
    [SerializeField] float coyoteTime = 0.1f;
    [SerializeField] float jumpBuffer = 0.1f;

    [Header("Wall")]
    [SerializeField] float wallSlideMaxSpeed = -2.5f;
    [SerializeField] Vector2 wallJumpDir = new Vector2(1.0f, 1.1f);
    [SerializeField] float wallJumpForce = 12f;
    [SerializeField] float wallJumpControlLock = 0.15f;

    [Header("Dash")]
    [SerializeField] float dashSpeed = 18f;

    [Header("Detect")]
    [SerializeField] LayerMask groundMask;
    [SerializeField] LayerMask wallMask;
    [SerializeField] Transform groundCheck;
    [SerializeField] Transform wallCheck;
    [SerializeField] float groundDist = 0.12f;
    [SerializeField] Vector2 wallBox = new Vector2(0.12f, 0.9f);
    [Header("내부 상태 보조")]
    [SerializeField] int faceDir = 1;     // 1 오른쪽, -1 왼쪽
    [SerializeField] float inputX;        // 연속 입력
    [SerializeField] float rawX;          // 원시 입력

    [SerializeField] int jumpCount;
    [SerializeField] float lastGroundTime;
    [SerializeField] float lastJumpPress;
    [SerializeField] float wallLock;
    [Header("Attack")]  

    [SerializeField] float attackLimitTime=3f;
    [SerializeField] float attackRemainTime;
    [Header("입력 요청 (우선순위용)")]
    [SerializeField] bool reqAttack, reqJump, reqDash;
    [SerializeField] bool reqMove; // “이동 의도” 플래그

    // ===== 수치 상수 =====
    const float eps = 0.01f;
    private int lastKeyDir=0;
    private MouseDirectionFromPlayer mouseDirScript;
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        mouseDirScript=GetComponent<MouseDirectionFromPlayer>();
        
        jumpCount = maxJumpCount;
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
    private bool prevLeft;
    private bool prevRight;
    public void SetMoveDir()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        bool left  = kb.aKey.isPressed;
        bool right = kb.dKey.isPressed;

        bool leftDown  = left  && !prevLeft;
        bool rightDown = right && !prevRight;

        // 이전 프레임 상태 갱신
        prevLeft  = left;
        prevRight = right;

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

    // ===================== 메인 루프 =====================
    void Update()
    {
        SetMoveDir();
        anim.speed = animSpeed;
        SenseBigState();                 // 1) 대전제 판정
        var desired = DecideNextState(); // 2) 요구 상태 결정(우선순위 반영)
        TryTransition(desired);          // 3) 현재/요구/대전제 관계 판단 → 전환

        TickTimers();

        FlipVisual();

        debugSummary = $"{Current} | G:{isGround} W:{isWall} A:{isAir} | Jump:{jumpCount}";
        Debug.Log(debugSummary);
    }

    void FixedUpdate()
    {
        anim.SetFloat("yVelocity",rb.linearVelocityY);
        anim.SetBool("isGround",isGround);
        ApplyMovement(Time.fixedDeltaTime);
        ClampFall();
    }

    // ===================== 1) 대전제 판정 =====================
    void SenseBigState()
    {
        // Ground 우선
        Vector2 gOrigin = groundCheck ? (Vector2)groundCheck.position : (Vector2)transform.position + Vector2.down * 0.5f;
        isGround = Physics2D.Raycast(gOrigin, Vector2.down, groundDist, groundMask);

        if (isGround)
        {
            isWall = false;
            isAir  = false;
            lastGroundTime = Time.time;
            jumpCount = maxJumpCount;
            return;
        }

        // 그 다음 Wall
        Vector2 wCenter = wallCheck ? (Vector2)wallCheck.position : (Vector2)transform.position + new Vector2(faceDir * 0.4f, 0f);
        isWall = Physics2D.OverlapBox(wCenter, wallBox, 0f, wallMask);

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
        
        
        // // 버퍼/코요테 허용
        // bool bufferedJump = reqJump && (Time.time - lastJumpPress <= jumpBuffer);
        // bool canCoyote    = (Time.time - lastGroundTime) <= coyoteTime;

        // if (bufferedJump)
        // {
        //     if (isWall && jumpCount > 0) return ActionState.WallJump;                 // 벽 전제 → 벽점프
        //     if (isGround && canCoyote && jumpCount > 0) return ActionState.Jump; // 일반/이중 점프
        // }
        if (reqJump && isWall && jumpCount > 0) 
        {
            return ActionState.WallJump;
        }
        if (reqJump && isGround && jumpCount > 0)
        {
            return ActionState.Jump;  
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

        // if (!CanEnter(next)) // 대전제/관계 위배 시, 패시브로 롤백
        // {
        //     // 대전제 기반 기본상태로
        //     next = (isGround) ? (reqMove ? ActionState.Move : ActionState.Idle)
        //          : (isWall) ? ActionState.WallSlide
        //          : ActionState.Fall;
        //     if (!CanEnter(next)) return; // 안전장치
        // }
        
        if (!CanEnter(next)) return;

        if (next != Current) Enter(next);

    }

    public void ExitCurrentState() => Current = ActionState.Idle;



    bool CanEnter(ActionState next)
    {
        // 현재/요구/대전제 관계 규칙을 한 곳에 정리
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
                return isGround || jumpCount > 0;
            
            case ActionState.Fall:
                if (Current == ActionState.Dead ) return false;
                return true;

            case ActionState.WallJump:
                // 실패: Dead/Attack/대시 중
                if (Current == ActionState.Dead || Current == ActionState.Attack || Current == ActionState.Dash) return false;
                // 전제: isWall
                return isWall;

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
                return isWall;

            default:
                return false;
        }
    }
    [SerializeField] private float attackForce=8;
    public GameObject attackSprite;
    void Enter(ActionState next)
    {
        // 상태 나갈 때 정리(필요 최소만)
        Exit(Current);
        Current = next;

        // 들어가며 애니/플래그/즉시동작
        switch (Current)
        {
            case ActionState.Attack:
                attackRemainTime = attackLimitTime;        // 잠금 시간
                if (mouseDirScript.MouseDirection.x>0 && faceDir == -1 ||
                    mouseDirScript.MouseDirection.x<0 && faceDir == 1) faceDir *= -1;
                Filp();

                Vector2 WorldMouseDir=mouseDirScript.MouseDirection * faceDir;

                
                attackSprite.gameObject.SetActive(true);
                attackSprite.transform.right = WorldMouseDir;

                // 공격반동 설정

                rb.linearVelocity = Vector2.zero;
                // rb.AddForce(mouseDirScript.MouseDirection * attackForce,ForceMode2D.Impulse);
                rb.linearVelocity = mouseDirScript.MouseDirection * attackForce;

                anim.SetBool("isRunning", false);
                anim.SetBool("isWallSlide", false);
                anim.SetBool("isJump", false);
                anim.SetTrigger("isAttack");

                break;

            case ActionState.Jump:
                // 점프 카운트 소비, 수직속도 리셋 후 가속
                // if (!isGround) jumpCount = Mathf.Max(0, jumpCount - 1);
                // else           jumpCount = maxJumpCount - 1;
                jumpCount = Mathf.Max(0, jumpCount - 1);
                rb.linearVelocityY = 0f;
                rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
                break;

            case ActionState.Fall:
                break;

            case ActionState.WallJump:
                // 벽 반대 방향으로 튕김 + 입력 잠깐 잠금
                faceDir *= -1;
                var away = new Vector2(faceDir, 1);
                wallLock = wallJumpControlLock;
                rb.linearVelocityY = 0f;
                rb.AddForce(wallJumpDir * wallJumpForce * away, ForceMode2D.Impulse);
                anim.SetTrigger("isWallJump");
                break;

            case ActionState.Dash:
                rb.linearVelocityX = faceDir * dashSpeed;
                anim.SetTrigger("isRoll");
                break;

            case ActionState.Move:
                anim.SetBool("isRunning", true);
                break;

            case ActionState.Idle:
                anim.SetBool("isRunning", false);
                anim.SetBool("isWallSlide", false);
                anim.SetBool("isJump", false);
                break;

            case ActionState.WallSlide:
                anim.SetBool("isWallSlide", true);
                break;
            }
    }

    void Exit(ActionState prev)
    {
        switch (prev)
        {
            case ActionState.Move:
                anim.SetBool("isRunning", false);
                break;
            case ActionState.WallSlide:
                anim.SetBool("WallSlide", false);
                break;
            case ActionState.Jump:
                anim.SetBool("isJump", false);
                break;
            // Attack/Dash/Jump/WallJump 등은 타이머로 자연 종료
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


    void FlipVisual()
    {
        if (faceDir == 0) return;
        if (Current == ActionState.Attack) return; ;
        Filp();
    }

    private void Filp()
    {
        var s = transform.localScale;
        s.x = Mathf.Abs(s.x) * faceDir;
        transform.localScale = s;
    }

    void ApplyMovement(float dt)
    {
        // === CHANGE: 벽점프 락 중엔 x 가속/감속을 건너뜀 ===
        // if (Current == ActionState.WallJump && wallLock > 0f)
        // {
        //     // y만 자연 낙하/제한 처리
        //     rb.linearVelocityY = maxFallSpeed;
        //     return; // ← 수평은 유지
        // }

        if (Current == ActionState.Dash || Current == ActionState.Attack) return;

        // 벽슬라이드 속도제한
        if (Current == ActionState.WallSlide && rb.linearVelocityY < wallSlideMaxSpeed)
            rb.linearVelocityY = wallSlideMaxSpeed;

        bool lockH = wallLock > 0f || Current == ActionState.Attack;

        float targetX = 0f;
        //공중에서 속도 조정
        if (!lockH)
        {
            float controlRate = (isAir && !isWall) ? airControl : 1f;
            targetX = inputX * moveSpeed * controlRate;
        }
        else
        {
            // === CHANGE: 락 중엔 x를 건드리지 않게 현재값을 목표로 둠 ===
            targetX = rb.linearVelocityX;
        }

        float rate = (Mathf.Abs(targetX) > 0.01f) ? accel : deccel;
        rb.linearVelocityX = Mathf.MoveTowards(rb.linearVelocityX, targetX, rate * dt);

    }


    void ClampFall()
    {
        if (rb.linearVelocityY < maxFallSpeed) { rb.linearVelocityY = maxFallSpeed; }
    }

    // 외부에서 사망 처리 예시
    public void Die()
    {
        Enter(ActionState.Dead);
        anim.SetBool("Dead", true);
        rb.linearVelocity = Vector2.zero;
    }

    void OnDrawGizmosSelected()
    {
        // Ground ray
        Gizmos.color = Color.green;
        Vector2 gOrigin = groundCheck ? (Vector2)groundCheck.position : (Vector2)transform.position + Vector2.down * 0.5f;
        Gizmos.DrawLine(gOrigin, gOrigin + Vector2.down * groundDist);

        // Wall box
        Gizmos.color = Color.cyan;
        int dir = Application.isPlaying ? faceDir : 1;
        Vector2 wCenter = wallCheck ? (Vector2)wallCheck.position : (Vector2)transform.position + new Vector2(dir * 0.4f, 0f);
        Gizmos.DrawWireCube(wCenter, wallBox);
    }
}
