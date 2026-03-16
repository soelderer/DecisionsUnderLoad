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

/// <summary>
/// A small script for debugging object movement. Legacy code but may be useful.
/// </summary>
public class DebugMovement : MonoBehaviour
{
    public bool fixedUpdate = false;

    public float speed = 0.4f;

    Rigidbody rb;

    void Start()
    {
        if (fixedUpdate)
            GetComponent<Rigidbody>().interpolation = RigidbodyInterpolation.Interpolate;

        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (!fixedUpdate)
        {
            transform.position += Vector3.right * Time.deltaTime * speed;
        }
    }

    void FixedUpdate()
    {
        if (fixedUpdate)
        {
            // transform.position += Vector3.right * Time.fixedDeltaTime * speed;

            rb.MovePosition(transform.position + Vector3.right * Time.fixedDeltaTime * speed);
        }
    }
}
