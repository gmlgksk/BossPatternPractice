using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using UnityEngine.UI;
// using Unity.VisualScripting.Dependencies.Sqlite;

public class PlayerMove : MonoBehaviour
{
    // 자동으로 불러올 컴포넌트
    Animator anim;
    SpriteRenderer sr;
    PlayerInput pInput;
    Rigidbody2D rb;
    Transform tf;

    [Header("=== 이동 설정 ===")]
    public float moveSpeed = 10f;
    public float jumpForce = 12f;
    public float rollingSpeed = 15f;
    public float inputX;
    public float faceDir;

    [Header("=== 점프 설정 ===")]
    [SerializeField] private bool canJump = true;
    [SerializeField] private float jumpCooldown = 0.1f;
    [SerializeField] private float jumpTime = 0.5f; // 점프 지속 시간

    [Header("=== 경사 감지 ===")]
    [SerializeField] Vector2 groundCheckSize;
    public Transform groundChkPos;
    public Transform frontChkPos;
    public float slopeHitDistance;
    public float forntHitDistance = 1f;
    public float angle;
    public float maxAngle;
    public Vector2 perp;
    public bool isSlope;

    [Header("=== 벽 슬라이딩 ===")]
    [SerializeField] float wallSlideSpeed;
    [SerializeField] LayerMask wallLayer;
    [SerializeField] Transform wallCheckPoint;
    [SerializeField] Vector2 wallCheckSize;
    [SerializeField] private bool isTouchingWall;
    [SerializeField] private bool isWallSlide;

    [Header("=== 벽 점프 ===")]
    [SerializeField] float wallJumpDirection;
    [SerializeField] float wallJumpForce;
    [SerializeField] Vector2 wallJumpAngel;

    [Header("=== 레이어 설정 ===")]
    public LayerMask groundMask;
    public int defaultLayer;
    public int invLayer;

    [Header("=== 물리 감지 ===")]
    public RaycastHit2D hit;
    public RaycastHit2D hit2;
    public RaycastHit2D frontHit;

    [Header("=== 플레이어 상태 ===")]
    [Space(10)]
    [Header("기본 상태")]
    public bool isGround = false;
    public bool isJumping = false;
    public bool wasJumping = false;
    public bool isMoving = false;
    public bool isDead;
    
    [Header("행동 상태")]
    public bool isJumpRequested = false;
    public bool isRolling;
    public bool upStair;
    public bool downStair;
    
    [Header("제한 상태")]
    public bool moveBound;
    public bool attackBound;
    public bool canRoll = true;
    
    [Header("쿨다운")]
    public float rollCooldown = 0.3f;

    [Header("=== 구르기 설정 ===")]
    [SerializeField] private bool isRollAnimationPlaying = false; // 애니메이션 재생 상태
    [SerializeField] private float rollTime = 0.3f;
 
    [Header("점프 상태")]
    [SerializeField] private float currentJumpTime = 0f; // 현재 점프 진행 시간

    // canWallJump 변수 추가
    private bool canWallJump = false;

    // 점프키 재입력 방지용 변수 추가
    private bool jumpKeyWasReleased = true;

    // 벽점프 감속 관련 변수 추가
    private bool isWallJumpDecelerating = false;
    public float wallJumpDecelTimer = 0f;
    public float wallJumpDecelDuration = 0.2f; // 감속 지속 시간(초)
    public float wallJumpDecelFactor = 0.85f;  // 매 프레임 곱할 감속 계수
    public float wallJumpDecelStartTime = 0f; // 벽점프 감속시작시간(초)

    private void Awake()
    {
        defaultLayer = gameObject.layer;

        pInput = GetComponent<PlayerInput>();
        rb = GetComponent<Rigidbody2D>();
        tf = GetComponent<Transform>();
        anim = GetComponentInChildren<Animator>();
        sr = GetComponentInChildren<SpriteRenderer>();
        canRoll = true;
        moveBound = false;
        isDead = GetComponent<PlayerDie>().IsDead;

        faceDir = 1;
        wallJumpDirection = -1;
        wallJumpAngel.Normalize();
        isJumpRequested = false;
        canJump = true;
    }

    void flip()
    {
        faceDir *= -1;
        if (faceDir == -1f)
            tf.localScale = new Vector3(-1, 1, 0);
        else
            tf.localScale = new Vector3(1, 1, 0);
    }

    void Update()
    {
        hit = Physics2D.Raycast(groundChkPos.position, Vector2.down, slopeHitDistance, groundMask);
        frontHit = Physics2D.Raycast(frontChkPos.position, Vector2.down, forntHitDistance, groundMask);
        GroundChk();
        if (hit.collider != null) SlopeChk(hit);
        else if (frontHit.collider != null) SlopeChk(frontHit);
               
        // 점프 요청이 있고 지면에 있다면 점프 실행
        if (isJumpRequested && isGround && canJump)
        {
            Jump();
            isJumpRequested = false; // 점프 실행 후 즉시 false로!
        }
        

        AnimationController();

        CheckWall();

        isDead = GetComponent<PlayerDie>().IsDead;
        attackBound = isGround ? false : GetComponent<PlayerAttack>().attackBound;
        


        // 점프 중일 때 시간 추적
        if (isJumping)
        {
            currentJumpTime += Time.deltaTime;
            
            // 점프 시간이 지나면 점프 중단
            if (currentJumpTime >= jumpTime)
            {
                isJumping = false;
                currentJumpTime = 0f;
            }
        }

        // 캐릭터가 보고 있는 방향 업데이트 (스케일 기반)
        UpdateFaceDirection();

        if (attackBound || isRolling)
            rb.constraints &= ~RigidbodyConstraints2D.FreezePositionX;
        else if (inputX == 0 && isSlope)
            rb.constraints |= RigidbodyConstraints2D.FreezePositionX;
        else
            rb.constraints &= ~RigidbodyConstraints2D.FreezePositionX;

        // 점프 상태 업데이트 - 올라갈 때만 점프, 떨어질 때는 fall
        if (isGround)
        {
            // 지면에 닿으면 점프 상태 해제
            if (isJumping)
            {
                JumpStop();
            }
            anim.SetTrigger("ground");
        }
        else
        {
            // 공중에 있을 때
            if (rb.linearVelocityY > 0)
            {
                // 올라갈 때 - 점프 상태
                isJumping = true;
            }
            else if (rb.linearVelocityY == 0)
            {
                // 떨어질 때 - fall 애니메이션, 점프 상태 해제
                isJumping = false;
            }
            else if (rb.linearVelocityY < 0)
            {
                // 떨어질 때 - fall 애니메이션, 점프 상태 해제
                isJumping = false;
                anim.SetTrigger("fall");
            }
            // linearVelocityY가 0이면 아무것도 하지 않음 (점프도 fall도 아님)
        }

        if (isRolling)
        {
            // 구르기 중에는 이동 상태만 비활성화하고 inputX는 유지
            isMoving = false;
            // 구르기 이동 실행
            Roll();
        }
        else
        {
            // 구르기가 끝나면 이동 상태 복원
            if (Keyboard.current.aKey.isPressed || Keyboard.current.dKey.isPressed)
            {
                isMoving = true;
            }
            Move();
        }
    }

    private void UpdateFaceDirection()
    {
        // 이동 중이거나 구르기 중이 아닐 때만 캐릭터 스케일을 기반으로 faceDir 업데이트
        if (!isMoving && !isRolling)
        {
            if (tf.localScale.x > 0)
                faceDir = 1f;
            else if (tf.localScale.x < 0)
                faceDir = -1f;
        }
    }

    public void AnimationController()
    {
        anim.SetBool("running", isMoving && !isRolling);
        anim.SetBool("rolling", isRolling);
        anim.SetBool("jumping", isJumping);
    }

    public void SlopeChk(RaycastHit2D hit)
    {
        // Vector2.Perpendicular( Vector2 A ) : A값에서 반시계방향으로 90도 회전한 벸터값을 반환.
        perp = Vector2.Perpendicular(hit.normal) * -1f;
        angle = Vector2.Angle(hit.normal, Vector2.up);

        Debug.DrawLine(hit.point, hit.point + hit.normal, Color.blue);
        Debug.DrawLine(hit.point, hit.point + perp, Color.blue);

        if (angle != 0) isSlope = true;
        else isSlope = false;
    }

    public void GroundChk()
    {
        isGround = Physics2D.OverlapBox(groundChkPos.position, groundCheckSize, 0, groundMask);
    }

    void CheckWall()
    {
        isTouchingWall = Physics2D.OverlapBox(wallCheckPoint.position, wallCheckSize, 0, wallLayer);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawCube(groundChkPos.position, groundCheckSize);

        Gizmos.color = Color.green;
        Gizmos.DrawLine(frontHit.point, frontHit.point + Vector2.down * forntHitDistance);

        Gizmos.color = Color.red;
        Gizmos.DrawCube(wallCheckPoint.position, wallCheckSize);

    }

    private void FixedUpdate()
    {
        if (isWallSlide && isJumpRequested)
        {
            canWallJump = true;
        }
        if (canWallJump)
        {
            Debug.Log("벽점프");
            flip();
            wallJump();
            canWallJump = false;
            isJumpRequested = false; // 벽점프 실행 후 즉시 false로!
            // 감속 함수 Invoke로 호출 (타이밍 변수 사용)
            Invoke(nameof(WallJumpDecelerate), wallJumpDecelStartTime);
        }

        WallSlide();
        if (isWallJumpDecelerating)
        {
            rb.linearVelocity = rb.linearVelocity * wallJumpDecelFactor; // X, Y 모두 감속
            wallJumpDecelTimer -= Time.fixedDeltaTime;
            if (wallJumpDecelTimer <= 0f)
            {
                isWallJumpDecelerating = false;
            }
        }

        // if (isGround && wasJumping)
        // {
        //     JumpStop();
        //     Debug.Log("점프 후 착지함!");
        // }

        // if (!isGround && !isJumping)
        // {
        //     wasJumping = false;
        // }
    }

    void WallSlide() {
        if (isTouchingWall && !isGround && rb.linearVelocityY < 0)
        {
            isWallSlide = true;
            wallJumpDirection = -faceDir; // 벽에 닿을 때 직전 facedir의 반대 방향 저장
        }
        else
            isWallSlide = false;

        if (isWallSlide)
            rb.linearVelocityY = wallSlideSpeed;
    }

    void wallJump()
    {
        if ((isTouchingWall || !isGround) )
        {
            rb.linearVelocity = Vector2.zero; // 벽점프 전 속도 초기화
            rb.AddForce(new Vector2(wallJumpForce * wallJumpDirection, wallJumpForce), ForceMode2D.Impulse);
        }
    }

    void Move()
    {
        if (inputX == 0) return;

        // 벽에 닿았을 때 해당 방향으로의 움직임 제한
        if (isTouchingWall)
        {
            Debug.Log($"벽에 막힘!");
            return;
        }

        // 이동1 : 경사,평지,공중 구분
        if (isSlope && isGround && !isJumping && angle < maxAngle)
        {
            rb.linearVelocity = Vector2.zero;
            transform.Translate(perp * moveSpeed * Time.deltaTime * inputX);
        }
        else if (!isSlope && isGround && !isJumping  && angle < maxAngle)
        {
            rb.linearVelocity = Vector2.zero;
            transform.Translate(Vector2.right * moveSpeed * Time.deltaTime * inputX);
        }
        else if (!isGround)
            transform.Translate(Vector2.right * moveSpeed  * 2 / 3 * Time.deltaTime * inputX);

        // 이동2 : 바닥, 공중 구분
        // if (isGround && !isJumping && angle < maxAngle)
        //     transform.Translate(Vector2.right * moveSpeed * Time.deltaTime * Mathf.Abs(inputX));
        // else if (!isGround)
        //     transform.Translate(Vector2.right * moveSpeed*2/3 * Time.deltaTime * Mathf.Abs(inputX));
    }

    public void MoveStop()
    {
        isMoving = false;
        inputX = 0;

        if (isSlope && moveBound)
        {
            rb.linearVelocityY = 0;
            moveBound = false;
        }
        
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            Vector2 input = context.ReadValue<Vector2>();
            
            // X축 입력이 있을 때만 inputX 업데이트
            if (Mathf.Abs(input.x) > 0.1f)
            {
                inputX = input.x;
                isMoving = true;
                moveBound = true;
                
                // X축 입력이 있을 때만 방향 전환
                if ((inputX > 0.8 || inputX < -0.8) && faceDir != inputX) 
                {
                    flip();
                }
            }
            // Y축 입력만 있을 때는 아무것도 하지 않음
        }
        if (context.canceled)
        {
            Debug.Log("move canceled");
            MoveStop();
        }
    }

    void Jump()
    {
        if (isGround && canJump)
        {
            // 구르기 중이면 구르기 종료
            if (isRolling)
            {
                RollEnd();
            }
            
            isJumping = true;
            currentJumpTime = 0f; // 점프 시간 초기화
            rb.linearVelocityY = 0f;
            rb.AddForceY(jumpForce, ForceMode2D.Impulse);
            
            // 점프 쿨다운 시작
            canJump = false;
            Invoke(nameof(EnableJump), jumpCooldown);
            
            Debug.Log("점프 실행!");
        }
    }

    public void JumpStop()
    {
        isJumping = false;
        isJumpRequested = false;
        currentJumpTime = 0f; // 점프 시간 초기화
    }

    private void EnableJump()
    {
        canJump = true;
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        Debug.Log($"OnJump: obj={name}, id={GetInstanceID()}, map={GetComponent<PlayerInput>()?.currentActionMap?.name}, " +
              $"started={context.started}, performed={context.performed}, canceled={context.canceled}");
        if (isDead) return;
        Debug.Log("점프함수 인식");
        if (context.started || context.performed)
        {
            Debug.Log("점프키 수행");
            if (jumpKeyWasReleased)
            {
                Debug.Log("키 릴리즈 참");
                isJumpRequested = true;
                jumpKeyWasReleased = false;
            }
        }
        
        if (context.canceled)
        {
            Debug.Log("점프키 취소");

            // isJumpRequested = false;
            jumpKeyWasReleased = true;
        }
    }

    public void OnRoll(InputAction.CallbackContext context)
    {
        if (!canRoll || isRolling || !context.started || !isGround)
            return;
        
        StartRoll();
    }

    private void StartRoll()
    {
        isRolling = true;
        isRollAnimationPlaying = true;
        canRoll = false;

        // 애니메이션 트리거 설정
        anim.SetBool("rolling", true);

        Debug.Log($"구르기 시작! 현재 방향: {faceDir}");
        Invoke(nameof(RollEnd), rollTime);
    }

    private void Roll()
    {
        // 벽에 닿았을 때 구르기 제한
        if (isTouchingWall)
        {
            Debug.Log($"구르기 벽에 막힘! 방향: {faceDir}");
            return;
        }
        

        if (!isJumping && angle < maxAngle)
        {
            rb.linearVelocity = Vector2.zero;
            transform.Translate(perp * rollingSpeed * Time.deltaTime * faceDir);
        }
        

        gameObject.layer = invLayer;
    }

    public void RollEnd() // 애니메이션 이벤트에서 호출하는 함수
    {
        anim.SetBool("rolling",false);
        isRolling = false;
        isRollAnimationPlaying = false;
        gameObject.layer = defaultLayer;

        // 구르기 쿨다운 시작
        Invoke(nameof(EnableRoll), rollCooldown);

        Debug.Log("구르기 종료");
    }

    private void EnableRoll()
    {
        if (!canRoll)
        {
            canRoll = true;
            Debug.Log("구르기 가능");
        }
    }

    // 벽점프 감속 함수 캡슐화
    private void WallJumpDecelerate()
    {
        isWallJumpDecelerating = true;
        wallJumpDecelTimer = wallJumpDecelDuration;
    }
}
  