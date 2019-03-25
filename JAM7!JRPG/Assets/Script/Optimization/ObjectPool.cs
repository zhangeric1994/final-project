﻿using System.Collections.Generic;
using UnityEngine;

public class ObjectPool : MonoBehaviour
{
    public static ObjectPool Singleton { get; private set; }

    [Header("Config")]
    [SerializeField] private static Vector3 stackingPlace = Vector3.one * -100;
    [SerializeField] private PooledObject[] prefabs;

    private Stack<PooledObject>[] pooledObjects;

    private ObjectPool() { }

    public PooledObject GetObject(int id)
    {
        if (pooledObjects[id].Count > 0)
            return pooledObjects[id].Pop();

        PooledObject recyclable = Instantiate(prefabs[id], transform);
        recyclable.id = id;

        return recyclable;
    }

    public T GetObject<T>(int id) where T : MonoBehaviour
    {
        if (pooledObjects[id].Count > 0)
            return pooledObjects[id].Pop().GetComponent<T>();

        T obj = Instantiate(prefabs[id].GetComponent<T>(), stackingPlace, Quaternion.identity, transform.GetChild(id));

        if (obj)
            obj.GetComponent<PooledObject>().id = id;

        return obj;
    }

    public void Recycle(PooledObject obj)
    {
        if (obj.id >= 0)
        {
            obj.transform.position = stackingPlace;
            pooledObjects[obj.id].Push(obj);
        }
        else
            Destroy(obj.gameObject);
    }

    private void Awake()
    {
        if (!Singleton)
        {
            Singleton = this;
            DontDestroyOnLoad(gameObject);

            pooledObjects = new Stack<PooledObject>[prefabs.Length];

            GameObject child;
            for (int id = 0; id < pooledObjects.Length; id++)
            {
                pooledObjects[id] = new Stack<PooledObject>(256);
                child = new GameObject();
                child.name = id.ToString();
                child.transform.parent = transform;
            }
        }
        else if (this != Singleton)
            Destroy(gameObject);
    }
}