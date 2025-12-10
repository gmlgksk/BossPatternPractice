using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    Rigidbody2D rigid;
    Animator anim;

    public Transform groundChkFront; // 바닥 체크 position
    public Transform groundChkBack;  // 바닥 체크 position

    public Transform wallChk;
    public float wallchkDistance;
    public LayerMask w_Layer;

    public bool isWall;
    public float slidingSpeed;
    public float wallJumpPower;
    public bool isWallJump;

    public float runSpeed; // 이동 속도
    public float isRight = 1; // 바라보는 방향 1 = 오른쪽, -1 = 왼쪽

    public float input_x;
    public bool isGround;
    public float chkDistance;
    public float jumpPower = 1;
    public LayerMask g_Layer;

    void Start()
    {
        rigid = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        input_x = Input.GetAxis("Horizontal");

        // 캐릭터의 앞쪽과 뒤쪽의 바닥 체크를 진행
        bool ground_front = Physics2D.Raycast(groundChkFront.position, Vector2.down, chkDistance, g_Layer);
        bool ground_back = Physics2D.Raycast(groundChkBack.position, Vector2.down, chkDistance, g_Layer);

        // 점프 상태에서 앞 또는 뒤쪽 바닥이 감지되면 바닥에 붙어서 이동하게 변경
        if (isGround && (ground_front || ground_back))
            rigid.linearVelocityY = 0f;

        // 앞 또는 뒤쪽의 바닥이 감지되면 isGround 변수를 참으로!
        isGround = ground_front || ground_back;

        anim.SetBool("isGround", isGround);

        isWall = Physics2D.Raycast(wallChk.position, Vector2.right * isRight, wallchkDistance, w_Layer);
        anim.SetBool("isSliding", isWall);

        // 스페이스바가 눌리면 점프 애니메이션을 동작
        if (Input.GetAxis("Jump") != 0)
        {
            anim.SetTrigger("jump");
        }

        // 방향키가 눌리는 방향과 캐릭터가 바라보는 방향이 다르면 캐릭터의 방향을 전환.
        if (!isWallJump)
        {
            if ((input_x > 0 && isRight < 0) || (input_x < 0 && isRight > 0))
            {
                FlipPlayer();
                anim.SetBool("run", true);
            }
            else if (input_x == 0)
            {
                anim.SetBool("run", false);
            }
        }
    }

    private void FixedUpdate()
    {

        // 캐릭터 이동
        if (isRight != input_x&& input_x!=0)
            FlipPlayer();
        
        if (!isWallJump)
                rigid.linearVelocityX = input_x * runSpeed;

        if (isGround)
        {
            // 캐릭터 점프
            if (Input.GetAxis("Jump") != 0)
            {
                rigid.linearVelocityY = jumpPower;
            }
        }

        if (isWall)
        {
            isWallJump = false;
            rigid.linearVelocityY *= slidingSpeed;

            if (Input.GetAxis("Jump") != 0)
            {
                isWallJump = true;
                Invoke("FreezeX", 0.3f);
                rigid.linearVelocity = new Vector2(-isRight * wallJumpPower, 0.9f * wallJumpPower);
                FlipPlayer();
            }
        }
    }

    void FreezeX()
    {
        isWallJump = false;
    }

    void FlipPlayer()
    {
        // 방향을 전환.
        transform.eulerAngles = new Vector3(0, Mathf.Abs(transform.eulerAngles.y - 180), 0);
        isRight = isRight * -1;
    }

    // 바닥 체크 Ray를 씬화면에 표시
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawRay(groundChkFront.position, Vector2.down * chkDistance);
        Gizmos.DrawRay(groundChkBack.position, Vector2.down * chkDistance);

        Gizmos.color = Color.blue;
        Gizmos.DrawRay(wallChk.position, Vector2.right * isRight * wallchkDistance);
    }
}
