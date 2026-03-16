// -----------------------------------------------------------------------------
// Copyright (C) 2026 Cognition, Action, and Sustainability Unit
// University of Freiburg, Department of Psychology
// Implementation: Paul Soelder
// Supervision: Dr. Andrea Kiesel, Dr. Irina Monno
// All rights reserved.
// 
// This file is part of an MIT-licensed project.
// Proprietary assets used at runtime are excluded from this license.
// SPDX-License-Identifier: MIT
// -----------------------------------------------------------------------------

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// Manages an object pool.
/// </summary>
/// <remarks>
/// Instatiating game objects is expensive => re-use the instances with this
/// object pool. We use it only for the dot motion dots (200 instances).
/// </remarks>
public class ObjectPoolManager : MonoBehaviour
{
    public ObjectPool<GameObject> objectPool;
    public GameObject prefab;

    void Awake()
    {
        objectPool = new ObjectPool<GameObject>(
            createFunc: CreateInstance,
            actionOnGet: OnGet,
            actionOnRelease: OnRelease,
            actionOnDestroy: OnDestroyInstance,
            collectionCheck: true,
            defaultCapacity: 200,
            maxSize: 1000
        );
    }
    
    public GameObject CreateInstance()
    {
        GameObject gameObject = Object.Instantiate(prefab);
        return gameObject;
    }
    
    public void OnGet(GameObject gameObject)
    {
        // We do this manually (we want to be able to get hidden dots and
        // activate them later)
        // gameObject.SetActive(true);
    }
    
    public void OnRelease(GameObject gameObject)
    {
        gameObject.SetActive(false);
    }
    
    public void OnDestroyInstance(GameObject gameObject)
    {
        Destroy(gameObject);
    }
    
    public GameObject Get()
    {
        return objectPool.Get();
    }
    
    public void Release(GameObject gameObject)
    {
        objectPool.Release(gameObject);
    }
}
