using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttack : MonoBehaviour
{
    [Header("=== 컴포넌트 ===")]
    [SerializeField] private Transform attackRangeTransform;
    [SerializeField] private Collider2D coll;
    [SerializeField] private SpriteRenderer sr;
    [SerializeField] private Transform player;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Animator anim;
    [SerializeField] private Camera mainCam;

    [Header("=== 공격 범위 설정 ===")]
    [SerializeField] private float indicatorDistance = 2f;
    [SerializeField] private Vector2 mouseDir;
    [SerializeField] public float attackForce = 100;

    [Header("=== 공격 타이밍 ===")]
    public float attackCooldown;
    public float attackTime;

    [Header("=== 공격 상태 ===")]
    [SerializeField] private bool canAttack = true;
    public bool attackBound;

    private InputAction attackAction;

    private void Awake()
    {
        if (player == null) player = transform;
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();

        attackRangeTransform = transform.Find("Attack Range");
        if (attackRangeTransform != null)
        {
            coll = attackRangeTransform.GetComponent<Collider2D>();
            sr = attackRangeTransform.GetComponent<SpriteRenderer>();
        }

        coll.enabled = false;
        mainCam = Camera.main;

        // PlayerInput 컴포넌트에서 Attack 액션 참조
        var playerInput = GetComponent<PlayerInput>();
        if (playerInput != null)
        {
            attackAction = playerInput.actions["Attack"];
        }
    }

    private void Update()
    {
        RotateTowardMouse2D();
    }

    private void OnEnable()
    { if (attackAction != null) attackAction.performed += OnAttackPerformed; }
    private void OnDisable()
    { if (attackAction != null) attackAction.performed -= OnAttackPerformed; }

    public void OnAttackPerformed(InputAction.CallbackContext ctx)
    {
        TryAttack();
    }

    private void TryAttack()
    {
        if (!canAttack) return;
        EnableAttackRange();
        anim.SetTrigger("attack");
        rb.linearVelocityY = 0f;
        rb.AddForce(mouseDir * attackForce, ForceMode2D.Impulse);
        Invoke(nameof(DisableAttackRange), attackTime);
        Invoke(nameof(ResetAttack), attackCooldown);
    }

    private void EnableAttackRange()
    {
        attackBound = true;
        canAttack = false;
        coll.enabled = true;
        SetAlpha(1f);
        Invoke(nameof(attackBoundOff), attackCooldown);
    }

    private void DisableAttackRange()
    {
        coll.enabled = false;
        SetAlpha(0.08f);
    }

    void attackBoundOff()
    {
        attackBound = false;
    }

    private void ResetAttack()
    {
        canAttack = true;
    }

    private void SetAlpha(float alpha)
    {
        if (sr != null)
        {
            Color c = sr.color;
            c.a = alpha;
            sr.color = c;
        }
    }
    private void RotateTowardMouse2D()
    {
        if (mainCam == null)
            mainCam = Camera.main;

        Vector3 mouseScreenPos = Mouse.current.position.ReadValue();
        mouseScreenPos.z = Mathf.Abs(mainCam.transform.position.z);
        Vector3 mouseWorldPos = mainCam.ScreenToWorldPoint(mouseScreenPos);
        mouseWorldPos.z = 0f;

        mouseDir = (mouseWorldPos - player.position).normalized;
        float angle = Mathf.Atan2(mouseDir.y, mouseDir.x) * Mathf.Rad2Deg;

        if (attackRangeTransform != null)
        {
            attackRangeTransform.rotation = Quaternion.Euler(0f, 0f, angle);
            attackRangeTransform.position = player.position + (Vector3)(mouseDir * indicatorDistance);
        }

        Debug.DrawLine(player.position, mouseWorldPos, Color.red);
    }
}
