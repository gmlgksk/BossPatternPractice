using UnityEngine;

public class InSightCheck : MonoBehaviour
{
    [Header("=== 시야 감지 상태 ===")]
    public bool inMySight;
    void Start()
    {
        inMySight=false;
    }
    void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.CompareTag("Player"))
        {
            inMySight=true;
        }
        
    }
    void OnTriggerExit2D(Collider2D collision)
    {
        if(collision.CompareTag("Player"))
        {
            inMySight=false;
        }
    }
}
