using UnityEngine;
using UnityEngine.InputSystem; // 새 Input System 사용 시

public class MouseDirectionFromPlayer : MonoBehaviour
{
    [Header("카메라 (비우면 자동으로 Main Camera 사용)")]
    [SerializeField] private Camera cam;

    [Header("기즈모 옵션")]
    public bool drawGizmo = true;
    public float gizmoLength = 3f;

    // 외부에서 읽기용
    public Vector2 MouseWorldPos  { get; private set; }
    public Vector2 MouseDirection { get; private set; }   // (정규화된 방향)

    void Awake()
    {
        if (cam == null)
            cam = Camera.main;
    }

    void Update()
    {
        UpdateMouseDirection();
    }

    /// <summary>
    /// 캐릭터 기준으로 마우스까지의 방향 벡터를 계산
    /// </summary>
    void UpdateMouseDirection()
    {
        if (cam == null) return;

        // --- 1) 화면 좌표에서 마우스 위치 읽기 ---
        Vector2 mouseScreenPos = Vector2.zero;

        if (Mouse.current != null)                    // 새 Input System
        {
            mouseScreenPos = Mouse.current.position.ReadValue();
        }
        else                                          // 혹시 옛날 Input 쓰면 대비용
        {
            mouseScreenPos = Input.mousePosition;
        }

        // --- 2) 월드 좌표로 변환 ---
        Vector3 world = cam.ScreenToWorldPoint(mouseScreenPos);

        // 2D라면 Z를 캐릭터와 맞춰주면 깔끔
        world.z = transform.position.z;

        MouseWorldPos = world;

        // --- 3) 방향 벡터 계산 (마우스 - 캐릭터) ---
        Vector2 rawDir = (Vector2)(MouseWorldPos - (Vector2)transform.position);

        // 길이 1인 방향 벡터로 정규화
        MouseDirection = rawDir.normalized;
    }

    /// <summary>
    /// 필요하면 다른 스크립트에서 바로 호출해서 사용
    /// </summary>
    public Vector2 GetMouseDirection()
    {
        return MouseDirection;
    }

    void OnDrawGizmos()
    {
        if (!drawGizmo) return;

        // 에디터에서 cam이 null일 수 있으니 한 번 더 방어
        if (cam == null)
            cam = Camera.main;
        if (cam == null) return;

        // 게임 실행 중일 때만 실제 마우스 방향 기준으로 그림
        if (Application.isPlaying)
        {
            Gizmos.color = Color.yellow;

            Vector3 from = transform.position;
            Vector3 to   = from + (Vector3)MouseDirection * gizmoLength;

            // 방향선
            Gizmos.DrawLine(from, to);
            // 마우스가 있는 대략적인 방향 끝점에 작은 구
            Gizmos.DrawSphere(to, 0.1f);
        }
    }
}
