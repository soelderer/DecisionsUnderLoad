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
using System.Globalization;
using System.Linq;

/// <summary>
/// Manages the flow and logic of the individual trials.
/// </summary>
public class TrialManager : MonoBehaviour
{
    // *************************************************************************
    // Game Objects that need to be assigned in inspector
    // *************************************************************************
    public GameObject dummyBallPrefab;

    public GameObject trayLeft;
    public GameObject trayRight;

    // Where to spawn the ball
    public Transform ballSpawnPoint;

    // Reference to the Haptic Direct plugin (assign the
    // HapticActor_DefaultDevice in inspector)
    public HapticPlugin hapticPlugin;

    // Reference to the touch manager (assign in inspector)
    public TouchManager touchManager;

    // Reference to the dot motion task (assign in inspector)
    public DotMotionManager dotMotionManager;

    // Reference to the task in control of the in-game canvas
    public Experiment2DManager experiment2DManager;

    // Reference to the renderer of the hotwire
    public TubeRenderer tubeRenderer;

    // Reference to trajectory 3 and 4 (these are made by balls and not
    // the TubeRenderer)
    public GameObject trajectory3, trajectory4;

    // Reference to the position tracker
    public RealTimePositionTracker realTimePositionTracker;
    
    // Reference to the block manager
    public BlockManager blockManager;

    // Reference to the LSL backend
    public LSLSyncBackend lslBackend;

    // Some debug object(s)
    public GameObject debugGO1;
    public GameObject debugGO2;
    public GameObject debugGO3;

    // *************************************************************************
    // State variables
    // *************************************************************************
    public bool ballGrabbed;

    // State variables for button handlers
    public bool waitingForButton1Click;
    public bool waitingForContinueButtonClickWithinTrial;
    public bool waitingForContinueButtonClickInMenu;

    public bool monitorProximityToFixationCross = false;
    public bool monitorProximityToTrajectory = false;

    public bool beginDotMotionCoroutineShouldStop = false;
    public int beginDotMotionCoroutineCounter = 0;

    public double startTime;
    public double grabTime;
    public double dotMotionStartTime;

    // *************************************************************************
    // Other fields
    // *************************************************************************
    // The ball, once it's spawned
    public GameObject ball;

    // To save events in the continuous logging
    public string eventForTracker = "";

    // *************************************************************************
    // Methods
    // *************************************************************************
    
    public void BeginTrial(Trial trial)
    {
        Debug.Log($"[TrialManager] Begin trial no. {trial.number}");

        beginDotMotionCoroutineShouldStop = false;

        if (dummyBallPrefab == null)
            Debug.LogError($"[TrialManager] dummyBallPrefab not assigned");

        if (ballSpawnPoint == null)
            Debug.LogError($"[TrialManager] ballSpawnPoint not set");

        ballGrabbed = false;

        // Practice, experimental or debug trial?
        if (Session.instance.CurrentBlock.settings.GetString("trial_type") == "practice")
            BeginPracticeTrial(trial);
        else if (Session.instance.CurrentBlock.settings.GetString("trial_type") == "experimental")
            BeginExperimentalTrial(trial);
        else if (Session.instance.CurrentBlock.settings.GetString("trial_type") == "debug")
            BeginDebugTrial(trial);
    }

    /// <summary>
    /// Begins a practice trial.
    /// </summary>
    /// <param name="trial"></param>
    public void BeginPracticeTrial(Trial trial)
    {
        // Change to 0 for faster skipping while debugging
        int shortMinimumViewingTime = 30;
        int longMinimumViewingTime = 30;

        var (pos, angles) = touchManager.GetPositionAndRotationFromAPI();

#if UNITY_EDITOR
        Debug.Log($"[TrialManager] Begin practice trial no. {trial.number}");
#endif

        startTime = Time.realtimeSinceStartupAsDouble;

        string practice_type = trial.settings.GetString("practice_type");

        // First practice trial is for learning to control the haptic touch
        // device (moving around).

        if (practice_type == "touch_movement")
        {
            HideTrays();

            ShowTextHeadsUp($@"Nimm den Stift aus der Fassung und halte ihn wie einen Bleistift.

Bewege ihn langsam nach rechts und links sowie nach oben und unten.

Beobachte dabei, wie sich der Stift am Bildschirm bewegt.

Nutze diese Zeit, um dich mit der Stiftbedienung vertraut zu machen.

Nach frühestens {longMinimumViewingTime} Sekunden kannst du mit dem dunkelgrauen Button am Stift
zur nächsten Aufgabe fortfahren.

Benutze deinen Zeigefinger, um den Button zu bedienen.",
                fontSize: 28);

            StartMinimumViewingTime(longMinimumViewingTime);
        }

        else if (practice_type == "touch_movement2")
        {
            HideTrays();

            ShowTextHeadsUp($@"Versuche, die gesamte Reichweite auszunutzen, um ein Gefühl
für die Bewegung zu bekommen.

Finde dabei heraus, wie weit du den Stift bewegen kannst.

Du kannst erst nach mindestens {shortMinimumViewingTime} Sekunden mit dem
dunkelgrauen Button am Stift fortfahren.

Benutze deinen Zeigefinger, um den Button zu bedienen.",
                fontSize: 28);

            StartMinimumViewingTime(shortMinimumViewingTime);
        }

        else if (practice_type == "touch_movement3")
        {
            HideTrays();

            ShowTextHeadsUp($@"Drehe den Stift aus dem Handgelenk so, dass die
Stiftspitze mal nach rechts und mal nach links zeigt.

Beobachte, wie sich der Stift am Bildschirm bewegt.

Du kannst erst nach mindestens {shortMinimumViewingTime} Sekunden mit dem
dunkelgrauen Button am Stift fortfahren.

Benutze deinen Zeigefinger, um den Button zu bedienen.",
                fontSize: 28);

            StartMinimumViewingTime(shortMinimumViewingTime);
        }

        else if (practice_type == "touch_movement4")
        {
            ShowTextHeadsUp($@"Versuche, das schwarze Kreuz zu berühren.

Das Kreuz wird blau, wenn du nahe genug dran bist.

Berühre drei Mal das Kreuz, sodass es blau wird.

Du kannst erst nach mindestens {shortMinimumViewingTime} Sekunden mit dem
dunkelgrauen Button am Stift fortfahren.

Benutze deinen Zeigefinger, um den Button zu bedienen.",
                fontSize: 28);

            StartMinimumViewingTime(shortMinimumViewingTime);

            ShowFixationCross();
            StartProximityToCrossFeedback();
        }


        // Realistic practice block
        else if (practice_type == "follow_trajectory")
        {
            ShowTextHeadsUp(@"Bewege den Stift zum schwarzen Kreuz, bis es blau wird.

Drücke und halte dann den dunkelgrauen Button.");

            waitingForContinueButtonClickWithinTrial = false;

            touchManager.Enable();

            StartProximityToTrajectory();

            BeginExperimentalTrial(trial);
        }

        // Realistic practice block
        else if (practice_type == "easy")
        {
            ShowTextHeadsUp(@"Bewege den Stift zum schwarzen Kreuz, bis es blau wird.

Drücke und halte dann den dunkelgrauen Button.");

            waitingForContinueButtonClickWithinTrial = false;

            touchManager.Enable();

            StartProximityToTrajectory();

            BeginExperimentalTrial(trial);
        }
    }

    /// <summary>
    /// Begins a dummy trial for debug purposes, e.g. to test some objects etc.
    /// </summary>
    /// <param name="trial"></param>
    public void BeginDebugTrial(Trial trial)
    {
#if UNITY_EDITOR
        Debug.Log($"[TrialManager] Begin debug trial no. {trial.number}");
#endif
    }

    public IEnumerator ResetDebugBalls(GameObject ball1, GameObject ball2)
    {
        while (true)
        {
            yield return new WaitForSeconds(3);
            ball1.GetComponent<Rigidbody>().MovePosition(new Vector3(0, 0.0616f, 0));
            ball2.transform.position = new Vector3(0, 0.0616f, -0.04f);
        }
    }

    /// <summary>
    /// Begins a proper experimental trial.
    /// </summary>
    /// <param name="trial"></param>
    public void BeginExperimentalTrial(Trial trial)
    {
        ShowTrays();

        int blockNum = trial.block.number;

        // Get trial and block settings (=IV)
        bool practice = trial.settings.GetString("trial_type") == "practice";
        string practiceType = trial.settings.GetString("practice_type");

        string ballMass = trial.settings.GetString("ball_mass");
        int trajectory = trial.settings.GetInt("trajectory");

        string coherentDirection = Session.instance.CurrentTrial.settings.
            GetString("coherent_direction");

        string motionCoherenceString = Session.instance.CurrentTrial.settings.
            GetString("motion_coherence");
        float motionCoherence = float.Parse(motionCoherenceString, CultureInfo.InvariantCulture);

        bool showFeedback = Session.instance.CurrentTrial.settings.
            GetBool("show_feedback");

        touchManager.Enable();
        touchManager.EnableCollider();

        ballGrabbed = false;

        // Regenerate trajectory if first trial in block
        // (re-generating is expensive).
        // If you want to manipulate trajectory within blocks, a good way could
        // be to keep track of the previous trajectory and re-generate only when
        // necessary (use Profiler to check performance)
        if (trial.numberInBlock == 1)
        {
            ShowTrajectory(trajectory);
        }

        // Prepare the dot motion trial in advance (reduce lag spikes)
        if (!practice || practiceType == "easy")
        {
            dotMotionManager.currentTrial = dotMotionManager.
                SimpleTrial(coherentDirection, motionCoherence, showFeedback);

            if (dotMotionManager.currentTrial is DotMotionTrial dotMotionTrial)
                dotMotionTrial.PrepareCanvas();
        }

        // In the first practice trials it's just arrows instead of dot motion.
        else if (practiceType == "follow_trajectory")
        {
            var flankerTrial = new FlankerTrial(
                coherentDirection,
                true,
                true,
                new Vector2(200, 200),
                dotMotionManager.canvasManager,
                32,
                dotMotionManager.textPrefab);

            dotMotionManager.currentTrial = flankerTrial;
        }

        eventForTracker = "begin_trial";
        lslBackend.SendEvent(eventForTracker);

        // TODO: do the following only when showing feedback
        // {
        ShowFixationCross();
        StartProximityToCrossFeedback();
        waitingForButton1Click = true;
        // }

        startTime = Time.realtimeSinceStartupAsDouble;

        // Start Tracker thread
        realTimePositionTracker.StartThread();

#if UNITY_EDITOR
        Debug.Log("[TrialManager] startTime = " + startTime);
        Debug.Log($"[TrialManager] Begin experimental trial no. {trial.number}");
#endif
    }

    public void EndTrial(string surfaceType)
    {
        double stopTime = Time.realtimeSinceStartupAsDouble;

        var trial = Session.instance.CurrentTrial;
        var settings = trial.settings;
        var result = trial.result;

#if UNITY_EDITOR
        Debug.Log($"[TrialManager] End trial no. {trial.number}: ball hit {surfaceType}");
#endif

        // Log reaction times to the results

        // Log the manually tracked start_time and stop_time (using realTime).
        result["real_start_time"] = startTime * 1000;
        result["real_grab_time"] = grabTime * 1000;
        result["real_dot_motion_start_time"] = dotMotionStartTime * 1000;
        result["real_end_time"] = stopTime * 1000;

        // Log the time to grab (starting from after block-instructions until
        // the ball is grabbed) to results
        result["time_to_grab"] = (grabTime - startTime) * 1000;

        // Log the reaction time (starting from grabbing the ball until the ball
        // hits a surface) to results
        result["reaction_time"] = (stopTime - dotMotionStartTime) * 1000;

        // Log behavioral response to the results.
        if (surfaceType == "ground")
            result["motion_response"] = "invalid";
        else if (surfaceType == "left")
            result["motion_response"] = "left";
        else if (surfaceType == "right")
            result["motion_response"] = "right";


        // Log the independent variables / trial settings to the results
        string trialType = settings.GetString("trial_type");
        string practiceType = settings.GetString("practice_type");
        string coherentDirection = settings.GetString("coherent_direction");
        string motionCoherence = settings.GetString("motion_coherence");
        string ballMass = settings.GetString("ball_mass");
        int trajectory = settings.GetInt("trajectory");
        bool showFeedback = settings.GetBool("show_feedback");

        result["trial_type"] = trialType;
        result["practice_type"] = practiceType;
        result["coherent_direction"] = coherentDirection;
        result["motion_coherence"] = motionCoherence;
        result["ball_mass"] = ballMass;
        result["trajectory"] = trajectory;
        result["show_feedback"] = showFeedback;

#if UNITY_EDITOR
        Debug.Log("[TrialManager] stopTime = " + stopTime);

        Debug.Log("[TrialManager] End trial: rt = " +
            (stopTime - grabTime) * 1000);

        Debug.Log("[TrialManager] End trial: time_to_grab = " +
            (grabTime - startTime) * 1000);
#endif

        if (dotMotionManager.Running)
            dotMotionManager.EndTrial();

        touchManager.DisableSpringForce();

        if (monitorProximityToTrajectory)
        {
            StopProximityToTrajectory();
            tubeRenderer.SetMaterial(tubeRenderer.tubeDark);
        }

        // Log event to continous position tracking
        eventForTracker = "trial_end";
        lslBackend.SendEvent(eventForTracker);

        // Show feedback if configured. The feedback Coroutine will call
        // Session.instance.CurrentTrial.End()
        if (dotMotionManager.currentTrial.ShowFeedback)
        {
            string feedback = coherentDirection == surfaceType ? "RICHTIG" : "FALSCH";

            StartCoroutine(ShowFeedbackRoutine(feedback));

            CleanupScene();
        }
        // If feedback is disabled, we call Session.instance.CurrentTrial.End() here.
        else
        {
            CleanupScene();

            // Stop Tracker thread
            realTimePositionTracker.StopThread();

            // Delay Session.instance.CurrentTrial.End() by 1 frame to avoid
            // lag spikes.
            StartCoroutine(DelayedTrialEnd());
        }
    }

    IEnumerator DelayedTrialEnd()
    {
        yield return null; // wait 1 frame

        Session.instance.CurrentTrial.End();
    }

    public void ShowFeedback(string feedback)
    {
        // Fixation cross might be visible if participant drops the ball while
        // fixation cross is still shown.
        dotMotionManager.currentTrial.canvasManager.ClearCanvas();

        ShowTextHeadsUp(feedback);
    }

    IEnumerator ShowFeedbackRoutine(string feedback)
    {
        ShowFeedback(feedback);

        yield return new WaitForSeconds(1f);

        // TODO: Refactor this with an interface
        dotMotionManager.currentTrial.canvasManager.ClearCanvas();

        // Stop Tracker thread
        realTimePositionTracker.StopThread();

        StartCoroutine(DelayedTrialEnd());
    }

    public void ShowTextHeadsUp(string text, int fontSize = 32)
    {
        var instructionText = new InstructionText(text, 
                                                  dotMotionManager.textPrefab,
                                                  Vector2.zero,
                                                  Vector2.zero,
                                                  Color.black,
                                                  fontSize);

        dotMotionManager.canvasManager.AddElement(instructionText);
    }

    public void CleanupScene()
    {
#if UNITY_EDITOR
        Debug.Log("[TrialManager] Cleaning up the scene");
#endif

        if (ball != null)
        {
            Destroy(ball);
#if UNITY_EDITOR
            Debug.Log("[TrialManager] Destroyed ball");
#endif
        }
    }

    public void Update()
    {
        // Regularly check if the touch is near the fixation cross;
        // if it is, turn the fixation cross blue.
        if (monitorProximityToFixationCross)
            if (CloseEnoughToCross())
                experiment2DManager.SetFixationCrossColor(Color.blue);
            else
                experiment2DManager.SetFixationCrossColor(Color.black);

        // Regularly check if the touch is near the trajectory;
        // if it is, turn it ligher blue.
        if (monitorProximityToTrajectory)
            if (CloseEnoughToTrajectory())
                tubeRenderer.SetMaterial(tubeRenderer.tubeLight);
            else
                tubeRenderer.SetMaterial(tubeRenderer.tubeDark);
    }

    // Grabbing and releasing the balls:
    // We actually do not grab a ball game object, but instead we resize the
    // tip of the haptic touch device to make it look like a ball is appearing.
    // This is a workaround, because we want to track the ball's position with
    // 1kHz. Unity's transform positions are tied to FixedUpdate(), which is
    // way too much overhead to run at 1kHz. Instead, by defining the "ball"
    // as the tip of the touch device, we can elegantly sample the position
    // of the device directly via the API, which works nicely at 1kHz.

    /// <summary>
    /// Emulates grabbing a ball by resizing the collider to be large.
    /// </summary>
    public void EmulateSpawnBallWithoutGravity()
    {
        string ballMassString = Session.instance.CurrentTrial.settings.GetString("ball_mass");
        float ballMass = float.Parse(ballMassString, CultureInfo.InvariantCulture);

        touchManager.SetColliderScale(0.03f);
    }

    /// <summary>
    /// Release the currently grabbed ball
    /// </summary>
    /// <remarks>
    /// It's actually just an emulation: we reset gravity and resize the
    /// collider to be very small.
    /// </remarks>
    public void ReleaseBall()
    {
        touchManager.SetColliderScale(0.001f);
        touchManager.SetGravityForce(new Vector3(0.0f, 0.0f, 0.0f));

        touchManager.DisableColliderTemporarily(0.5f);

        ballGrabbed = false;
    }

    /// <summary>
    /// Spawns a dummy ball so it looks like we actually released the ball
    /// </summary>
    /// <param name="position"></param>
    public void SpawnDummyBall(Vector3 position)
    {
        ball = Instantiate(dummyBallPrefab, position, Quaternion.identity);
    }

    /// <summary>
    /// Button handler for button 1 click of the haptic touch device.
    /// </summary>
    /// <remarks>
    /// Register with HapticPlugin event "On Click Button 1".
    /// (In the inspector: TouchActor - HapticActor - HapticPlugin - Events)
    /// </remarks>
    public void HandleButton1Click()
    {

#if UNITY_EDITOR
        Debug.Log("[TrialManager] Button 1 clicked at " + Time.realtimeSinceStartup + " seconds");
#endif

        if (!Session.instance.InTrial)
        {
#if UNITY_EDITOR
            Debug.Log("[TrialManager] Nothing to do because we're not in a trial");
#endif
            return; // Nothing to do because no trial is running
        }
        

        if (waitingForContinueButtonClickWithinTrial)
        {
            var trial = Session.instance.CurrentTrial;
            var settings = trial.settings;

            // We're in a practice trial => end trial
            if (settings.GetString("trial_type") == "practice")
            {
#if UNITY_EDITOR
                Debug.Log($"[TrialManager] Ended practice trial no. {trial.number} due to button click");
#endif

                double stopTime = Time.realtimeSinceStartupAsDouble;

                StopProximityToCrossFeedback();

                dotMotionManager.canvasManager.ClearCanvas();

                var result = trial.result;

                result["trial_type"] = settings.GetString("trial_type");
                result["practice_type"] = settings.GetString("practice_type");
                result["real_start_time"] = startTime * 1000;
                result["real_stop_time"] = startTime * 1000;
                result["reaction_time"] = (stopTime - startTime) * 1000;

                Session.instance.CurrentTrial.End();
            }
        }


        if (!waitingForButton1Click)
        {
#if UNITY_EDITOR
            Debug.Log("[TrialManager] Nothing to do because we're not waiting for a button click");
#endif
            return;
        }


        if (dotMotionManager.Running)
        {
#if UNITY_EDITOR
            Debug.Log("[TrialManager] Nothing to do because task already running");
#endif
            return; // Nothing to do if already running
        }


        if (!CloseEnoughToCross())
        {
#if UNITY_EDITOR
            Debug.Log("[TrialManager] Nothing to do because the touch is not close enough to the fixation cross");
#endif
            return;
        }


        grabTime = Time.realtimeSinceStartupAsDouble;
        ballGrabbed = true;

        waitingForButton1Click = false;

        StopProximityToCrossFeedback();
        HideFixationCross();
        EmulateSpawnBallWithoutGravity();

        // Lock the haptic device in starting position
        touchManager.MoveToFixedPositionWithSpring(ballSpawnPoint.position, 0.1f);

        eventForTracker = "ball_grabbed";
        lslBackend.SendEvent(eventForTracker);
        
#if UNITY_EDITOR
        Debug.Log("[TrialManager] Ball has been grabbed: grabTime = " + grabTime);
#endif

        dotMotionManager.ClearText();

        StartCoroutine(BeginDotMotionTrialAfterFixationCrossCoroutine(1500f));
    }


    /// Button handler for button 1 release of the haptic touch device.
    /// </summary>
    /// <remarks>
    /// Register with HapticPlugin event "On Release Button 1".
    /// (In the inspector: TouchActor - HapticActor - HapticPlugin - Events)
    /// </remarks>
    public void HandleButton1Release()
    {
#if UNITY_EDITOR
        Debug.Log("[TrialManager] Button 1 released at " + Time.realtimeSinceStartup + " seconds");
#endif

        // Check if ball was released while dot motion fixation cross was showing
        // => stop coroutine of starting the dot motion task.
        if (eventForTracker == "upper_fixation_cross")
        {
            beginDotMotionCoroutineShouldStop = true;
        }

        if (!Session.instance.InTrial)
        {
#if UNITY_EDITOR
            Debug.Log("[TrialManager] Nothing to do because we're not in a trial");
#endif
            return; // Nothing to do because no trial is running
        }

        if (!ballGrabbed)
        {
#if UNITY_EDITOR
            Debug.Log("[TrialManager] Nothing to do because we haven't grabbed a ball");
#endif
            return;
        }

        ReleaseBall();

        // Spawn dummy ball to emulate dropping the ball
        var position = touchManager.GetTransformPosition();
        SpawnDummyBall(position);

    }

    /// <summary>
    /// Button handler for button 1 hold of the haptic touch device.
    /// </summary>
    /// <remarks>
    /// Don't register - debug only
    /// </remarks>
    public void HandleButton1Hold()
    {
#if UNITY_EDITOR
        Debug.Log("[TrialManager] Button 1 hold at " + Time.realtimeSinceStartup + " seconds");
#endif
    }

    /// <summary>
    /// Button handler for button 2 click of the haptic touch device.
    /// </summary>
    /// <remarks>
    /// Don't register - debug only
    /// </remarks>
    public void HandleButton2Click()
    {
#if UNITY_EDITOR
        Debug.Log("[TrialManager] Button 2 clicked at " + Time.realtimeSinceStartup + " seconds");
#endif

        if (waitingForContinueButtonClickInMenu)
        {
#if UNITY_EDITOR
            Debug.Log("[TrialManager] Continue instructions due to button click");
#endif

            waitingForContinueButtonClickInMenu = false;

            // Directly call the handler, just like from a mouse click
            blockManager.InstructionsButtonHandler();
        }
    }

    /// <summary>
    /// Button handler for button 2 release of the haptic touch device.
    /// </summary>
    /// <remarks>
    /// Don't register - debug only
    /// </remarks>
    public void HandleButton2Release()
    {
#if UNITY_EDITOR
        Debug.Log("[TrialManager] Button 2 released at " + Time.realtimeSinceStartup + " seconds");
#endif
    }

    /// <summary>
    /// Button handler for button 2 hold of the haptic touch device.
    /// </summary>
    /// <remarks>
    /// Don't register - debug only
    /// </remarks>
    public void HandleButton2Hold()
    {
#if UNITY_EDITOR
        Debug.Log("[TrialManager] Button 2 hold at " + Time.realtimeSinceStartup + " seconds");
#endif
    }

    public void ShowFixationCross()
    {
        experiment2DManager.ShowFixationCross();

        Vector3 crossPosition = experiment2DManager.canvasManager.gameObject.transform.position;
    }

    public void HideFixationCross()
    {
        experiment2DManager.HideFixationCross();
    }

    /// <summary>
    /// Indicate proximity to the fixation cross by changing its color.
    /// </summary>
    public void StartProximityToCrossFeedback()
    {
#if UNITY_EDITOR
        Debug.Log("[TrialManager] Start showing feedback of proximity to the fixation cross.");
#endif

        monitorProximityToFixationCross = true;
    }

    public void StopProximityToCrossFeedback()
    {
#if UNITY_EDITOR
        Debug.Log("[TrialManager] Stop showing feedback of proximity to the fixation cross.");
#endif
        monitorProximityToFixationCross = false;
    }

    /// <summary>
    /// Checks if the touch is held within a certain radius around the fixation cross.
    /// </summary>
    /// <returns></returns>
    public bool CloseEnoughToCross()
    {
        Vector3 touchPosition = touchManager.GetTransformPosition();
        Vector3 ballSpawnPointPosition = ballSpawnPoint.position;

        float distance = Vector3.Distance(touchPosition, ballSpawnPointPosition);

        if (distance < 0.03f)
            return true;
        else
            return false;
    }

    /// <summary>
    /// Indicate proximity to the trajectory by changing its color.
    /// </summary>
    public void StartProximityToTrajectory()
    {
#if UNITY_EDITOR
        Debug.Log("[TrialManager] Start showing feedback of proximity to the trajectory.");
#endif

        monitorProximityToTrajectory = true;
    }

    public void StopProximityToTrajectory()
    {
#if UNITY_EDITOR
        Debug.Log("[TrialManager] Stop showing feedback of proximity to the trajectory.");
#endif
        monitorProximityToTrajectory = false;
    }

    /// <summary>
    /// Checks if the touch is held within a certain radius around the eas
    /// trajectory.
    /// </summary>
    /// <returns></returns>
    public bool CloseEnoughToTrajectory()
    {
        Vector3 touchPosition = touchManager.GetTransformPosition();

        float distance = tubeRenderer.DistanceToTrajectory(touchPosition, 1);

        if (distance < 0.015f)
            return true;
        else
            return false;
    }
    
    public void ShowTrays()
    {
#if UNITY_EDITOR
        Debug.Log("[TrialManager] Showing trays.");
#endif

        trayLeft.SetActive(true);
        trayRight.SetActive(true);
    }
    public void HideTrays()
    {
#if UNITY_EDITOR
        Debug.Log("[TrialManager] Hiding trays.");
#endif

        trayLeft.SetActive(false);
        trayRight.SetActive(false);
    }

    /// <summary>
    /// Ensures a minimum viewing time for instructions.
    /// </summary>
    /// <param name="minimumViewingTime"></param>
    public void StartMinimumViewingTime(float minimumViewingTime)
    {
        StartCoroutine(MinimumViewingTimeCoroutine(minimumViewingTime));
    }

    public IEnumerator MinimumViewingTimeCoroutine(float minimumViewingTime)
    {
        waitingForContinueButtonClickWithinTrial = false;

        yield return new WaitForSeconds(minimumViewingTime);

        waitingForContinueButtonClickWithinTrial = true;
    }


    /// <summary>
    /// Dot motion task starts after the fixation cross is shown.
    /// </summary>
    /// <param name="milliseconds"></param>
    /// <returns></returns>
    IEnumerator BeginDotMotionTrialAfterFixationCrossCoroutine(float milliseconds)
    {
        int coroutineId = ++beginDotMotionCoroutineCounter;
        int trialNumber = Session.instance.CurrentTrial.number;

#if UNITY_EDITOR
        Debug.Log($"[TrialManager] Part 1 of BeginDotMotionTrialAfterFixationCrossCoroutine with ID {coroutineId} in trial no. {trialNumber}");
#endif

        dotMotionManager.ShowFixationCross();

        eventForTracker = "upper_fixation_cross";
        lslBackend.SendEvent(eventForTracker);

        yield return new WaitForSeconds(milliseconds / 1000f);

#if UNITY_EDITOR
        Debug.Log($"[TrialManager] Part 2 of BeginDotMotionTrialAfterFixationCrossCoroutine with ID {coroutineId} in trial no. {trialNumber}");
#endif

        // If this is a coroutine from a previous trial:
        // Participant dropped the ball while fixation cross was showing
        // => don't start dot motion task and let trial be ended.
        if (trialNumber != Session.instance.CurrentTrial.number || beginDotMotionCoroutineShouldStop)
        {
#if UNITY_EDITOR
            Debug.Log($"[TrialManager] BeginDotMotionTrialAfterFixationCrossCoroutine with ID {coroutineId} in trial no. {trialNumber}: Ball was dropped while fixation cross was showing.");
#endif
            yield break;
        }

        touchManager.DisableSpringForce();

        string ballMassString = Session.instance.CurrentTrial.settings.GetString("ball_mass");
        float ballMass = float.Parse(ballMassString, CultureInfo.InvariantCulture);

        var gravity = touchManager.GravityForceFromMass(ballMass);
        touchManager.SetGravityForce(gravity);

        dotMotionManager.HideFixationCross();

        eventForTracker = "dot_motion_task";
        lslBackend.SendEvent(eventForTracker);

        dotMotionStartTime = Time.realtimeSinceStartupAsDouble;

        dotMotionManager.BeginTrial();
    }
    

    public void ShowTrajectory(int trajectory)
    {
        HideTrajectories();

        // Full trajectories (easy and hard) are generated by the tubeRenderer,
        // the trajectories that only show some waypoints are pre-generated
        // game objects that are just enabled.
        if (trajectory == 1 || trajectory == 2)
        {
            tubeRenderer.ApplyTube(trajectory);
            tubeRenderer.Show();
        }
        else if (trajectory == 3)
        {
            trajectory3.SetActive(true);
        }
        else if (trajectory == 4)
        {
            trajectory4.SetActive(true);
        }
    }
    

    public void HideTrajectories()
    {
        tubeRenderer.Hide();
        trajectory3.SetActive(false);
        trajectory4.SetActive(false);
    }
}