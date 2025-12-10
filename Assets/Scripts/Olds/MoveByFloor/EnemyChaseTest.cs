using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class EnemyChaseSimple : MonoBehaviour
{
    
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
    public float moveSpeed = 3f;
    public Transform target;

    [Header("Stairs")]
    // stairs[floor][i] : floor층 계단 포인트들
    public Transform[][] stairs = new Transform[3][];
    public Transform[] stairs_0;
    public Transform[] stairs_1;
    public Transform[] stairs_2;

    Rigidbody2D rb;
    Collider2D col;

    int currentFloor;   // 0,1,2
    int targetFloor;    // 0,1,2
    private int goToUpDown;

    void Awake()
    {
        rb  = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        stairs[0] = stairs_0;
        stairs[1] = stairs_1; 
        stairs[2] = stairs_2;

        currentFloor = FloorCheck(transform);
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



    void Update()
    {
        if (!target) return;

        state = $"curFloor:{currentFloor},targetFloor:{targetFloor},updown{goToUpDown}";
        targetFloor = FloorCheck(target);
		Destinate();
    }
    void Destinate()
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
    private void MoveTo(float destinationX)
    {
        float deltaX = destinationX - rb.position.x;
        float dir = Mathf.Sign(deltaX);
        if (Mathf.Abs(deltaX) < 0.05f) dir = 0f;

        rb.linearVelocityX = dir * moveSpeed;
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

    

    // ====== 계단 포인트 찾기 (이전처럼 인수 받는 형태) ======
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
