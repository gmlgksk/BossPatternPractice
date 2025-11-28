using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class Enemy : Entity
{
    private enum State {Patrol, Warning, Chase}
    [Header("State detail")]
    [SerializeField] State currentState;
    [SerializeField] bool lookedTarget;
    [SerializeField] float lookingTimer = 0f;
    [SerializeField] float chasingTimer = 0f;
    [SerializeField] float identityTime = 1f;
    [SerializeField] float chaseEndTime = 1f;
    [Header("Indecator")]
    public String state;
    public String destinationLog;
    [Header("Layer")]
    public LayerMask platformLayer;      // 플랫폼 레이어(한 개)
    public LayerMask platformIgnoreLayer;

    [Header("Floors")]
    [Tooltip("플레이어 초기화에 사용. 각 층의 Y값 (index 0=1층, 1=2층, 2=3층)")]
    public int Floor_Number;
    public float[] floorY;
    public Transform[] platforms;

    [Header("Chase")]
    public float chaseSpeed = 6f;
    public Transform target;

    [Header("Stairs")]
    // stairs[floor][i] : floor층 계단 포인트들
    public Transform[][] stairs = new Transform[3][];
    public Transform[] stairs_0;
    public Transform[] stairs_1;
    public Transform[] stairs_2; 
    private int currentFloor;   // 0,1,2
    private int targetFloor;    // 0,1,2
    private int goToUpDown;








    protected override void Awake()
    {
        base.Awake();
        
        stairs[0] = stairs_0;
        stairs[1] = stairs_1; 
        stairs[2] = stairs_2;

        floorY = new float[Floor_Number];
        currentFloor = FloorCheck(transform);
    }
    protected override void Update()
    {
        base.Update();
        Handle_State();
        state = $"curFloor:{currentFloor},targetFloor:{targetFloor},updown{goToUpDown}";
        targetFloor = FloorCheck(target);
    }









    // ========== 핵심함수들 ==========
    public void Handle_State()
    {// 순찰 -> 경계 -> 추적 -> 경계 -> 순찰
        switch (currentState)
        {
            // 순찰 상태
            case State.Patrol:
                if (lookedTarget)
                {
                    currentState = State.Warning;
                    break;
                }
                break;
            // 경계 상태
            case State.Warning:
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
            // 추적 상태
            case State.Chase:
                lookingTimer = 0f;
                chasingTimer += Time.deltaTime;

                if (chasingTimer > chaseEndTime)
                    currentState = State.Chase;

                break;
            default:
                break;
        }   
    }
    private void Handle_MovementByState(State state)
    {
        if(state== State.Patrol)
            Patrol();
        else if(state== State.Warning)
            Warning();
        else if(state== State.Chase)
            Chase();
            
    }
    public void Patrol()
    {
        
    }
    public void Warning()
    {
        
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

        MoveTo(destination.x);

        if (Vector2.Distance(transform.position,destination) < 0.1f)
            setCurrentFloor(goToUpDown);
    }








    // ========== 보조함수들 ==========
    public int FloorCheck(Transform trans)
	{
		float posY = trans.position.y;
		if (floorY[2] < posY) return 2;
		if (floorY[1] < posY) return 1;
		if (floorY[0] < posY) return 0;
        return 0;
	}

    public void DrawSight()
    {
        Collider2D[] enemyColliders = Physics2D.OverlapCircleAll(attackPoint.position, attackRadius, whatIsTarget);
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
}
