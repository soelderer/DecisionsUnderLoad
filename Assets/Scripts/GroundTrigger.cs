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
using UXF;

/// <summary>
/// Handles object collisions with ground objects.
/// </summary>
/// <remarks>
/// Attach this to the boxes and ground planes.
/// </remarks>
public class GroundTrigger : MonoBehaviour
{
    public TrialManager trialManager;
    public string surfaceType;

    public Material surfaceMaterial;

    public Color glowColor = Color.yellow;
    public float glowIntensity = 0.7f;
    public float glowDuration = 0.4f;
    public Color originalEmissionColor;

    void Start()
    {
        surfaceMaterial = gameObject.GetComponent<Renderer>().material;

        originalEmissionColor = surfaceMaterial.GetColor("_EmissionColor");

        // Ensure the material uses emission by enabling the keyword
        surfaceMaterial.EnableKeyword("_EMISSION");
    }

    /// <summary>
    /// Handles object collisions with ground objects.
    /// </summary>
    /// <param name="other"></param>
    void OnTriggerEnter(Collider other)
    {
        if (string.IsNullOrEmpty(surfaceType))
            Debug.LogError($"[GroundTrigger] surfaceType not set on {gameObject.name}");

        if (trialManager == null)
            Debug.LogError($"[GroundTrigger] trialManager not assigned on {gameObject.name}");

        if (!Session.instance.InTrial)
        {
#if UNITY_EDITOR
            Debug.Log($"[GroundTrigger] Something hit a surface, but " +
                            "we're not in a trial. Ignoring");
#endif
            return;
        }

        if (other.CompareTag("Ball"))
        {
#if UNITY_EDITOR
            Debug.Log($"[GroundTrigger] A ball hit a surface: {other.gameObject.name} at" +
                      $" position {other.gameObject.transform.position}");
#endif

            StartCoroutine(GlowEffect());

            trialManager.EndTrial(surfaceType);
        }

        else if (other.CompareTag("TouchCollider") && trialManager.ballGrabbed)
        {
#if UNITY_EDITOR
            Debug.Log($"[GroundTrigger] Haptic collider hit a surface: {other.gameObject.name} at" +
                      $" position {other.gameObject.transform.position}");
#endif

            StartCoroutine(GlowEffect());

            trialManager.ReleaseBall();
            trialManager.EndTrial(surfaceType);
        }

        // In practice trials, we want glow effect if the actor touches the surfaces.
        else if (Session.instance.CurrentBlock.settings.GetString("trial_type") == "practice" &&
                 other.CompareTag("TouchCollider"))
        {
#if UNITY_EDITOR
            Debug.Log($"[GroundTrigger] Touch actor hit a surface (practice trial).");
#endif
            StartCoroutine(GlowEffect());
        }

        else
        {
#if UNITY_EDITOR
            Debug.Log($"[GroundTrigger] Something hit a surface: {other.gameObject.name}");
#endif
        }
    }

    IEnumerator GlowEffect()
    {
        surfaceMaterial.SetColor("_EmissionColor", glowColor * glowIntensity);

        yield return StartCoroutine(FadeEmission(originalEmissionColor,
                                                 glowDuration));

        surfaceMaterial.SetColor("_EmissionColor", originalEmissionColor);
    }

    IEnumerator FadeEmission(Color targetColor, float duration)
    {
        Color startColor = surfaceMaterial.GetColor("_EmissionColor");

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;  // Progress ratio between 0 and 1
            surfaceMaterial.SetColor("_EmissionColor", Color.Lerp(startColor,
                targetColor, t));
            yield return null;  // Wait for the next frame
        }

        // Ensure the final color is exactly the target color
        surfaceMaterial.SetColor("_EmissionColor", targetColor);
    }
}
