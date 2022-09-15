using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class ObjectPool<T>
{
    Func<T> instantiate_func;
    Action<T> OnGetAction;
    Action<T> OnReleaseAction;
    Action<T> OnDestoryAction;
    Action<Queue<T>> OnCleanUpAction;
    int maxSize;
    Queue<T> readyPool;

    //Dictionary<string, GameObject> releasedPool;
    public int getPoolCount() { return readyPool.Count; }
    public int activeCount { get; private set; }

    public ObjectPool(Func<T> _instantiate_func, Action<T> _OnGetAction,
        Action<T> _OnReleaseAction, Action<T> _OnDestoryAction, Action<Queue<T>> _OnCleanUpAction, int _maxSize)
    {
        readyPool = new Queue<T>();
        instantiate_func = _instantiate_func;
        OnGetAction = _OnGetAction;
        OnReleaseAction = _OnReleaseAction;
        OnDestoryAction = _OnDestoryAction;
        OnCleanUpAction = _OnCleanUpAction;
        maxSize = _maxSize;
    }
    
    public T GetObject()
    {
        if(readyPool.Count + activeCount < maxSize || readyPool.Count == 0)
        {
            var instance = instantiate_func();
            ++activeCount;
            return instance;
        }
        else
        {
            var instance = readyPool.Dequeue();
            OnGetAction(instance);
            ++activeCount;
            return instance;
        }
    }

    public void ReleaseObject(T instance)
    {
        if(readyPool.Count + activeCount <= maxSize)
        {
            OnReleaseAction(instance);
            readyPool.Enqueue(instance);
            --activeCount;
        }
        else
        {
            /*
             * TODO: 외부 요인으로 destroy 되었을때 예외처리 필요
             */
            OnDestoryAction(instance);
            --activeCount;
        }
    }

    public void CleanUp()
    {
        OnCleanUpAction(readyPool);
    }
}
