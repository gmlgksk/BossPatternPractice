using UnityEngine;

public class AttackAnimation : MonoBehaviour
{
    private Animator anim;
    public GameObject attackCollider;

    void Awake()
    {
        anim = GetComponent<Animator>();
        gameObject.SetActive(false); // 시작할 땐 꺼두기
    }
    // 활성화될 때 항상 0프레임부터 재생
    void OnEnable() =>anim.Play(0, -1, 0f);
    // 공격오브젝트 비활성화.
    void DisableAttack() => transform.parent.gameObject.SetActive(false); 
    // 공격범위 비활성화.
    void DisableAttackCollision() => attackCollider.SetActive(false); 

    // 바깥에서 호출할 함수
    public void Play()
    {
        // 혹시 이미 켜져있을 수도 있으니까 리셋용
        gameObject.SetActive(false);
        gameObject.SetActive(true); // OnEnable → 재생 시작
    }
}
