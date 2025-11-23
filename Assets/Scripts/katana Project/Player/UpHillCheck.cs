using UnityEngine;
public class UpStairCheck : MonoBehaviour
{
    [Header("=== 계단 감지 상태 ===")]
    public bool stairCheck;
    public bool wall;
    void Awake()
    {
        stairCheck=false;
    }
    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            if (collision.CompareTag("Stair")) stairCheck = true;
        }
        else
            stairCheck = false;
    }
}
