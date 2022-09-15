using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class PoolingManager : MonoBehaviour
{
    private static PoolingManager instance = null;

    Dictionary<string, object> poolMap;
    Dictionary<string, int> idMap;
    public static PoolingManager GetInstance()
    {
        if(instance == null)
        {
            Debug.LogError("PoolingManager instance does not exist");
            return null;
        }
        return instance;
    }

    private void Awake()
    {
        if (instance != null)
            Destroy(gameObject);

        instance = this;
        poolMap = new Dictionary<string, object>();
        idMap = new Dictionary<string, int>();
    }
    public string GetPoolID(string id)
    {
        if (idMap.ContainsKey(id) == false)
        {
            string newKey = id + "0";
            idMap[id] = 1;
            return newKey;
        }
        else
        {
            string newKey = id + idMap[id].ToString();
            idMap[id]++;
            return newKey;
        }
    }
   
    public void AddObjectPool<T>(string id, Func<T> _instantiate_func, Action<T> _OnGetAction,
        Action<T> _OnReleaseAction, Action<T> _OnDestoryAction, Action<Queue<T>> _OnCleanUpAction, int _maxSize)
    {
        if (idMap.ContainsKey(id) == false)
        {
            poolMap.Add(id, new ObjectPool<T>(_instantiate_func, _OnGetAction, _OnReleaseAction,
                _OnDestoryAction, _OnCleanUpAction ,_maxSize));
        }

    }
    public ObjectPool<T> GetObjectPool<T>(string id)
    {
        if (poolMap.ContainsKey(id))
            return (ObjectPool <T>)poolMap[id];

        return null;
    }

    public void RemoveObjectPool<T>(string id)
    {
        if (poolMap.ContainsKey(id))
            poolMap.Remove(id);
    }
        
}
