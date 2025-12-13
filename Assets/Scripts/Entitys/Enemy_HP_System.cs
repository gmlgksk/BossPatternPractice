using UnityEngine;

public class Enemy_HP_System : HP_System
{
    protected override void Reaction_Die()
    {
        anim.SetTrigger("die");
    }
}
