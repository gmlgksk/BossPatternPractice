using UnityEngine;

public class GroundCheck : MonoBehaviour
{
    [Header("=== 지면 감지 상태 ===")]
    public bool Ground;
    void Awake()
    {
        Ground=false;
    }
    void Start()
    {
        Ground=false;
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            Ground=true;
            Debug.Log("땅");
        }
    }
    void OnTriggerExit2D(Collider2D collision)
    {
        if(collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            Ground=false;
        }
    }
}
