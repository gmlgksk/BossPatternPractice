using System;
using TMPro;
using UnityEngine;

public class Enemy : Entity
{
    private enum EnemyState {Dead, Patrol, Warning, Chase, Attack}
    [Header("===== Enemy Details! =====")]

    [Header("[ Indecator ]")]
    public String           totalState;
    public String           entityState;
    public String           destinationLog;


    [Header("[ Layer ]")]
    public LayerMask        platformLayer;      // 플랫폼 레이어(한 개)
    public LayerMask        platformIgnoreLayer;

    [Header("[ Floors ]")]
    public int              Floor_Number;
    public float[]          floorY;
    public Transform[]      platforms;

    [Header("[ Sight Details ]")]
    private EnemyState currentState;
    public bool             lookedTarget;
    public float            lookingTimer = 0f;
    public float            chasingTimer = 0f;
    public float            identityTime = 1f;
    public float            chaseEndTime = 1f;
    public LayerMask        whatIsTarget;
    public LayerMask        whatIsBlock;
    public Transform        sightPoint;

    [Header("[ Patrol ]")]
    [SerializeField] private Transform[]    patrolPoints;
    [SerializeField] private int            patrolIndex;
    [SerializeField] private Vector2        sightDirection;
    [SerializeField] private float          sightDistance;
    [SerializeField] private float          spreadAngle;

    [Header("[ Attack ]")]
    public GameObject       ATK_Range;
    public Transform        attackPoint;
    public float            attackRadius;
    // private Collider2D ATK_Col;

    [Header("[ Chase ]")]
    public float            chaseSpeed = 6f;
    public float            chaseEndRadius;
    public Transform        target;

    [Header("[ Stairs ]")]
    // stairs[floor][i] : floor층 계단 포인트들
    public Transform[][]    stairs = new Transform[3][];
    public Transform[]      stairs_0;
    public Transform[]      stairs_1;
    public Transform[]      stairs_2; 
    private int             currentFloor;   // 0,1,2
    private int             targetFloor;    // 0,1,2
    private int             goToUpDown;
    
    public TMP_Text         stateText; 










    protected override void Awake()
    {
        base.Awake();
        
        stairs[0] = stairs_0;
        stairs[1] = stairs_1; 
        stairs[2] = stairs_2;

        // floorY = new float[Floor_Number];
        currentFloor = FloorCheck(transform);

        // ATK_Range = GetComponentInChildren<GameObject>();
        // if (ATK_Range!=null)
        // ATK_Col=ATK_Range.GetComponent<Collider2D>();

        stateText   = GetComponentInChildren<TMP_Text>();
        anim        = GetComponentInChildren<Animator>();

        currentState = EnemyState.Patrol;
    }
    protected override void Update()
    {
        base.Update();
        Handle_State();
        Handle_Information();
        if (isDie) {currentState = EnemyState.Dead;return;}
        
        anim.SetFloat("velocityX",rb.linearVelocityX);

        HandleSight();
        Handle_MovementByState(currentState);
    }

    public void Handle_Information()
    {
        totalState = $"curFloor:{currentFloor},targetFloor:{targetFloor},updown{goToUpDown}";
        stateText.text = entityState;
        if(target)
            targetFloor = FloorCheck(target);
    }







    // ========== 핵심함수들 ==========
    public void Handle_State()
    {
        // [순찰] -> [경계] -> [추적]&[공격] -> [경계] -> [순찰]
        // 공격범위에 들어오면 무조건 [공격]
        if (!isDie && OnAttackRange() && target) currentState = EnemyState.Attack;
        switch (currentState)
        {
            case EnemyState.Dead:
                entityState = "i'm Dead. Sad:(";
                break;
            // ===== 순찰 상태
            case EnemyState.Patrol:
                entityState = "patrol";
                if (lookedTarget)
                {
                    currentState = EnemyState.Warning;
                    break;
                }
                break;
            // ===== 경계 상태
            case EnemyState.Warning:
                entityState = "warning";
                chasingTimer = 0;
                if (lookedTarget)
                    lookingTimer += Time.deltaTime;
                else
                    lookingTimer -= Time.deltaTime;
                
                // 경계 애니메이션, 움직임 등

                if (lookingTimer > identityTime)
                    currentState = EnemyState.Chase;
                else if (lookingTimer < 0)
                    currentState = EnemyState.Patrol;

                break;
            // ===== 추적 상태
            case EnemyState.Chase:
                entityState = "chase";
                lookingTimer = 0f;
                chasingTimer += Time.deltaTime;

                if (lookedTarget) chasingTimer=0;
                if (chasingTimer > chaseEndTime)
                    currentState = EnemyState.Warning;
                break;
            // ===== 공격상태 /상태해제는 애니메이터에서
            case EnemyState.Attack:
                entityState = "attack";
                break;

            default:
                break;
        }   
    }
    private void Handle_MovementByState(EnemyState state)
    {
        if      (state == EnemyState.Dead)       Dead();
        else if (state == EnemyState.Patrol)     Patrol();
        else if (state == EnemyState.Warning)    Warning();
        else if (state == EnemyState.Chase)      Chase();
        else if (state == EnemyState.Attack)     Attack();
    }
    public void Dead()
    {
        
    }
    public void Patrol()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
            return;

        // 현재 목표 지점
        Vector2 targetPos = patrolPoints[patrolIndex].position;
        Debug.Log($"목표 x값 : {targetPos.x}");
        MoveTo(targetPos.x,moveSpeed);

        // 도착했으면 다음 포인트로 인덱스 변경
        if (Vector2.Distance(transform.position, targetPos) < 0.1f)
        {
            patrolIndex++;
            if (patrolIndex >= patrolPoints.Length) patrolIndex = 0; // 다시 처음으로
            Debug.Log($"[Patrol] 도착! length={patrolPoints.Length}");
        }
    }
    public void Warning()
    {
        Debug.Log("의심중");
    }
    public void Chase()
    {
        Vector2 destination = transform.position;

        if (currentFloor == targetFloor)
        {
            goToUpDown = 0; ;
            destination.x = target.position.x;
            Debug.Log("chech Same");
            destinationLog = $"target, x:{destination.x}";
        }
        if (currentFloor > targetFloor)
        {
            goToUpDown = -1;
            
            destination.x = FindNearestStair(stairs,currentFloor-1,faceDir).position.x;
            Debug.Log("chech Down");
            destinationLog=$"downPoint, x:{destination.x}";
        }
        if (currentFloor < targetFloor)
        {
            goToUpDown = 1;
            destination.x = FindNearestStair(stairs,currentFloor,faceDir).position.x;
            Debug.Log("chech Up");
            destinationLog=$"upPoint, x:{destination.x}";
        }

        MoveTo(destination.x,chaseSpeed);

        if (Vector2.Distance(transform.position,destination) < 0.1f)
            setCurrentFloor(goToUpDown);
    }
    public void Attack()
    {
        rb.linearVelocity = Vector2.zero;
        anim.SetTrigger("attack");
        Debug.Log("공격!");
    }
    public override void Attack_Perform()
    {
        // 1) 공격 위치가 null이면 바로 리턴 (실수 방지용)
        if (attackPoint == null)
        {
            Debug.LogError("[Enemy] attackPoint 가 설정되지 않았습니다.", this);
            return;
        }

        // 2) 타겟 탐색
        Collider2D targetCollider = Physics2D.OverlapCircle(
            attackPoint.position,
            attackRadius,
            whatIsTarget
        );

        // 3) 맞은 대상이 없으면 종료
        if (targetCollider == null)
            return;

        // 4) HP_System 우선 체크
        HP_System hp = targetCollider.GetComponent<HP_System>();
        if (hp != null)
        {
            hp.Health_Reduce();
            return;
        }

        // 5) 그게 아니면 PlayerController 인지 체크
        PlayerController player = targetCollider.GetComponent<PlayerController>();
        if (player != null)
        {
            player.Die();
            return;
        }

        // 6) 둘 다 아니면 로그만 찍어보기 (선택)
        Debug.Log($"[Enemy] 공격했지만 처리 대상 없는 콜라이더: {targetCollider.name}", targetCollider);
    }

    public override void Attack_End()
    {
        currentState = EnemyState.Chase;
    }

    public override void Die_End()
    {
        gameObject.SetActive(false);
    }






    // ========== 보조함수들 ==========
    public bool OnAttackRange()
    {
        Collider2D target = Physics2D.OverlapCircle(attackPoint.position,chaseEndRadius,whatIsTarget);

        return target != null
            && target.TryGetComponent<PlayerController>(out var player)
            && player.Current != PlayerController.ActionState.Dead;
    }

    private void HandleSight()
    {
        Vector2 baseDir = sightDirection.normalized;

        // 3개 방향 계산
        Vector2 dirCenter = baseDir;
        Vector2 dirUp = RotateVector(baseDir, spreadAngle);   // 기준 +각도
        Vector2 dirDown = RotateVector(baseDir, -spreadAngle);   // 기준 -각도

        // 3개의 결과를 각각 받고
        bool hitCenter = SightRaycast(dirCenter, "Center");
        bool hitUp     = SightRaycast(dirUp,     "Up");
        bool hitDown   = SightRaycast(dirDown,   "Down");

        // 하나라도 true면 lookedTarget = true
        lookedTarget = hitCenter || hitUp || hitDown;

        // // 🔹 "놓친 시점" 체크 (원하면 사용)
        // if (!lookedTarget && lastLookedTarget)
        // {
        //     Debug.Log("👀 타겟을 이제 막 놓친 순간!");
        //     // 여기서 '놓쳤을 때' 로직 처리 (탐색 모션, 애니메이션, 상태 변경 등)
        // }

        // // 다음 프레임 대비해서 저장
        // lastLookedTarget = lookedTarget;
    }

    private bool SightRaycast(Vector2 dir, string debugTag = "")
    {
        int mask = whatIsTarget | whatIsBlock;

        RaycastHit2D hit = Physics2D.Raycast(
            sightPoint.position,
            dir * faceDir,
            sightDistance,
            mask
        );

        if (hit.collider == null)
        {
            Debug.Log($"[{debugTag}] 아무것도 안 맞음");
            return false;
        }
        if (hit.collider.TryGetComponent<PlayerController>(out var player)
            && player.Current == PlayerController.ActionState.Dead)
            return false;

        int hitLayer = hit.collider.gameObject.layer;
        int hitBit   = 1 << hitLayer;

        bool isBlock  = (whatIsBlock  & hitBit) != 0;
        bool isTarget = (whatIsTarget & hitBit) != 0;

        Debug.Log($"[{debugTag}] 맞은 것: {hit.collider.name}, 레이어={LayerMask.LayerToName(hitLayer)}");

        // 1️⃣ 벽에 맞았으면 시야 차단
        if (isBlock)
        {
            Debug.Log($"[{debugTag}] 벽에 막힘 (레이 중단)");
            return false;
        }

        // 2️⃣ 플레이어에 맞았으면 타겟 발견
        if (isTarget)
        {
            target = hit.collider.transform;
            lookedTarget = true;
            Debug.Log($"[{debugTag}] 플레이어 발견!");
            return true;
        }

        // 3️⃣ 둘 다 아니면 그냥 무시
        Debug.Log($"[{debugTag}] 타겟/벽이 아닌 다른 레이어 맞음");
        return false;
    }


    private Vector2 RotateVector(Vector2 v, float degrees)
    {
        // 2D 에서는 z축 회전만 쓰니까 이렇게 처리
        return (Vector2)(Quaternion.Euler(0f, 0f, degrees) * v);
    }
    private void OnDrawGizmos()
    {
        if (sightPoint != null)
        {
            Gizmos.color = Color.red;
            Vector2 baseDir = sightDirection.normalized;
            baseDir.x *= faceDir;
            Vector2 dirCenter = baseDir;
            Vector2 dirUp     = RotateVector(baseDir,  spreadAngle);
            Vector2 dirDown   = RotateVector(baseDir, -spreadAngle);
            Gizmos.DrawLine(
                sightPoint.position,
                sightPoint.position + (Vector3)dirCenter * sightDistance
            );
            Gizmos.DrawLine(
                sightPoint.position,
                sightPoint.position + (Vector3)dirUp * sightDistance
            );
            Gizmos.DrawLine(
                sightPoint.position,
                sightPoint.position + (Vector3)dirDown * sightDistance
            );
        }
        if (sightPoint)
        {
            Gizmos.color = Color.orange;
            Gizmos.DrawWireSphere(attackPoint.position,attackRadius);
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(attackPoint.position,chaseEndRadius);
        }

    }

    int FloorCheck(Transform trans)
    {
        if (floorY == null || floorY.Length == 0)
        {
            Debug.LogError("floorY가 설정되지 않았습니다.");
            return 0;
        }

        float posY = trans.position.y;

        // 높은 층부터 내려가며 체크 (마지막 인덱스가 가장 높은 층이라고 가정)
        for (int i = floorY.Length - 1; i >= 0; i--)
        {
            if (posY > floorY[i])
                return i;
        }

        return 0;
    }

    public void AttackRangeCheck()
    {
        
    }

    
    void setCurrentFloor(int goToUpDown)
	{
		if (goToUpDown == 0)
			return;
		if (goToUpDown == 1)
		{
			currentFloor +=1;
			goToUpDown = 0;
		}
		if (goToUpDown == -1)
		{
			currentFloor -=1;
			goToUpDown = 0;
		}
        
        PlatformIgnore(currentFloor);
	}
    // ====== 계단 포인트 찾기 ======
    Transform FindNearestStair(Transform[][] points, int floor,int direction)
    {
        Debug.Log("");
        if (points == null) {
            Debug.Log("noStairs");
            return null;
        }
        if (floor < 0 || floor >= points.Length) {
            Debug.Log("noFloor");
            return null;
        }
        Transform[] list = points[floor];
        if (list == null || list.Length == 0) return null;

        Transform best      = null;
        float bestSqr       = float.MaxValue;
        Transform semiBest  = null;
        float semiBestSqr   = float.MaxValue;

        foreach (var t in list)
        {
            if (!t) continue;

            float dx  = t.position.x - rb.position.x;
            // dx가 양수면 우측, 바라보고 있는 방향 우선
            
            
            float sqr = dx * dx;

            if (sqr < bestSqr)
            {
                if ( direction * dx > 0)
                {
                    bestSqr = sqr;
                    best    = t;
                }
                else
                {
                    semiBestSqr = sqr;
                    semiBest    = t;
                }
            }

        }
        if(best !=null)
            return best;
        else
            return semiBest;
    }
    void PlatformIgnore(int currentFloor)
	{
		Collider2D platformCol = null;
		for (int i=0; i< platforms.Length; i++)
		{
			platformCol = platforms[i].GetComponent<Collider2D>();
			
			if (currentFloor==i+1)
				Physics2D.IgnoreCollision(col, platformCol, false);
			else
				Physics2D.IgnoreCollision(col, platformCol, true);
		}
	}
    protected override void Flip()
    {
        base.Flip();
        stateText.rectTransform.Rotate(0,180,0);
    }
}
