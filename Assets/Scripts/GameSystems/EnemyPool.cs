using UnityEngine;

public class EnemyPool : MonoBehaviour
{
    [SerializeField]
    private int _remainEnemy;

    void Awake()
    {
        _remainEnemy = 0;
    }
    void Update()
    {
        _remainEnemy = ChildObjectCheck();
    }

    public int ChildObjectCheck()
    {
        Enemy[] children = gameObject.GetComponentsInChildren<Enemy>(includeInactive: false);
        int count = 0;

        foreach (var c in children)
        {
            if (!c.isDie)
                count++;
        }

        return count;
    }

    public int ShowRemainEnemy()
    {
        return _remainEnemy;
    }
}
