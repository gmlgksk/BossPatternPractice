using UnityEngine;
using UnityEngine.InputSystem;
public class AttackIndicator2D : MonoBehaviour
{
    [Header("=== 참조 설정 ===")]
    [SerializeField] private Transform player;          // 주인공

    [Header("=== 표시 설정 ===")]
    [SerializeField] private float rotationOffset = 270f; // 삼각형이 "오른쪽"을 0°로 본다면 90
    [SerializeField] private float indicatorDistance = 2.5f;   // 원하는 반지름
    [SerializeField] private float scaleMultiplier; // 클릭 시 배율

    private Vector3 originalScale;

    private void Awake()
    {
        if (player == null) player = transform.parent;
        originalScale = transform.localScale;
        scaleMultiplier = 2f;
    }

    private void Update()
    {
        RotateTowardMouse2D();
        HandleScale();
    }

    // ───────── 회전 (2D 전용) ─────────
    private void RotateTowardMouse2D()
    {
        Vector3 mouseScreenPos = Mouse.current.position.ReadValue();
        Vector3 mouseWorldPos  = Camera.main.ScreenToWorldPoint(mouseScreenPos);
        mouseWorldPos.z = 0f;

        Vector2 dir = (mouseWorldPos - player.position).normalized;

        // 회전
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle + rotationOffset);

        // **플레이어에서 dir 방향으로 distance만큼 떨어뜨림**
        transform.position = player.position + (Vector3)(dir * indicatorDistance);
    }

    // ───────── 클릭 시 단순 크기 변경 ─────────
    private void HandleScale()
    {
        if (Mouse.current.leftButton.isPressed)
        {
            transform.localScale = originalScale * scaleMultiplier;
            gameObject.SetActive(true);

        }
            
        else
            transform.localScale = originalScale;
    }
}