// -----------------------------------------------------------------------------
// Copyright (C) 2026 Cognition, Action, and Sustainability Unit
// University of Freiburg, Department of Psychology
// Implementation: Paul Soelder
// Supervision: Dr. Andrea Kiesel, Dr. Irina Monno
// All rights reserved.
// 
// This file is part of an MIT-licensed project.
// Proprietary assets used at runtime are excluded from this license.
// Portions of this code are heavily inspired by the RDK plugin for JsPsych 
// (Copyright (c) 2017 Sivananda Rajananda; GPL-3). No GPL code is included; 
// this is an independent implementation.
// SPDX-License-Identifier: MIT
// -----------------------------------------------------------------------------

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Manages the dot motion task.
/// </summary>
/// <remarks>
/// Would be nice to refactor it as inheriting from Experiment2DManager.
/// </remarks>
public class DotMotionManager : MonoBehaviour
{
    // Logical size of the experiment. (0, 0) is the center and it expands in
    // each direction by +-experimentSize.
    // Does not need to match the canvasSize (e.g. scaled).
    private Vector2 experimentSize = new(200, 200);

    // Internal state variable.
    private bool running = false;
    public bool Running => running;

    // Currently running trial, if any.
    public Experiment2DTrial currentTrial;

    // Graphical parameters
    public GameObject dotPrefab; // choose a circle (or something else)
                                 // in the inspector

    public GameObject textPrefab; // choose in the inspector

    public GameObject overlay; // choose in the inspector

    public ObjectPoolManager dotObjectPool; // choose in the inspector

    // You could adjust the rendered colors here
    public Color colorRed = Color.red;
    public Color colorBlue = Color.blue;

    // CanvasManager object to actually handle the drawing
    public CanvasManager canvasManager;
    public SessionManager sessionManager;

    // Using Screen.DPI did not work as expected in the fullscreen windowed mode,
    // so we calculate the real DPI per hand.
    private float realDPI;

    void Start()
    {
        // Get real DPI from session manager
        realDPI = sessionManager.realDPI;
    }

    void Update()
    {
        if (running)
            currentTrial.UpdateObjects();
    }

    /// <summary>
    /// Begins the current trial. currentTrial must be assigned before calling.
    /// </summary>
    public void BeginTrial()
    {
        Debug.Log("[DotMotionManager] Starting dot motion task");

        // Some trial is already running -> stop with error
        if (running)
            Debug.LogError("[DotMotionManager] Attempted to start dot motion task while another task is already running.");

        currentTrial.BeginTrial();

        running = true;
    }

    /// <summary>
    /// Stop running the current trial (if any).
    /// </summary>
    public void EndTrial()
    {
        canvasManager.ClearCanvas();

        currentTrial.EndTrial();

        running = false;
        Debug.Log("[DotMotionManager] Ending dot motion task");
    }

    public void ShowFixationCross()
    {
        canvasManager.ShowFixationCross();
    }

    public void HideFixationCross()
    {
        canvasManager.HideFixationCross();
    }

    public void ClearText()
    {
        canvasManager.ClearText();
    }

    /// <summary>
    /// Creates and returns a dot motion trial with the desired properties
    /// for our experiment.
    /// </summary>
    /// <param name="direction"></param>
    /// <param name="motionCoherence"></param>
    /// <param name="showFeedback"></param>
    /// <returns></returns>
    /// <remarks>
    /// Is called at BeginTrial().
    /// </remarks>
    public DotMotionTrial SimpleTrial(string direction, float motionCoherence,
                                      bool showFeedback)
    {
        // The logical color of all dots.
        DotMotionDot.DotMotionColor dotMotionColor = DotMotionDot.
            DotMotionColor.Blue;

        Vector2 coherentDirection;

        if (direction == "left")
            coherentDirection = Vector2.left;
        else if (direction == "right")
            coherentDirection = Vector2.right;
        else
        {
            Debug.LogError("[DotMotionManager] Invalid direction specified");
            coherentDirection = Vector2.zero;
        }

        // Total number of dots.
        int numDots = 200;

        // Dot diameter in millimeters
        float dotDiameterMM = 1.5f;

        // Calculate diameter in pixel from display resolution
        float dotDiameterPixel =  dotDiameterMM * realDPI / 25.4f;

        // Dot speed in °/s
        float dotDegPerSecond = 1.5f;

        // Distance of participant to the screen in centimeters
        float distanceToScreenCM = 60f;

        float dotSpeed = DotMotionTrial.PixelSpeedFromAngularVelocity(
            dotDegPerSecond,
            distanceToScreenCM,
            realDPI);

        Debug.Log($"Dot speed is {dotSpeed}px/s, calculated from an angular " +
                  $" velocity of {dotDegPerSecond}°/s and distance to the " + 
                  $" screen of {distanceToScreenCM}cm");

        Debug.Log($"Dot diameter is {dotDiameterMM} mm. With {realDPI} DPI, " +
                  $"it results in {dotDiameterPixel} px.");

        var dotMotionTrial = new DotMotionTrial(
            DotMotionTrial.SignalSelectionRule.Same,
            DotMotionTrial.NoiseType.RandomDirection,
            DotMotionTrial.ApertureShape.Square,
            DotMotionTrial.OutOfBoundsDecision.RandomlyOpposite,
            coherentDirection,
            dotMotionColor,
            numDots,
            motionCoherence,
            dotSpeed,
            showFeedback,
            experimentSize,
            canvasManager,
            overlay,
            dotObjectPool,
            dotDiameterPixel,
            dotPrefab);

        return dotMotionTrial;
    }
}

/// <summary>
/// Implements the logic of the dot motion task.
/// </summary>
/// <remarks>
/// Portions of this code are heavily inspired by the RDK plugin for JsPsych 
/// (Copyright (c) 2017 Sivananda Rajananda). No GPL code is included; 
/// this is an independent implementation.
/// https://github.com/vrsivananda/RDK
/// </remarks>
public class DotMotionTrial : Experiment2DTrial
{
    // See https://doi.org/10.1016/0042-6989(95)00325-8 for selection rules
    // and noise types

    // Signal Selection rule:
    // - Same: Each dot is designated to be either a coherent dot (signal) or
    // incoherent dot (noise) and will remain so throughout all frames in the
    // display. Coherent dots will always move in the direction of coherent
    // motion in all frames.
    // - Different: Each dot can be either a coherent dot (signal) or incoherent
    // dot (noise) and will be designated randomly (weighted based on the
    // coherence level) at each frame. Only the dots that are designated to be
    // coherent dots will move in the direction of coherent motion, but only in
    // that frame. In the next frame, each dot will be designated randomly again
    // on whether it is a coherent or incoherent dot.
    public enum SignalSelectionRule
    {
        Same,
        Different
    }

    // Noise Type:
    // - Random position: The incoherent dots are in a random location in the
    // aperture in each frame
    // - Random walk: The incoherent dots will move in a random direction
    // (designated randomly in each frame) in each frame.
    // - Random direction: Each incoherent dot has its own alternative direction
    // of motion (designated randomly at the beginning of the trial), and moves
    // in that direction in each frame.
    public enum NoiseType
    {
        RandomPosition,
        RandomWalk,
        RandomDirection
    }

    public enum ApertureShape
    {
        Circle,
        Ellipse,
        Square,
        Rectangle
    }

    // How we reinsert a dot that has moved outside the edges of the aperture:
    // Randomly anywhere: Randomly appear anywhere in the aperture
    // Randomly opposite: Appear on the opposite edge of the aperture (Random if
    // square or rectangle, reflected about origin in circle and ellipse)
    public enum OutOfBoundsDecision
    {
        RandomlyAnywhere,
        RandomlyOpposite
    }

    // Not yet implemented: color discrimination.

    // Others are not yet implemented
    public SignalSelectionRule signalSelectionRule = SignalSelectionRule.Same;
    public NoiseType noiseType = NoiseType.RandomDirection;
    public ApertureShape apertureShape = ApertureShape.Square;
    public OutOfBoundsDecision outOfBoundsDecision = OutOfBoundsDecision.
        RandomlyOpposite;


    // The coherent direction of dot motion.
    public Vector2 coherentDirection;

    // The coherent color of the dots.
    public DotMotionDot.DotMotionColor coherentColor;

    // Total number of dots.
    public int numDots;

    // Fraction of dots moving in the coherent direction as opposed to random.
    public float motionCoherence;

    // Speed of the dots
    public float speed;

    // Logical size of the experiment. (0, 0) is the center and it expands in
    // each direction by +-experimentSize.
    // Does not need to match the canvasSize (e.g. scaled).
    public Vector2 experimentSize;

    // Lists of all current dots.
    public List<DotMotionDot> dots = new();

    // Graphical parameters
    public GameObject dotPrefab; // choose a circle (or something else)
                                 // in the inspector
                                 // Scale the prefab
    
    // Dot diameter in pixels
    public float dotDiameterPixel;

    // Similar to a canvas, but the Unity UI is very costly => for the dots
    // its much more efficient to implement them as SpriteRenderer.
    // The overlay is just an empty container for the SpriteRenderers.
    public GameObject overlay;

    public ObjectPoolManager dotObjectPool;

    // You could adjust the rendered colors here
    public Color colorRed;
    public Color colorBlue;

    public DotMotionTrial(SignalSelectionRule signalSelectionRule,
                          NoiseType noiseType,
                          ApertureShape apertureShape,
                          OutOfBoundsDecision outOfBoundsDecision,
                          Vector2 coherentDirection,
                          DotMotionDot.DotMotionColor coherentColor,
                          int numDots,
                          float motionCoherence,
                          float speed,
                          bool showFeedback,
                          Vector2 experimentSize,
                          CanvasManager canvasManager,
                          GameObject overlay,
                          ObjectPoolManager dotObjectPool,
                          float dotDiameterPixel,
                          GameObject dotPrefab)
    {
        this.signalSelectionRule = signalSelectionRule;
        this.noiseType = noiseType;
        this.apertureShape = apertureShape;
        this.outOfBoundsDecision = outOfBoundsDecision;
        this.coherentDirection = coherentDirection;
        this.coherentColor = coherentColor;
        this.numDots = numDots;
        this.motionCoherence = motionCoherence;
        this.speed = speed;
        this.showFeedback = showFeedback;
        this.experimentSize = experimentSize;
        this.canvasManager = canvasManager;
        this.overlay = overlay;
        this.dotObjectPool = dotObjectPool;
        this.dotDiameterPixel = dotDiameterPixel;
        this.dotPrefab = dotPrefab;

        SetupTrial();
    }

    /// <summary>
    /// Generates the dot motion trial according to the specified rules.
    /// </summary>
    public void SetupTrial()
    {
        if (signalSelectionRule != SignalSelectionRule.Same &&
           noiseType != NoiseType.RandomDirection)
        {
            Debug.LogError("[DotMotionManager] This selection rule or noise "
                           + " type is not yet implemented.");
        }

        // The following assumes SignalSelectionRule.Same and
        // NoiseType.RandomDirection.

        int numCoherentDots = (int)Mathf.Ceil(numDots * motionCoherence);

        // Generate the dots
        for (int i = 0; i < numDots; i++)
        {
            Vector2 position = RandomPosition();

            // We need numCoherentDots coherent dots, the rest has random
            // direction
            Vector2 direction = (i < numCoherentDots) ?
                coherentDirection : RandomDirection();

            DotMotionDot dot = new(
                position,
                direction,
                speed,
                coherentColor,
                dotPrefab,
                dotDiameterPixel
            );

            dots.Add(dot);
        }
    }

    /// <summary>
    /// Returns a random position within the experiment size.
    /// </summary>
    /// <returns></returns>
    public Vector2 RandomPosition()
    {
        float x = Random.Range(-experimentSize.x, experimentSize.x);
        float y = Random.Range(-experimentSize.y, experimentSize.y);

        return new Vector2(x, y);
    }

    /// <summary>
    /// Checks boundaries according to the RandomlyOpposite rule. Returns new
    /// positions.
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    public (Vector2 pos, bool outOfBounds) RandomlyOpposite(Vector2 position)
    {
        bool outOfBounds = false;

        if (position.x > experimentSize.x)
        {
            position.x = position.x - 2 * experimentSize.x;
            position.y = Random.Range(-experimentSize.y, experimentSize.y);
            outOfBounds = true;
        }
        else if (position.x < -experimentSize.x)
        {
            position.x = position.x + 2 * experimentSize.x;
            position.y = Random.Range(-experimentSize.y, experimentSize.y);
            outOfBounds = true;
        }

        if (position.y > experimentSize.y)
        {
            position.y = position.y - 2 * experimentSize.y;
            position.x = Random.Range(-experimentSize.x, experimentSize.x);
            outOfBounds = true;
        }
        else if (position.y < -experimentSize.y)
        {
            position.y = position.y + 2 * experimentSize.y;
            position.x = Random.Range(-experimentSize.x, experimentSize.x);
            outOfBounds = true;
        }

        return (position, outOfBounds);
    }

    /// <summary>
    /// Returns a random direction vector of length one.
    /// </summary>
    /// <returns></returns>
    public Vector2 RandomDirection()
    {
        return Random.insideUnitCircle.normalized;
    }

    /// <summary>
    /// Converts speeds from °/s to px/s depending on distance to the screen.
    /// This is an approximation, assuming dots are close to fixation point.
    /// </summary>
    /// <param name="degPerSecond"></param>
    /// <param name="distanceCM"></param>
    /// <param name="dpi"></param>
    /// <returns></returns>
    public static float PixelSpeedFromAngularVelocity(float degPerSecond,
                                                      float distanceCM,
                                                      float dpi)
    {
        // Exact formula: degPerSecond * mathf.Deg2Rad * distanceCM *
        // (1 + (distance_of_point_to_center / distanceCM))
        // which depends on the dots position.
        // But error is quite small, 2.8% for 10cm
        float cmPerSecond = degPerSecond * Mathf.Deg2Rad * distanceCM;

        // Pixels per second depend on screen resolution
        return cmPerSecond * dpi / 2.54f;
    }
    
    /// <summary>
    /// Prepare the canvas for the next trial: adds dots and hides them.
    /// </summary>
    /// <remarks>
    /// This is to prevent lag spikes.
    /// </remarks>
    public void PrepareCanvas()
    {
        Debug.Log("[DotMotionTrial] Prepare canvas");

        Transform parentTransform = overlay.transform;

        foreach (var dot in dots)
        {
            dot.Spawn(parentTransform, dotObjectPool);
            dot.Hide();
        }
    }

    public override void BeginTrial()
    {
        canvasManager.ClearText();

        foreach (var dot in dots)
            dot.Show();
    }

    /// <summary>
    /// Update the position of all dots. To be called on a frame-by-frame basis.
    /// </summary>
    /// <remarks>
    /// This is not a MonoBehavior so it's called manually by DotMotionManager
    /// </remarks>
    public override void UpdateObjects()
    {
        foreach (var dot in dots)
        {
            dot.UpdatePosition();
            CheckBoundariesAndUpdate(dot);
        }
    }

    /// <summary>
    /// Checks the boundaries of a dot according to the selected rule.
    /// Updates its position accordingly. Returns true iff the dot is
    /// out of bounds.
    /// </summary>
    /// <param name="dot"></param>
    /// <returns></returns>
    public bool CheckBoundariesAndUpdate(DotMotionDot dot)
    {
        bool outOfBounds = false;

        if (outOfBoundsDecision == OutOfBoundsDecision.RandomlyOpposite)
        {
            var (newPosition, wasOutOfBounds) = RandomlyOpposite(dot.position);
            outOfBounds = wasOutOfBounds;

            dot.SetPosition(newPosition);
        }
        return outOfBounds;
    }

    /// <summary>
    /// Checks if the object is out of bounds on the next frame.
    /// </summary>
    /// <param name="dot"></param>
    /// <returns></returns>
    /// <remarks>
    /// Mainly for debug purposes of flickering on the first frame.
    /// </remarks>
    public bool NextFrameIsOutOfBounds(DotMotionDot dot)
    {
        bool outOfBounds = false;

        if (outOfBoundsDecision == OutOfBoundsDecision.RandomlyOpposite)
        {
            var (newPosition, wasOutOfBounds) = RandomlyOpposite(
                dot.position + dot.speed * Time.deltaTime * dot.direction
            );

            outOfBounds = wasOutOfBounds;
        }
        return outOfBounds;
    }
    
    public override void EndTrial()
    {
        foreach(var dot in dots)
            dot.Despawn(dotObjectPool);
    }
}

/// <summary>
/// A class to represent a single dot of the dot motion task.
/// </summary>
public class DotMotionDot : Stimulus
{
    // Custom datatype to abstractly represent the colors in the dot motion task,
    // independent of the actual rendering details.
    public enum DotMotionColor
    {
        Red,
        Blue
    }

    // You could adjust the rendered colors here
    public static Color colorRed = Color.red;
    public static Color colorBlue = Color.blue;

    // The logical color variable (red or blue), not the actual color for rendering.
    public DotMotionColor dotMotionColor;

    public DotMotionDot(Vector2 position,
                        Vector2 direction,
                        float speed,
                        DotMotionColor dotMotionColor,
                        GameObject prefab,
                        float diameterPixel)
        : base(position, direction, speed, prefab,
               1, dotMotionColor == DotMotionColor.Red ? colorRed : colorBlue)
    {
        this.dotMotionColor = dotMotionColor;

        // Prefabs are 1x1
        this.scale = diameterPixel;
    }
}