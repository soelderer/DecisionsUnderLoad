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

using System;
using UnityEngine;
using UXF;

/// <summary>
/// Base class for backends that synchronize data streams between different
/// devices.
/// </summary>
/// <remarks>
/// Base class for the LSL integration. Could be extended to incorporate other
/// backends like TTL pulses.
/// </remarks>
public abstract class SyncDataStreamBackend : MonoBehaviour
{
    public bool active = true;
    public Session session { get; private set; }
    public string StreamName { get; protected set; }

    public void Initialise(Session session)
    {
        this.session = session;
    }

    public abstract void SetUp();

    public abstract void SendEvent(string eventName, double? timestamp = null);

    public abstract double Now();

    public abstract void Dispose();
}