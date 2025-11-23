using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class PlayerDie : MonoBehaviour
{
    [Header("=== 필수 컴포넌트 ===")]
    [SerializeField] public Animator animator;          // 플레이어 Animator

    [Header("=== 애니메이션 설정 ===")]
    [SerializeField] private string dieTriggerName = "die"; // Animator 트리거 이름

    [Header("=== 사망 상태 ===")]
    public bool IsDead { get; private set; }             // 사망 여부 조회용 프로퍼티

    private void Reset()
    {
        // 컴포넌트를 자동 할당해 편의성 향상
        animator = GetComponent<Animator>();
    }

    /// <summary>
    /// 외부에서 호출해 플레이어를 사망 상태로 전환
    /// </summary>
    public void Die()
    {   
        // 중복 방지
        IsDead = true;
        GetComponent<Rigidbody2D>().simulated = false;
        // 애니메이션 트리거 발동
        if (animator == null) animator = GetComponent<Animator>();
        if (animator != null && !string.IsNullOrEmpty(dieTriggerName))
            animator.SetTrigger(dieTriggerName);

        // TODO: 이동/공격 스크립트 비활성화, 점수 처리 등 추가 로직이 필요하면 여기서 호출
        // 공격 범위 오브젝트 비활성화
        Transform Indicator = transform.Find("Attack Indicator");
        if (Indicator != null)
        {
            Indicator.gameObject.SetActive(false);
        }
        StartCoroutine(DieAndReload());   
    }
    private IEnumerator DieAndReload()
    {
        yield return new WaitForSeconds(2f); // 2초 대기

        Debug.Log("플레이어 죽음");
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);

    }
}
