using System.Runtime.InteropServices;
using UnityEngine;

public class Entity : MonoBehaviour
{
    protected Rigidbody2D rb;
    protected Collider2D col;
    protected Animator anim;
    protected HP_System hp;


    [Header("Move Option")]
    [SerializeField] protected float moveSpeed;

    [Header("Chracter Option")]
    [SerializeField] protected int faceDir = 1;
    
    [Header("Attack Option")]
    [SerializeField] protected Transform sightPoint;
    [SerializeField] protected float attackRadius;
    [SerializeField] protected LayerMask whatIsTarget;

    protected virtual void Awake()
    {
        faceDir = 1;
        rb      = GetComponent<Rigidbody2D>();
        col     = GetComponent<Collider2D>();
        anim    = GetComponent<Animator>();
        hp      = GetComponent<HP_System>();
    }
    protected virtual void Update()
    {
        HandleAnimation();
        Handle_Flip();
    }

    protected void MoveTo(float destinationX,float speed)
    {
        float deltaX = destinationX - rb.position.x;
        float dir = Mathf.Sign(deltaX);
        if (Mathf.Abs(deltaX) < 0.05f) dir = 0f;

        rb.linearVelocityX = dir * speed;
    }
    public void HandleAnimation()
    {
        // anim.SetFloat("velocityX",rb.linearVelocityX); // idle/move
        // anim.SetFloat("velocityY",rb.linearVelocityY); // jump/fall
    }
    public void Attack_Start()
    {
        // anim.SetTrigger("attack");
    }
    public void Attack_Perform()
    {
        // Collider2D[] enemyColliders = Physics2D.OverlapCircleAll(sightPoint.position, attackRadius, whatIsTarget);
        
        // foreach (Collider2D enemy in enemyColliders)
        // {
        //     HP entityTarget = enemy.GetComponent<HP>();
        //     entityTarget.TakeDamage();
        // }
    }
    public void TakeDamage()
    {
        hp.Health_Reduce();
    }
    protected virtual void Handle_Flip()
    {
        if (faceDir * rb.linearVelocityX < 0)
            Flip();
    }
    protected virtual void Flip()
    {
        faceDir *= -1;
        transform.Rotate(0, 180, 0);
    }
}
