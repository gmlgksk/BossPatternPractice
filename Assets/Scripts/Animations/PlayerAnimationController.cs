using UnityEngine;

public class PlayerAnimationController : MonoBehaviour
{
    PlayerController player;
    void Awake()
    {
        player = GetComponentInParent<PlayerController>();
    }
    public void ExitCurrentState()
    {
        player.ExitCurrentState();
    }
}
