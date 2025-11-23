// using Microsoft.Unity.VisualStudio.Editor;
using UnityEngine;
using UnityEngine.UI;

public class TimeGaugeActivator : MonoBehaviour
{//IngameUI

    [Header("field")]
    public GameObject timeGageOJ;
    public float currentFreezeRatio;
    [Header("elements")]
    public float totalBlock;
    public Color activated;
    public Color deactivated;


    

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        timeGageOJ = transform.GetChild(0).gameObject;
        currentFreezeRatio = GetComponentInParent<TimeFreezeController>().GetCurrentFreezeRatio();
    }

    // Update is called once per frame
    void Update()
    {
        currentFreezeRatio = GetComponentInParent<TimeFreezeController>().GetCurrentFreezeRatio();
        Display();
    }

    void Display()
    {

        float activeBlock = totalBlock * currentFreezeRatio;

        for (int i = 0; i < (int)totalBlock; i++)
        {
            RawImage img = timeGageOJ.transform.GetChild(i).GetComponent<RawImage>();
            img.color = (i < activeBlock) ? activated : deactivated;
        }
    }
}
