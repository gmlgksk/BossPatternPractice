using UnityEngine;

public class EntityAnimationController : MonoBehaviour
{
    Entity entity;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        entity  = GetComponentInParent<Entity>();
    }

    public void Attack_Perform()
    {
        entity.Attack_Perform();
    }
    public void Attack_End()
    {
        entity.Attack_End();
    }
    public void Die_End()
    {
        entity.Die_End();
    }

}
