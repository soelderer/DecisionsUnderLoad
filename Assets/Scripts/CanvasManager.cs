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
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// An element of a two-dimensional experiment that can be displayed on
/// a canvas or as a sprite renderer, e.g. a fixation cross or a stimulus.
/// </summary>
/// <remarks>
/// Also holds its instance of a GameObject, if it is instantiated. This way,
/// we can control the logical variables (e.g. logical position) as well as the
/// instance.
/// </remarks>
public class CanvasElement
{
    public Vector2 position;
    // TODO: Could be extend to incorporate rotation.
    // public Vector2 rotation;

    public Vector2 direction;
    public float speed;

    // protected bool instantiated = false;
    protected bool instantiated;
    public bool IsInstantiated => instantiated;

    protected bool visible = true;
    public bool IsVisible => visible;

    // Whether the position should be updated if the object is invisible.
    protected bool movesWhenHidden = false;
    public bool MovesWhenHidden => movesWhenHidden;

    // Graphics-related member variables.
    public GameObject prefab;

    public GameObject instance;
    private Transform transform;
    private RectTransform rectTransform;

    public float scale;
    public List<Graphic> graphics;
    public List<SpriteRenderer> sprites;

    public Color color;

    public CanvasElement(Vector2 position, Vector2 direction, float speed,
                              GameObject prefab, float scale, Color color)
    {
        this.position = position;
        this.direction = direction;
        this.speed = speed;
        this.prefab = prefab;
        this.scale = scale;
        this.color = color;
    }

    public void SetPosition(Vector2 position)
    {
        this.position = position;

        if (instantiated)
            SetPositionOfInstance();
    }

    public void UpdatePosition()
    {
        if (visible || (!visible && movesWhenHidden))
            SetPosition(position + speed * Time.deltaTime * direction);
    }

    public void SetScale(float scale)
    {
        this.scale = scale;

        if (instantiated)
            SetScaleOfInstance();
    }

    public void SetColor(Color color)
    {
        this.color = color;

        if (instantiated)
            SetColorOfInstance();
    }

    public void Show()
    {
        visible = true;

        if (instantiated)
            ShowInstance();
    }

    public void Hide()
    {
        visible = false;

        if (instantiated)
            HideInstance();
    }

    protected void SetPositionOfInstance()
    {
        if (rectTransform)
            rectTransform.anchoredPosition = position;

        else
            transform.localPosition = position;
    }

    protected void SetScaleOfInstance()
    {
        if (rectTransform)
            rectTransform.localScale = new Vector3(scale, scale, scale);

        else
            transform.localScale = new Vector3(scale, scale, scale);
    }

    protected void SetColorOfInstance()
    {
        foreach (var graphic in graphics)
            if (graphic) graphic.color = color;

        foreach (var sprite in sprites)
            if (sprite) sprite.color = color;
    }

    protected void ShowInstance()
    {
        instance.SetActive(true);
    }

    protected void HideInstance()
    {
        instance.SetActive(false);
    }

    protected void GetGraphics()
    {
        graphics = instance.GetComponentsInChildren<Graphic>().ToList(); // includes GO itself
        sprites = instance.GetComponentsInChildren<SpriteRenderer>().ToList(); // includes GO itself
    }

    /// <summary>
    /// Spawns an instance of the prefab without object pooling.
    /// </summary>
    /// <param name="parentTransform"></param>
    public virtual void Spawn(Transform parentTransform)
    {
        instance = Object.Instantiate(prefab);

        rectTransform = instance.GetComponent<RectTransform>();
        transform = instance.transform;

        // Set the parent of the GameObject to the canvas
        transform.SetParent(parentTransform, false);
        // 'false' ensures we don't change local position

        if (!visible)
            HideInstance();

        GetGraphics();

        SetPositionOfInstance();
        SetScaleOfInstance();
        SetColorOfInstance();

        instantiated = true;
    }
    
    /// <summary>
    /// Spawns an instance of the prefab with object pooling
    /// </summary>
    /// <param name="parentTransform"></param>
    /// <param name="poolManager"></param>
    /// <remarks>See ObjectPoolManager.cs for details</remarks>
    public virtual void Spawn(Transform parentTransform, ObjectPoolManager poolManager)
    {
        instance = poolManager.Get();

        rectTransform = instance.GetComponent<RectTransform>();
        transform = instance.transform;

        // Set the parent of the GameObject to the canvas
        transform.SetParent(parentTransform, false);
        // 'false' ensures we don't change local position

        if (!visible)
            HideInstance();

        GetGraphics();

        SetPositionOfInstance();
        SetScaleOfInstance();
        SetColorOfInstance();

        instantiated = true;
    }

    /// <summary>
    /// Despawns the instance without object pooling.
    /// </summary>
    public virtual void Despawn()
    {
        Object.Destroy(instance);

        instantiated = false;
    }

    /// <summary>
    /// Despawns the instance by returning it to its object pool.
    /// </summary>
    /// <param name="poolManager"></param>
    public virtual void Despawn(ObjectPoolManager poolManager)
    {
        poolManager.Release(instance);

        instantiated = false;
    }
}

/// <summary>
/// Instructions text for on-canvas instructions (opposed to the between-blocks
/// paneled instructions).
/// </summary>
public class InstructionText : CanvasElement
{
    public string text;
    public int fontSize;

    public InstructionText(string text,
                           GameObject prefab,
                           Vector2 position,
                           Vector2 direction,
                           Color color,
                           int fontSize = 18,
                           float speed = 0f,
                           float scale = 1f)
        : base (position, direction, speed, prefab, scale, color)
    {
        this.text = text;
        this.fontSize = fontSize;
    }

    public void SetText(string text)
    {
        if (text == null)
            return;

        this.text = text;

        if (instantiated)
            SetTextOfInstance();
    }

    public void SetFontSize(int fontSize)
    {
        this.fontSize = fontSize;

        if (instantiated)
            SetFontSizeOfInstance();
    }

    private void SetTextOfInstance()
    {
        if (instance.TryGetComponent<Text>(out var textComponent))
        {
            textComponent.text = text;
            textComponent.fontSize = fontSize;
        }
    }

    private void SetFontSizeOfInstance()
    {
        if (instance.TryGetComponent<Text>(out var textComponent))
        {
            textComponent.text = text;
            textComponent.fontSize = fontSize;
        }
    }

    public override void Spawn(Transform parentTransform)
    {
        base.Spawn(parentTransform);

        SetTextOfInstance();
        SetFontSizeOfInstance();
    }
}

/// <summary>
/// A fixation cross.
/// </summary>
/// <remarks>
/// Sub-classes for type-checking. Could be extended.
/// </remarks>
public class FixationCross : CanvasElement
{
    public FixationCross(Vector2 position, Vector2 direction, float speed,
                          GameObject prefab, float scale, Color color)
        : base(position, direction, speed, prefab, scale, color) {}
}

/// <summary>
/// A two-dimensional stimulus.
/// </summary>
/// <remarks>
/// Sub-classes for type-checking. Could be extended.
/// </remarks>
public class Stimulus : CanvasElement
{
    public Stimulus(Vector2 position, Vector2 direction, float speed,
                    GameObject prefab, float scale, Color color)
        : base(position, direction, speed, prefab, scale, color) {}
}

/// <summary>
/// Manages all the CanvasElements attached to it, e.g. updates their positions.
/// </summary>
public class CanvasManager : MonoBehaviour
{
    // Size of the canvas. (0, 0) is the center and it expands in each direction
    // by +-canvasSize.
    private Vector2 canvasSize = new(200, 200);

    private Canvas canvas;

    public bool autoUpdatePositions = true;

    // For identification in logs
    public string canvasName;

    // Holds all the canvas elements attached to it. Keeping these separate
    // lists allows us to have methods like ClearText().
    protected List<CanvasElement> canvasElements = new();
    protected List<FixationCross> fixationCrosses = new();
    protected List<InstructionText> instructionTexts = new();
    protected List<Stimulus> stimuli = new();

    // Graphics-related fields
    public GameObject fixationCrossPrefab; // choose in the inspector

    void Start()
    {
        // Script is attached to the "Background" child => get in Parent
        canvas = GetComponentInParent<Canvas>();

        if (canvas == null)
            Debug.LogError($"[CanvasManager][{gameObject.name}] Failed to get component <Canvas>");
            
#if UNITY_EDITOR
        Debug.Log($"[CanvasManager][{canvasName}] Canvas size is {canvasSize}");
#endif
    }

    void Update()
    {
        if (autoUpdatePositions)
        {
            foreach (var element in fixationCrosses)
                element.UpdatePosition();

            foreach (var element in instructionTexts)
                element.UpdatePosition();

            foreach (var element in stimuli)
                element.UpdatePosition();
        }
    }

    /// <summary>
    /// Instantiates a CanvasElement with this Canvas as parent and adds it to
    /// the list of elements. Returns a handle on the instantiated element.
    /// </summary>
    /// <param name="canvasElement"></param>
    /// <returns></returns>
    public CanvasElement AddElement(CanvasElement canvasElement)
    {
        // Instantiate
        if (canvasElement.IsInstantiated)
        {
            Debug.LogError($"[CanvasManager][{canvasName}] Error in AddElement: canvasElement is already instantiated.");
            return canvasElement;
        }

        // Spawn with canvas as parent
        canvasElement.Spawn(gameObject.transform);

        // Add to list
        canvasElements.Add(canvasElement);

        if (canvasElement is FixationCross cross)
            fixationCrosses.Add(cross);
        else if (canvasElement is InstructionText instructionText)
            instructionTexts.Add(instructionText);
        else if (canvasElement is Stimulus stimulus)
            stimuli.Add(stimulus);

        return canvasElement;
    }

    /// <summary>
    /// Adds CanvasElements and hides them immediately.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="elements"></param>
    /// <returns></returns>
    /// <remarks>This helps to prevent lag spikes: we can prepare the canvas
    /// in advance ("showing" is cheaper than instantiating).
    /// </remarks>
    public List<T> AddElementsHidden<T>(List<T> elements) where T : CanvasElement
    {
#if UNITY_EDITOR
        Debug.Log($"[CanvasManager][{canvasName}] AddElementsHidden with " + elements.Count + " elements");
#endif

        foreach (var element in elements)
        {
            AddElement(element);
            element.Hide();
        }

        return elements;
    }

    public void RemoveElement(CanvasElement canvasElement)
    {
        if (canvasElements.Contains(canvasElement))
        {
            canvasElement.Despawn();

            canvasElements.Remove(canvasElement);

            if (canvasElement is FixationCross cross)
                fixationCrosses.Remove(cross);
            else if (canvasElement is InstructionText instructionText)
                instructionTexts.Remove(instructionText);
            else if (canvasElement is Stimulus stimulus)
                stimuli.Remove(stimulus);
        }
    }

    /// <summary>
    /// Clear canvas (remove all objects).
    /// </summary>
    public void ClearCanvas()
    {
#if UNITY_EDITOR
        Debug.Log($"[CanvasManager][{canvasName}] Clearing canvas");
#endif

        // We need to iterate backwards because RemoveElement() removes the
        // items from the list that we are iterating over. If we iterate backwards
        // this should not be a problem.
        for (int i = canvasElements.Count - 1; i >= 0; i--)
            RemoveElement(canvasElements[i]);
    }

    /// <summary>
    /// Clear text (remove only text objects)
    /// </summary>
    public void ClearText()
    {
#if UNITY_EDITOR
        Debug.Log($"[CanvasManager][{canvasName}] Clearing text");
#endif

        // We need to iterate backwards because RemoveElement() removes the
        // items from the list that we are iterating over. If we iterate backwards
        // this should not be a problem.
        for (int i = instructionTexts.Count - 1; i >= 0; i--)
            RemoveElement(instructionTexts[i]);
    }

    public void ShowFixationCross()
    {
#if UNITY_EDITOR
        Debug.Log($"[CanvasManager][{canvasName}] Showing fixation cross");
#endif

        if (fixationCrosses.Count == 0)
        {
#if UNITY_EDITOR
            Debug.Log($"[CanvasManager][{canvasName}] Creating new fixation cross");
#endif

            float scale = 1f;

            // Different scaling for the two canvases to make them appear the
            // same size: override for the dot motion canvas
            if (canvasName == "DotMotionCanvas")
                scale = 3150f;  // this was tuned by eyeballing to have approx.
                                // the same size as the other cross

                FixationCross fixationCross = new(Vector2.zero, Vector2.zero, 0,
                fixationCrossPrefab, scale, Color.black);

            AddElement(fixationCross);
        }
        else
            foreach (var cross in fixationCrosses)
                cross.Show();
    }

    public void HideFixationCross()
    {
#if UNITY_EDITOR
        Debug.Log($"[CanvasManager][{canvasName}] Hiding fixation cross");
#endif

        if (fixationCrosses.Count == 0)
            return;

        else
            foreach (var cross in fixationCrosses)
                cross.Hide();
    }

    public void SetFixationCrossColor(Color color)
    {
        if (fixationCrosses.Count == 0)
            return;

        else
            foreach (var cross in fixationCrosses)
                cross.SetColor(color);
    }

    public void ShowCanvas()
    {
        canvas.enabled = true;

#if UNITY_EDITOR
        Debug.Log($"[CanvasManager][{canvasName}] Showing canvas");
#endif
    }

    public void HideCanvas()
    {
        canvas.enabled = false;

#if UNITY_EDITOR
        Debug.Log($"[CanvasManager][{canvasName}] Hiding canvas");
#endif
    }
}
