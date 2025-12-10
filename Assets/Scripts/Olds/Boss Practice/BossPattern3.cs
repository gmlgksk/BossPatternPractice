using System;
using System.Collections;
using UnityEngine;

public class BossPattern3 : MonoBehaviour
{
    public Transform[] teleportPoints;
    public BossPattern teleportScript;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        teleportScript = GetComponent<BossPattern>();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void Start_DownATK_Pattern()
    {
        StartCoroutine( DownATK_Pattern() );
    }
    public IEnumerator DownATK_Pattern()
    {
        for (int i = 0; i < teleportPoints.Length; i++)
        {
            yield return StartCoroutine(DownATK(teleportPoints[i]));
        }
    }
    public IEnumerator DownATK(Transform point)
    {
        yield return StartCoroutine (teleportScript.Teleport(point));

        teleportScript.StartAttackPattern_1();
    }

}
