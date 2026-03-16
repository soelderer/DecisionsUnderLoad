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
using System.Security.Cryptography;
using System.Text;
using LSL;
using UXF;

/// <summary>
/// A backend for integrating the Lab Streaming Layer (LSL).
/// </summary>
/// <remarks>
/// LSL is a protocol to exchange time-critical information like event signals
/// between different lab PC's and devices via the local network.
/// Right now, this only broadcasts the current events of the experiment
/// (e.g. begin_trial, dot_motion_task).
/// Before using it, make sure to calibrate the setup (see LSL documentation).
/// </remarks>
public class LSLSyncBackend : SyncDataStreamBackend
{
    string StreamType = "Markers";
    private string[] sample = {""};

    private StreamOutlet outlet;

    public override void SetUp()
    {
        StreamName = "UXF.LSLEvent";

        using var sha = SHA256.Create();

        var hashBytes = sha.ComputeHash(
            Encoding.UTF8.GetBytes(
                $"{StreamName}-{StreamType}"
            )
        );

        var hash = BitConverter
            .ToString(hashBytes)
            .Replace("-", "")
            .ToLowerInvariant();

        StreamInfo streamInfo = new StreamInfo(
            StreamName,
            StreamType,
            1,
            LSL.LSL.IRREGULAR_RATE,
            channel_format_t.cf_string,
            hash
        );

        outlet = new StreamOutlet(streamInfo);
    }

    public override void SendEvent(string eventName, double? timestamp = null)
    {
        if (outlet != null)
        {
            sample[0] = eventName;
            outlet.push_sample(sample);
        }
    }

    public override double Now() => LSL.LSL.local_clock();

    public override void Dispose()
    {
        outlet?.Dispose();
    }
}