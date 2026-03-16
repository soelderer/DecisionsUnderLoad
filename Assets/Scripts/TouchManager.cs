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
/// Manages the haptic touch device via the Haptic Plugin.
/// </summary>
public class TouchManager : MonoBehaviour
{
    public GameObject touchCollider; // assign GO in inspector
    public GameObject touchStylus; // assign GO in inspector
    public HapticPlugin hapticPlugin; // assign HapticActor_DefaultDevice in inspector

    private bool isEnabled;
    public bool IsEnabled => isEnabled;

    private Matrix4x4 cachedTransform;

    void Start()
    {
        if (touchCollider == null || touchStylus == null)
        {
            Debug.LogError("[TouchManager] touchCollider or touchStylus is not assigned.");
        }

        // While initial UXF instructions are shown, the device should be
        // disabled.
        Disable();
    }

    void Update()
    {
        // Allow Tracker thread access to the transform matrix.
        // Could probably be cached only once in Start?
        cachedTransform = hapticPlugin.transform.localToWorldMatrix;
    }

    /// <summary>
    /// Show the game objects related to the Haptic Touch device.
    /// </summary>
    public void Enable()
    {
#if UNITY_EDITOR
        Debug.Log("[TouchManager] Enabled haptic touch device.");
#endif
        touchCollider.SetActive(true);
        touchStylus.SetActive(true);

        isEnabled = true;
    }

    /// <summary>
    /// Hide the game objects related to the Haptic Touch device.
    /// </summary>
    public void Disable()
    {
#if UNITY_EDITOR
        Debug.Log("[TouchManager] Disabled haptic touch device.");
#endif
        touchCollider.SetActive(false);
        touchStylus.SetActive(false);

        isEnabled = false;
    }

    /// <summary>
    /// Returns the transform position of the haptic collider GO (stylus tip).
    /// </summary>
    /// <returns></returns>
    /// <remarks>
    /// This does not call the API but just returns the GO position.
    /// </remarks
    public Vector3 GetTransformPosition()
    {
        return touchCollider.transform.position;
    }

    /// <summary>
    /// Transforms a 4x4 matrix from local to world coordinates based on the
    /// haptic touch transform.
    /// </summary>
    /// <param name="local"></param>
    /// <returns></returns>
    public Matrix4x4 LocalToWorldDeviceTransformation(Matrix4x4 local)
    {
        return cachedTransform * local;
    }

    /// <summary>
    /// Extracts the position from a 4x4 transform matrix
    /// </summary>
    /// <param name="mat"></param>
    /// <returns></returns>
    public Vector3 GetPositionFromTransformMatrix(Matrix4x4 mat)
    {
        return mat.ExtractPosition();
    }

    public Quaternion GetRotationFromTransformMatrix(Matrix4x4 mat)
    {
        return mat.ExtractRotation();
    }

    /// <summary>
    /// Returns the current position and rotation (eulerAngles) directly from
    /// the Haptic Plugin API.
    /// </summary>
    /// <remarks>
    /// This is thread-safe and called by RealTimePositionTracker.
    /// </remarks>
    /// <returns></returns>
    public (Vector3 pos, Vector3 angles) GetPositionAndRotationFromAPI()
    {
        Vector3 pos;
        Quaternion rot;
        Vector3 angles;

        Matrix4x4 localTransform = GetDeviceTransformationRaw();

        Matrix4x4 worldTransform = LocalToWorldDeviceTransformation(localTransform);

        pos = GetPositionFromTransformMatrix(worldTransform);
        rot = GetRotationFromTransformMatrix(worldTransform);

        angles = rot.eulerAngles;

        return (pos, angles);
    }


    /// <summary>
    /// Gets the raw device transform directly from the API.
    /// </summary>
    /// <remarks>
    /// This is thread-safe and called by RealTimePositionTracker.
    /// </remarks>
    /// <returns></returns>
    public Matrix4x4 GetDeviceTransformationRaw()
    {
        double[] matInput = new double[16];
        float scaleFactor;

        HapticPlugin.GetTransformThreadSafe("Default Device", matInput);

        if (hapticPlugin.ScaleToMeter)
            scaleFactor = hapticPlugin.GlobalScale;
        else
            scaleFactor = 1.0f;

        for (int ii = 0; ii < 16; ii++)
            if (ii % 4 != 3)
                matInput[ii] *= scaleFactor;

        Matrix4x4 mat;
        mat.m00 = (float)matInput[0];
        mat.m01 = (float)matInput[1];
        mat.m02 = (float)matInput[2];
        mat.m03 = (float)matInput[3];
        mat.m10 = (float)matInput[4];
        mat.m11 = (float)matInput[5];
        mat.m12 = (float)matInput[6];
        mat.m13 = (float)matInput[7];
        mat.m20 = (float)matInput[8];
        mat.m21 = (float)matInput[9];
        mat.m22 = (float)matInput[10];
        mat.m23 = (float)matInput[11];
        mat.m30 = (float)matInput[12];
        mat.m31 = (float)matInput[13];
        mat.m32 = (float)matInput[14];
        mat.m33 = (float)matInput[15];

        return mat.transpose;
    }

    /// <summary>
    /// Returns a unit vector with the forward position of the stylus.
    /// </summary>
    /// <returns></returns>
    public Vector3 GetForward()
    {
        return -touchCollider.transform.forward;
    }

    public void DisableCollider()
    {
#if UNITY_EDITOR
        Debug.Log("[TouchManager] Disabled haptic collider.");
#endif
        touchCollider.GetComponent<SphereCollider>().enabled = false;
    }

    public void EnableCollider()
    {
#if UNITY_EDITOR
        Debug.Log("[TouchManager] Enabled haptic collider.");
#endif
        touchCollider.GetComponent<SphereCollider>().enabled = true;
    }

    public void SetColliderScale(float scale)
    {
#if UNITY_EDITOR
        Debug.Log($"[TouchManager] Set scale of haptic collider to {scale}.");
#endif

        touchCollider.transform.localScale = new Vector3(scale, scale, scale);
    }

    public void DisableColliderTemporarily(float seconds)
    {
        StartCoroutine(DisableColliderTemporarilyCoroutine(seconds));
    }

    private IEnumerator DisableColliderTemporarilyCoroutine(float seconds)
    {
#if UNITY_EDITOR
        Debug.Log("[TouchManager] Disabling haptic collider for " + seconds + " seconds.");
#endif

        DisableCollider();

        yield return new WaitForSeconds(seconds);

        EnableCollider();
    }

    public Vector3 GravityForceFromMass(float massKG)
    {
        return new Vector3(0.0f, -9.81f * massKG, 0.0f);
    }

    /// <summary>
    /// Sets a constant force equivalent to gravity.
    /// </summary>
    /// <param name="gravity"></param>
    public void SetGravityForce(Vector3 gravity)
    {
#if UNITY_EDITOR
        Debug.Log($"[TouchManager] Set gravity force to {gravity}.");
#endif

        var gravityArray = HapticPlugin.Vector3ToDoubleArray(gravity);

        HapticPlugin.SetGravityForceThreadSafe(hapticPlugin.DeviceIdentifier,
                                               gravityArray);
    }

    /// <summary>
    /// Moves the touch tip to a fixed position by a spring force
    /// (locks the position).
    /// </summary>
    /// <param name="position"></param>
    /// <param name="springForce"></param>
    public void MoveToFixedPositionWithSpring(Vector3 position, float springForce)
    {
#if UNITY_EDITOR
        Debug.Log($"[TouchManager] Set spring force to position {position} with force {springForce}.");
#endif

        if (springForce < 0 || springForce > 1)
        {
            Debug.LogError($"[TouchManager] Spring force not in [0, 1]: {springForce}.");
            return;
        }

        hapticPlugin.SpringGMag = springForce;
        hapticPlugin.SpringGDir = hapticPlugin.gameObject.transform.InverseTransformPoint(position) / hapticPlugin.GlobalScale;
        hapticPlugin.enable_GloablSpring = true;
    }

    public void DisableSpringForce()
    {
#if UNITY_EDITOR
        Debug.Log($"[TouchManager] Disable spring force.");
#endif

        hapticPlugin.enable_GloablSpring = false;
    }
    
    public bool IsConnected()
    {
        return hapticPlugin.DeviceIdentifier != "Not Connected";
    }
}