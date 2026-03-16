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
using UnityEngine.UI;

/// <summary>
/// Base class for managing the two-dimensional experiment-within-the-experiment.
/// </summary>
/// <remarks>
/// Right now, it is not used. Would be nice to refactor DotMotionManager as
/// inheriting from this class; right now we have some code duplication.
/// </remarks>
public class Experiment2DManager : MonoBehaviour
{
    public CanvasManager canvasManager;

    public GameObject textPrefab; // choose in the inspector
    public GameObject fixationCross; // choose in the inspector

    public void ShowFixationCross()
    {
        fixationCross.SetActive(true);
    }

    public void HideFixationCross()
    {
        fixationCross.SetActive(false);
    }

    public void SetFixationCrossColor(Color color)
    {
        var graphics = new List<Graphic>();

        Graphic parentGraphic = fixationCross.GetComponent<Graphic>();
        Graphic[] graphicsInChildren = fixationCross.GetComponentsInChildren<Graphic>();

        if (parentGraphic == null && graphicsInChildren.Length == 0)
            Debug.LogError($"[DrawableObject] GameObject {gameObject} or its children have no graphic component.");

        else
        {
            if (parentGraphic != null)
                graphics.Add(parentGraphic);
            if (graphicsInChildren.Length > 0)
                foreach (var graphicChild in graphicsInChildren)
                    graphics.Add(graphicChild);
        }

        foreach (var graphic in graphics)
            if (graphic)
                graphic.color = color;
    }
}

/// <summary>
/// Base class for trials of the two-dimensional
/// experiment-within-the-experiment.
/// </summary>
public abstract class Experiment2DTrial
{
    // Whether to show feedback after the trial.
    protected bool showFeedback;
    public bool ShowFeedback => showFeedback;

    // CanvasManager object to actually handle the drawing
    public CanvasManager canvasManager;

    public abstract void BeginTrial();
    public abstract void UpdateObjects();
    public abstract void EndTrial();
}


/// <summary>
/// A trial of a simple Flanker task.
/// </summary>
public class FlankerTrial : Experiment2DTrial
{
    public Vector2 coherentDirection;

    public bool congruent;

    // Logical size of the experiment. (0, 0) is the center and it expands in
    // each direction by +-experimentSize.
    // Does not need to match the canvasSize (e.g. scaled).
    public Vector2 experimentSize;

    public GameObject textPrefab;

    public int fontSize;
    public string text;

    public FlankerTrial(string direction,
                        bool congruent,
                        bool showFeedback,
                        Vector2 experimentSize,
                        CanvasManager canvasManager,
                        int fontSize,
                        GameObject textPrefab)
    {
        if (direction == "left")
            coherentDirection = Vector2.left;
        else if (direction == "right")
            coherentDirection = Vector2.right;
        else
        {
            Debug.LogError("[DotMotionManager] Invalid direction specified");
            coherentDirection = Vector2.zero;
        }

        this.congruent = congruent;
        this.showFeedback = showFeedback;
        this.experimentSize = experimentSize;
        this.canvasManager = canvasManager;
        this.fontSize = fontSize;
        this.textPrefab = textPrefab;

        SetupTrial();
    }

    public override void UpdateObjects() { }

    public void ShowText(string text, int fontSize)
    {
        var instructionText = new InstructionText(text,
                                                  textPrefab,
                                                  Vector2.zero,
                                                  Vector2.zero,
                                                  Color.black,
                                                  fontSize);

        canvasManager.AddElement(instructionText);
    }

    public void SetupTrial()
    {
        if (congruent && (coherentDirection == Vector2.left))
            text = "<<<<<<";

        else if (congruent && (coherentDirection == Vector2.right))
            text = ">>>>>>";

        else if (!congruent && (coherentDirection == Vector2.left))
            text = ">><<>>";

        else if (!congruent && (coherentDirection == Vector2.right))
            text = "<<>><<";
    }

    public override void BeginTrial()
    {
        canvasManager.ClearText();
        ShowText(text, fontSize);
    }

    public override void EndTrial() { }
}