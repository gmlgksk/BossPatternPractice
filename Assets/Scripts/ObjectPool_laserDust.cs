using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Pool;

public class ObjectPool_laserDust : MonoBehaviour
{
    [SerializeField]
    private GameObject poolingObjectPrefab;

    private Queue<GameObject> poolingObjectQueue = new Queue<GameObject>();
    private void Awake()
    {
        Initialize(10);
        
    }

    private void Initialize(int initCount)
    {
        for(int i = 0; i < initCount; i++) 
        {
            poolingObjectQueue.Enqueue(CreateNewObject());
        }
    }

    private GameObject CreateNewObject()
    {
        var newObj = Instantiate(poolingObjectPrefab);
        newObj.gameObject.SetActive(false);
        newObj.transform.SetParent(transform);
        return newObj;
    }

    public GameObject GetObject()
    {
        if (poolingObjectQueue.Count > 0)
        {
            var obj = poolingObjectQueue.Dequeue();
            obj.transform.SetParent(null);
            obj.gameObject.SetActive(true);
            return obj;
        }
        else
        {
            var newObj = CreateNewObject();
            newObj.gameObject.SetActive(true);
            newObj.transform.SetParent(null);
            return newObj;
        }
    }

    public void ReturnObject(GameObject obj)
    {
        Wait(1f);
        obj.gameObject.SetActive(false);
        obj.transform.SetParent(transform);
        poolingObjectQueue.Enqueue(obj);
    }
    public IEnumerator Wait(float seconds)
    {
        yield return new WaitForSeconds(seconds);
    }
}