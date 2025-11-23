using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D), typeof(PlayerInput))]
public class PlayerMove2 : MonoBehaviour
{
    // ==== Components ====
    Rigidbody2D rb;
    Animator anim;
    SpriteRenderer sr;
    Transform tf;
    PlayerInput pInput;

    // ==== Layers ====
    [Header("Layers")]
    public LayerMask groundMask;
    public LayerMask wallMask;
    public int defaultLayer;
    public int invLayer;

    // ==== Locomotion & Action States ====
    public enum LocoState { Grounded, Airborne, WallSlide }
    public enum ActionState { None, Rolling }

    public LocoState locoState { get; private set; } = LocoState.Airborne;
    public ActionState actionState { get; private set; } = ActionState.None;

    // ==== Settings ====
    [Header("Move")]
    public float moveSpeed = 10f;
    public float airMoveSpeed = 6.5f;        // 공중 제어력
    public float maxRunSpeed = 12f;
    public float accel = 60f;
    public float airAccel = 35f;
    public float friction = 20f;             // 지면 마찰(감속)

    [Header("Jump")]
    public float jumpForce = 12f;
    public float coyoteTime = 0.12f;         // 코요테
    public float jumpBufferTime = 0.12f;     // 버퍼
    public float jumpCutMultiplier = 0.5f;   // 점프키 떼면 하강 가속
    public float maxFallSpeed = 35f;

    [Header("Wall")]
    public Vector2 wallCheckSize = new Vector2(0.3f, 1.0f);
    public Transform wallCheckPoint;
    public float wallSlideMaxFall = -3.5f;   // 벽슬라이드 최대 하강속도
    public Vector2 wallJumpImpulse = new(11f, 12f);
    public float wallCheckDistance = 0.1f;

    [Header("Ground & Slope")]
    public Transform groundChkPos;
    public Vector2 groundCheckSize = new(0.9f, 0.1f);
    public float maxSlopeAngle = 45f;        // 이동 허용 경사

    [Header("Roll")]
    public float rollSpeed = 16f;
    public float rollDuration = 0.30f;
    public float rollCooldown = 0.30f;

    // ==== Runtime ====
    float inputX;                 // -1..1
    int faceDir = 1;              // 시선 방향
    bool jumpHeld;                // 점프키 유지
    bool jumpPressed;             // 프레임 버퍼용

    float coyoteTimer;
    float jumpBufferTimer;
    bool canRoll = true;
    float rollTimer;

    // 경사 관련
    RaycastHit2D groundHit;
    Vector2 groundNormal = Vector2.up;
    bool onSlope;

    // 캐시
    readonly Vector2 right = Vector2.right;
    readonly Vector2 up = Vector2.up;

    // ==== Unity ====
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        pInput = GetComponent<PlayerInput>();
        tf = transform;
        anim = GetComponentInChildren<Animator>();
        sr = GetComponentInChildren<SpriteRenderer>();
        defaultLayer = gameObject.layer;
    }

    void Update()
    {
        // --- Timers ---
        if (IsGrounded())
            coyoteTimer = coyoteTime;
        else
            coyoteTimer -= Time.deltaTime;

        if (jumpPressed) jumpBufferTimer = jumpBufferTime;
        else jumpBufferTimer -= Time.deltaTime;

        // --- State Resolve (Locomotion) ---
        ResolveGroundAndSlope();
        ResolveLocomotionState();

        // --- Jump: 버퍼+코요테 ---
        if (jumpBufferTimer > 0f)
        {
            if (locoState == LocoState.Grounded && CanStartJump())
                DoGroundJump();
            else if (locoState == LocoState.WallSlide && CanStartJump())
                DoWallJump();
        }

        // --- Jump cut: 키에서 손 떼면 상승 억제 ---
        if (!jumpHeld && rb.linearVelocity.y > 0f)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);

        // --- Action Layer (Roll) ---
        if (actionState == ActionState.Rolling)
        {
            rollTimer -= Time.deltaTime;
            if (rollTimer <= 0f) EndRoll();
        }

        // --- Facing ---
        if (Mathf.Abs(inputX) > 0.01f)
        {
            faceDir = inputX > 0 ? 1 : -1;
            tf.localScale = new Vector3(faceDir, 1, 1);
        }

        // --- Animator ---
        anim.SetBool("running", locoState == LocoState.Grounded && Mathf.Abs(rb.linearVelocity.x) > 0.1f && actionState != ActionState.Rolling);
        anim.SetBool("rolling", actionState == ActionState.Rolling);
        anim.SetBool("jumping", locoState == LocoState.Airborne);
        anim.SetFloat("yVelocity",rb.linearVelocityY);

        // 입력 버퍼 플래그 소비
        jumpPressed = false;
    }

    void FixedUpdate()
    {
        // 이동 벡터 계산
        var targetSpeed = (locoState == LocoState.Grounded ? moveSpeed : airMoveSpeed) * inputX;
        var curX = rb.linearVelocity.x;

        float usedAccel = (locoState == LocoState.Grounded ? accel : airAccel);
        float newX = Mathf.MoveTowards(curX, targetSpeed, usedAccel * Time.fixedDeltaTime);

        // 경사면 투영: 지면일 때만
        Vector2 vel = rb.linearVelocity;
        if (locoState == LocoState.Grounded && onSlope)
        {
            // 입력 방향을 경사 접선(perp)으로 투영
            Vector2 tangent = Vector2.Perpendicular(groundNormal) * Mathf.Sign(inputX);
            var tangentialSpeed = (locoState == LocoState.Grounded ? moveSpeed : airMoveSpeed) * Mathf.Abs(inputX);
            Vector2 slopeVel = tangent.normalized * tangentialSpeed;
            vel.x = slopeVel.x;
            vel.y = Mathf.Min(vel.y, slopeVel.y); // 오르막에서 약간 보정
        }
        else
        {
            vel.x = Mathf.Clamp(newX, -maxRunSpeed, maxRunSpeed);
        }

        // Action: Roll은 수평 속도 우선권
        if (actionState == ActionState.Rolling)
            vel.x = faceDir * rollSpeed;

        // 중력/낙하 제한
        vel.y = Mathf.Max(vel.y, -maxFallSpeed);

        // 벽슬라이드 감속
        if (locoState == LocoState.WallSlide && vel.y < wallSlideMaxFall)
            vel.y = wallSlideMaxFall;

        rb.linearVelocity = vel;

        // 지면 마찰(입력없고, 지면, 구르기아님)
        if (locoState == LocoState.Grounded && Mathf.Abs(inputX) < 0.01f && actionState != ActionState.Rolling)
        {
            float f = Mathf.Min(Mathf.Abs(rb.linearVelocity.x), friction * Time.fixedDeltaTime);
            rb.linearVelocity = new Vector2(rb.linearVelocity.x - Mathf.Sign(rb.linearVelocity.x) * f, rb.linearVelocity.y);
        }
    }

    // ==== Input System ====
    public void OnMove(InputAction.CallbackContext ctx)
    {
        if (ctx.performed || ctx.canceled)
        {
            var v = ctx.ReadValue<Vector2>();
            inputX = Mathf.Abs(v.x) > 0.1f ? Mathf.Sign(v.x) : 0f;
        }
    }

    public void OnJump(InputAction.CallbackContext ctx)
    {
        if (ctx.started)
        {
            jumpPressed = true; // 버퍼에 저장
            jumpHeld = true;
        }
        else if (ctx.canceled)
        {
            jumpHeld = false;
        }
    }

    public void OnRoll(InputAction.CallbackContext ctx)
    {
        if (!ctx.started) return;
        if (actionState != ActionState.None || !canRoll) return;
        if (locoState != LocoState.Grounded) return;

        StartRoll();
    }

    // ==== Locomotion helpers ====
    bool IsGrounded()
    {
        var hit = Physics2D.OverlapBox(groundChkPos.position, groundCheckSize, 0f, groundMask);
        return hit != null;
    }

    void ResolveGroundAndSlope()
    {
        groundHit = Physics2D.BoxCast(groundChkPos.position, groundCheckSize, 0f, Vector2.down, 0.05f, groundMask);
        if (groundHit.collider)
        {
            groundNormal = groundHit.normal;
            float angle = Vector2.Angle(groundNormal, Vector2.up);
            onSlope = angle > 0.01f && angle <= maxSlopeAngle;
        }
        else
        {
            groundNormal = Vector2.up;
            onSlope = false;
        }
    }

    void ResolveLocomotionState()
    {
        if (IsGrounded())
        {
            locoState = LocoState.Grounded;
            return;
        }

        // 벽슬라이드: 공중 + 수평 접촉 + 하강중
        bool touchingWall = Physics2D.OverlapBox(wallCheckPoint.position, wallCheckSize, 0f, wallMask);
        if (touchingWall && rb.linearVelocity.y < -0.01f && Mathf.Abs(inputX) > 0.1f)
        {
            locoState = LocoState.WallSlide;
        }
        else
        {
            locoState = LocoState.Airborne;
        }
    }

    bool CanStartJump()
    {
        // 코요테 또는 벽슬라이드 중
        return (locoState == LocoState.Grounded && coyoteTimer > 0f)
            || (locoState == LocoState.WallSlide);
    }

    void DoGroundJump()
    {
        jumpBufferTimer = 0f;
        coyoteTimer = 0f;
        var v = rb.linearVelocity;
        v.y = 0f;
        rb.linearVelocity = v;
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
    }

    void DoWallJump()
    {
        jumpBufferTimer = 0f;
        // 벽 방향 찾기: 캐릭터 좌우에 짧은 박스 검사
        int wallDir = CheckWallSide(); // -1 = 왼벽, +1 = 오른벽, 0 = 없음
        if (wallDir == 0) wallDir = -faceDir; // 안전장치

        // 반대 방향으로 튕김
        Vector2 impulse = new Vector2(-wallDir * wallJumpImpulse.x, wallJumpImpulse.y);
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(impulse, ForceMode2D.Impulse);

        // 벽에서 튀어나오니 바라보는 방향 갱신
        faceDir = -wallDir;
        tf.localScale = new Vector3(faceDir, 1, 1);
    }

    int CheckWallSide()
    {
        // 오른쪽
        bool rightWall = Physics2D.OverlapBox(wallCheckPoint.position + (Vector3)(right * wallCheckDistance),
                                              wallCheckSize, 0f, wallMask);
        bool leftWall = Physics2D.OverlapBox(wallCheckPoint.position - (Vector3)(right * wallCheckDistance),
                                             wallCheckSize, 0f, wallMask);
        if (rightWall && !leftWall) return +1;
        if (leftWall && !rightWall) return -1;
        return 0;
    }

    // ==== Roll ====
    void StartRoll()
    {
        actionState = ActionState.Rolling;
        rollTimer = rollDuration;
        canRoll = false;
        gameObject.layer = invLayer; // 무적 레이어
        anim.SetBool("rolling", true);
        Invoke(nameof(EndRoll), rollDuration);
        Invoke(nameof(EnableRoll), rollCooldown + rollDuration);
    }

    void EndRoll()
    {
        if (actionState != ActionState.Rolling) return;
        actionState = ActionState.None;
        anim.SetBool("rolling", false);
        gameObject.layer = defaultLayer;
    }

    void EnableRoll() => canRoll = true;

    // ==== Gizmos ====
    void OnDrawGizmosSelected()
    {
        if (groundChkPos)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(groundChkPos.position, groundCheckSize);
        }
        if (wallCheckPoint)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(wallCheckPoint.position, wallCheckSize);
        }
    }
}
