using UnityEngine;

public class PlayHitLineEffect : MonoBehaviour
{
    [Header("이펙트 설정")]
    [SerializeField] private GameObject slashEffectPrefab; // 발사할 이미지(프리팹)
    [SerializeField] private float slashSpeed = 20f;       // 이동 속도
    [SerializeField] private float slashLifeTime = 0.4f;   // 유지 시간 (필요하면)
    [SerializeField] private float startOffset = 10f;   // 유지 시간 (필요하면)

    // 공격 성공 시 호출할 함수
    // attackDir : 공격 방향(월드 공간 기준, 정규화 벡터 권장)
    public void PlayHitEffect(Vector2 attackDir)
    {
        if (slashEffectPrefab == null)
        {
            Debug.LogWarning("[PlayerHitEffectLauncher] slashEffectPrefab 이 비어있습니다.");
            return;
        }

        if (attackDir.sqrMagnitude < 0.0001f)
        {
            // 방향이 0이면 플레이어의 바라보는 방향으로 대체
            attackDir = Vector2.right;
        }
        attackDir.Normalize();

        // ★ 공격 방향의 반대쪽(startOffset만큼 뒤)에서 생성
        Vector3 spawnPos = transform.position - (Vector3)attackDir * startOffset;

        GameObject effectObj = Instantiate(slashEffectPrefab, spawnPos, Quaternion.identity);

        // 스프라이트가 오른쪽을 기본 방향으로 가정 (위쪽이면 .up 으로)
        effectObj.transform.right = attackDir;

        // 이펙트 이동 스크립트에 초기값 전달
        var mover = effectObj.GetComponent<UnscaledMoveEffect>();
        if (mover != null)
        {
            mover.Init(attackDir, slashSpeed, slashLifeTime);
        }
    }
}
