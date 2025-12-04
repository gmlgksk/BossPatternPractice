using UnityEngine;

public class HP_System : MonoBehaviour
{
    public float hp_max;
    public float hp_current;
    private Entity entity;
    private Animator anim;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        hp_current  = hp_max;
        entity      = GetComponent<Entity>();
        anim        = GetComponentInChildren<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        Handle_HP();
    }
    public void Handle_HP()
    {
        if (hp_current <= 0) 
        {
            entity.Die();
        }
    }
    public void Health_Init()
    {
        hp_current = hp_max;
    }
    public void Health_Reduce()
    {
        if (hp_current > 0)
        {
            hp_current-=1;
            if (hp_current == 0)    Reaction_Die();
            else                    Reaction_hurt();
        }
        else
        {
            Debug.Log("hp is 0");
            return;
        }
    }
    public void Health_Recover()
    {
        if (hp_current < hp_max)
        {
            hp_current+=1;
            Reaction_heal();
        }
        else
        {
            Debug.Log("hp is max");
            return;
        }
    }
    public void Reaction_heal()
    { // 회복 효과
    
    }
    public void Reaction_hurt()
    { // 피격 효과
    
    }
    public void Reaction_Die()
    { // 사망효과
        anim.SetTrigger("die");
    }
}
