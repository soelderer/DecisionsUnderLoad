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
/// Manages the overlay camera.
/// </summary>
/// <remarks>
/// We need the second camera for the dot motion dots, so the SpriteRenderers
/// are rendered infront of everything else.
/// </remarks>
public class OverlayCameraManager : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        var cam = GetComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = Screen.height * 0.5f;       
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
