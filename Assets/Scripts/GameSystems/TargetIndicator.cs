using DG.Tweening;
using UnityEngine;

public class TargetIndicator : MonoBehaviour
{
    [Header("참조")]
    public Camera cam;
    public Transform player;       // 필요하면 안 써도 됨 (카메라 기준)
    public Transform target;       // 탈출구
    public Transform targetIndicator;// 탈출구 가리킬 위치

    [Header("UI")]
    public Canvas canvas;
    public RectTransform arrowUI;          // 화면 가장자리 화살표

    [Header("옵션")]
    public float edgePadding = 50f;        // 화면 가장자리에서 약간 안쪽으로
    public float arrowOffsetFromEdge = 0f; // 추가 여유 (원하면 사용)

    RectTransform canvasRect;
    Sequence markerSeq;   // ✨ 마커 연출용 시퀀스
    EnemyPool enemyPool;
    bool hasEnemyPool;
    void Awake()
    {
        if (!cam) cam = Camera.main;
        canvasRect = canvas.GetComponent<RectTransform>();
        
        enemyPool =FindAnyObjectByType<EnemyPool>();
        hasEnemyPool = enemyPool? true: false;
        SetupMarkerSequence();
    }
    void LateUpdate()
    {
        if (!target || (hasEnemyPool && enemyPool.ShowRemainEnemy()>0)) 
        {
            arrowUI.gameObject.SetActive(false);
            markerSeq?.Pause();
            return;
        }

        target.gameObject.SetActive(true);

        // 1. 월드 → 뷰포트
        Vector3 vp = cam.WorldToViewportPoint(target.position);
        bool isOnScreen =
            vp.z > 0f &&
            vp.x > 0f && vp.x < 1f &&
            vp.y > 0f && vp.y < 1f;

        if (isOnScreen)
        {
            ShowOnScreenMarker();
        }
        else
        {
            ShowOffScreenArrow(vp);
        }
    }








    void SetupMarkerSequence()
    {
        if (arrowUI == null) return;

        // 초기값 리셋
        arrowUI.localScale = Vector3.one;
        arrowUI.anchoredPosition = Vector2.zero;
        arrowUI.localRotation = Quaternion.Euler(0, 0, 180f); // 아래 방향

        // 이미 있다면 재사용
        markerSeq?.Kill();
        markerSeq = DOTween.Sequence()
            .SetAutoKill(false)
            .SetLoops(-1, LoopType.Restart)
            .Pause(); // 기본은 멈춰두기

        // 1단계: 살짝 납작해지면서 살짝 올라감
        markerSeq.Append(
            arrowUI.DOScaleY(0.8f, 0.08f).SetEase(Ease.OutQuad)
        );
        markerSeq.Join(
            arrowUI.DOAnchorPosY(8f, 0.08f).SetEase(Ease.OutQuad)
        );

        // 2단계: 원래 비율로 돌아오면서 더 위로
        markerSeq.Append(
            arrowUI.DOScaleY(1.1f, 0.12f).SetEase(Ease.OutQuad)
        );
        markerSeq.Join(
            arrowUI.DOAnchorPosY(16f, 0.12f).SetEase(Ease.OutQuad)
        );

        // 3단계: 스케일/위치 원상복귀
        markerSeq.Append(
            arrowUI.DOScaleY(1f, 0.12f).SetEase(Ease.InQuad)
        );
        markerSeq.Join(
            arrowUI.DOAnchorPosY(0f, 0.12f).SetEase(Ease.InQuad)
        );
    }

    void ShowOnScreenMarker()
    {
        arrowUI.gameObject.SetActive(true);

        // 월드 → 스크린 → 캔버스 로컬
        Vector2 screenPos = cam.WorldToScreenPoint(targetIndicator.position);

        Vector2 uiPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, screenPos, 
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : cam, 
            out uiPos);

        arrowUI.anchoredPosition = uiPos;
        // 필요하면 마커 회전 초기화
        arrowUI.rotation = Quaternion.Euler(0f, 0f, 180f);
        
        markerSeq?.Play();
    }

    void ShowOffScreenArrow(Vector3 viewportPos)
    {
        arrowUI.gameObject.SetActive(true);

        markerSeq?.Pause();

        // === 1) 타겟 스크린 위치 구하기 ===
        Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        Vector2 screenPos   = cam.WorldToScreenPoint(target.position);

        // 카메라 뒤에 있는 경우 방향 반대로 뒤집기
        if (viewportPos.z < 0f)
        {
            screenPos = screenCenter - (screenPos - screenCenter);
        }

        // === 2) 중심 → 타겟 방향 ===
        Vector2 dir = (screenPos - screenCenter).normalized;

        // === 3) 화면 경계와의 교점 계산 ===
        float w = screenCenter.x - edgePadding;
        float h = screenCenter.y - edgePadding;

        float scale = Mathf.Min(
            Mathf.Abs(w / dir.x),
            Mathf.Abs(h / dir.y)
        );

        Vector2 edgePos = screenCenter + dir * scale;
        edgePos += dir * arrowOffsetFromEdge; // 여유로 조금 더 바깥쪽/안쪽 조절용

        // === 4) 스크린 → 캔버스 로컬 좌표 ===
        Vector2 uiPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, edgePos, 
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : cam, 
            out uiPos);

        arrowUI.anchoredPosition = uiPos;

        // === 5) 화살표 방향 회전 ===
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        // 화살표 이미지가 "위쪽"을 향하고 있다면 -90 보정
        arrowUI.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
    }
}
