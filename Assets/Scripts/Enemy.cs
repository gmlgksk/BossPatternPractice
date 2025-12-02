using System;
using TMPro;
using UnityEngine;

public class Enemy : Entity
{
    private enum State {Patrol, Warning, Chase, Attack}
    [Header("===== Additional Details! =====")]
    [Header("== State detail ==")]
    [SerializeField] State currentState;
    [SerializeField] bool lookedTarget;
    [SerializeField] float lookingTimer = 0f;
    [SerializeField] float chasingTimer = 0f;
    [SerializeField] float identityTime = 1f;
    [SerializeField] float chaseEndTime = 1f;

    [Header("== Indecator ==")]
    public String totalState;
    public String entityState;
    public String destinationLog;


    [Header("== Layer ==")]
    public LayerMask platformLayer;      // 플랫폼 레이어(한 개)
    public LayerMask platformIgnoreLayer;

    [Header("== Floors ==")]
    [Tooltip("플레이어 초기화에 사용. 각 층의 Y값 (index 0=1층, 1=2층, 2=3층)")]
    public int Floor_Number;
    public float[] floorY;
    public Transform[] platforms;

    [Header("== Patrol ==")]
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private int patrolIndex;
    // [SerializeField] private Transform sightPoint;
    [SerializeField] private Vector2 sightDirection;
    [SerializeField] private float sightDistance;
    [SerializeField] private float spreadAngle;

    [Header("== Attack ==")]
    public GameObject ATK_Range;
    private Collider2D ATK_Col;

    [Header("== Chase ==")]
    public float chaseSpeed = 6f;
    public Transform target;

    [Header("== Stairs ==")]
    // stairs[floor][i] : floor층 계단 포인트들
    public Transform[][] stairs = new Transform[3][];
    public Transform[] stairs_0;
    public Transform[] stairs_1;
    public Transform[] stairs_2; 
    private int currentFloor;   // 0,1,2
    private int targetFloor;    // 0,1,2
    private int goToUpDown;
    
    public TMP_Text stateText; 










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
        stateText = GetComponentInChildren<TMP_Text>();
    }
    protected override void Update()
    {
        base.Update();
        Handle_State();
        HandleSight();
        Handle_MovementByState(currentState);
        totalState = $"curFloor:{currentFloor},targetFloor:{targetFloor},updown{goToUpDown}";
        if(target)
            targetFloor = FloorCheck(target);

        stateText.text = entityState;
    }









    // ========== 핵심함수들 ==========
    public void Handle_State()
    {
        // [순찰] -> [경계] -> [추적]&[공격] -> [경계] -> [순찰]
        // 공격범위에 들어오면 무조건 [공격]
        if (OnAttackRange() && target) currentState = State.Attack;
        
        switch (currentState)
        {
            // ===== 순찰 상태
            case State.Patrol:
                entityState = "patrol";
                if (lookedTarget)
                {
                    currentState = State.Warning;
                    break;
                }
                break;
            // ===== 경계 상태
            case State.Warning:
                entityState = "warning";
                chasingTimer = 0;
                if (lookedTarget)
                    lookingTimer += Time.deltaTime;
                else
                    lookingTimer -= Time.deltaTime;
                
                // 경계 애니메이션, 움직임 등

                if (lookingTimer > identityTime)
                    currentState = State.Chase;
                else if (lookingTimer < 0)
                    currentState = State.Patrol;

                break;
            // ===== 추적 상태
            case State.Chase:
                entityState = "chase";
                lookingTimer = 0f;
                chasingTimer += Time.deltaTime;

                if (lookedTarget) chasingTimer=0;
                if (chasingTimer > chaseEndTime)
                    currentState = State.Warning;
                break;
            // ===== 공격상태
            case State.Attack:
                entityState = "attack";
                if (OnAttackRange()) currentState = State.Chase;
                break;

            default:
                break;
        }   
    }
    private void Handle_MovementByState(State state)
    {
        if(state == State.Patrol)
            Patrol();
        else if(state == State.Warning)
            Warning();
        else if(state == State.Chase)
            Chase();
            
    }
    public void Patrol()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
            return;

        // 현재 목표 지점
        Vector2 targetPos = patrolPoints[patrolIndex].position;

        MoveTo(targetPos.x,moveSpeed);

        // 도착했으면 다음 포인트로 인덱스 변경
        if (Vector2.Distance(transform.position, targetPos) < 0.1f)
        {
            patrolIndex++;
            if (patrolIndex >= patrolPoints.Length)
                patrolIndex = 0; // 다시 처음으로
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
            
            destination.x = FindNearestStair(stairs,currentFloor-1).position.x;
            Debug.Log("chech Down");
            destinationLog=$"downPoint, x:{destination.x}";
        }
        if (currentFloor < targetFloor)
        {
            goToUpDown = 1;
            destination.x = FindNearestStair(stairs,currentFloor).position.x;
            Debug.Log("chech Up");
            destinationLog=$"upPoint, x:{destination.x}";
        }

        MoveTo(destination.x,chaseSpeed);

        if (Vector2.Distance(transform.position,destination) < 0.1f)
            setCurrentFloor(goToUpDown);
    }
    public void Attack()
    {
        target.GetComponent<HP_System>().Health_Reduce();
        new WaitForSeconds(1f);
    }

    public bool OnAttackRange()
    {
        if(Physics2D.OverlapCircle(sightPoint.position,attackRadius,whatIsTarget))
            return true;
        return false;
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
        RaycastHit2D hit = Physics2D.Raycast(
            sightPoint.position,
            dir * faceDir,
            sightDistance,
            whatIsTarget          // 👈 Player + Wall 레이어만 맞음
        );

        if (hit.collider == null)
        {
            Debug.Log($"[{debugTag}] 아무것도 안 맞음");
            return false;
        }

        int hitLayer = hit.collider.gameObject.layer;

        // 1️⃣ 벽 레이어 먼저 체크 → 시야 차단
        if (hitLayer != whatIsTarget)
        {
            Debug.Log($"[{debugTag}] 벽에 막힘 (레이 중단)");
            return false;
        }

        // 2️⃣ 플레이어 레이어면 → 타겟 발견
        if (hitLayer == whatIsTarget)
        {
            target = hit.collider.transform;
            lookedTarget = true;
            Debug.Log($"[{debugTag}] 플레이어 발견!");
            return true;
        }
        return false;
    }


    private Vector2 RotateVector(Vector2 v, float degrees)
    {
        // 2D 에서는 z축 회전만 쓰니까 이렇게 처리
        return (Vector2)(Quaternion.Euler(0f, 0f, degrees) * v);
    }







    // ========== 보조함수들 ==========
    private void OnDrawGizmos()
    {
        if (sightPoint == null) return;

        Vector2 baseDir = sightDirection.normalized;
        baseDir.x *= faceDir;
        Vector2 dirCenter = baseDir;
        Vector2 dirUp     = RotateVector(baseDir,  spreadAngle);
        Vector2 dirDown   = RotateVector(baseDir, -spreadAngle);

        Gizmos.color = Color.red;

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

    public void DrawSight()
    {
        Collider2D[] enemyColliders = Physics2D.OverlapCircleAll(sightPoint.position, attackRadius, whatIsTarget);
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
    Transform FindNearestStair(Transform[][] points, int floor)
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

        Transform best = null;
        float bestSqr = float.MaxValue;

        foreach (var t in list)
        {
            if (!t) continue;

            float dx  = t.position.x - rb.position.x;
            float sqr = dx * dx;

            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                best    = t;
            }
        }
        return best;
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
