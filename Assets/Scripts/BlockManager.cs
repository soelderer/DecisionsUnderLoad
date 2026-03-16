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
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;
using UXF;

/// <summary>
/// Controls the flow and implements functionality of experimental blocks
/// (e.g., showing instructions for the block and starting trials as necessary)
/// </summary>
public class BlockManager : MonoBehaviour
{
    public enum BlockState
    {
        PreBlock,
        TrialRunning,
        PostBlock,
        EndExperiment
    }

    public BlockState state;

    // *************************************************************************
    // Game Objects that need to be assigned in inspector
    // *************************************************************************

    public GameObject instructionsPanel;
    public GameObject instructionsPanelButtonText;
    public GameObject ufxUI;
    public TouchManager touchManager;
    public TrialManager trialManager;
    public SessionManager sessionManager;

    public void BeginBlock()
    {
        Debug.Log("[BlockManager] Begin block");

        state = BlockState.PreBlock;

        ShowPreBlockInstructions();
    }

    public void EndBlock()
    {
        Debug.Log("[BlockManager] End block");

        state = BlockState.PostBlock;

        // Disable touch input device while showing instructions
        touchManager.Disable();

        ShowPostBlockInstructions();
    }

    public void SetInstructionsText(string instructionsText)
    {
        if (instructionsPanel == null)
        {
            Debug.Log("[BlockManager] No instructionsPanel selected. Nothing to show");
            return;
        }

        Transform[] children = instructionsPanel.GetComponentsInChildren<Transform>();

        foreach (var child in children)
            if (child.CompareTag("DialogText"))
                child.GetComponent<Text>().text = instructionsText;
    }

    public void ShowInstructions(string instructionsText)
    {
        SetInstructionsText(instructionsText);

        instructionsPanel.SetActive(true);

        // Disable the Startup Panel so it's not visible behind the new dialog
        ufxUI.transform.Find("Startup Panel").gameObject.SetActive(false);
        ufxUI.SetActive(true);

        // Show cursor
        Cursor.visible = true;
        
        // Enable continuing with button press on haptic device
        trialManager.waitingForContinueButtonClickInMenu = true;
    }

    public void ShowPreBlockInstructions()
    {
        Debug.Log("[BlockManager] ShowPreBlockInstructions");

        // We are always 1 block number behind: the block number increments
        // when we start the first trial in the block, not before. Before
        // we start the first trial, currentBlockNum = 0 and so on
        // (but the blocks are non-zero indexed).
        int currentBlockNum = Session.instance.currentBlockNum + 1;

        string preInstructionsText = Session.instance.GetBlock(currentBlockNum).
            settings.GetString("pre_instructions");

        if (preInstructionsText != "")
        {
            ShowInstructions(preInstructionsText);
        }
        else
            // Simulate button press (TODO: refactor this more elegantly)
            InstructionsButtonHandler();
    }

    public void ShowPostBlockInstructions()
    {
        Debug.Log("[BlockManager] ShowPostBlockInstructions");

        int currentBlockNum = Session.instance.currentBlockNum;

        string postInstructionsText = Session.instance.GetBlock(currentBlockNum).
            settings.GetString("post_instructions");

        if (postInstructionsText != "")
        {
            ShowInstructions(postInstructionsText);

            // Disable touch input device while showing instructions
            touchManager.Disable();
        }
        else
            // Simulate button press (TODO: refactor this more elegantly)
            InstructionsButtonHandler();
    }

    public void HideInstructions()
    {
        Debug.Log("[BlockManager] HideInstructions");

        if (instructionsPanel == null)
        {
            Debug.Log("[BlockManager] No instructionsPanel selected. Nothing to show");
            return;
        }

        instructionsPanel.SetActive(false);
        ufxUI.SetActive(false);

        // Hide the cursor
        Cursor.visible = false;
    }

    /// <summary>
    /// Begins the next trial within the current block or a new block, if the
    /// current block is at its last trial.
    /// </summary>
    /// <remarks>
    /// Register with UXF OnTrialEnd.// Thanks to Jack Brookes:
    /// https://github.com/immersivecognition/unity-experiment-framework/issues/178#issuecomment-2729512349
    /// </remarks>
    public void BeginNextTrialOrBlock()
    {
        Trial trial = null;

        // Session.instance.CurrentTrial throws an exception if no trial already
        // started.
        if (Session.instance.currentTrialNum > 0)
            trial = Session.instance.CurrentTrial;

        // Beginning of the experiment
        if (trial == null)
        {
            // Begin first block
            BeginBlock();

            return;
        }

        // Last trial of experiment => end session
        else if (trial == trial.session.LastTrial)
        {
            EndBlock();

            // Delay session end by 2 frames: FileSaver delays saving of trackers
            // by 1 frame to avoid lag spikes and we can't end the session before
            // starting SaveData. This is a bit of a hack but it works.
            StartCoroutine(DelayedSessionEnd());

            return;
        }

        // Same block, continue with next trial
        else if (trial.block.number == trial.session.NextTrial.block.number)
        {
            StartCoroutine(BeginNextTrialSafeDelayed());
            return;
        }

        // Last trial of block, still another block to follow (because it's not
        // the last trial of the experiment)
        else
        {
            EndBlock();
        }
    }

    /// <summary>
    /// Begins the next trial delayed by 1 frame.
    /// </summary>
    /// <returns></returns>
    /// <remarks>
    /// To prevent lag spikes on trial end -> move trial begin to next frame.
    /// </remarks
    IEnumerator BeginNextTrialSafeDelayed()
    {
        yield return null; // wait 1 frame

        Session.instance.BeginNextTrialSafe();
    }

    /// <summary>
    /// Delay session end by 2 frames.
    /// </summary>
    /// <returns></returns>
    /// <remarks>
    /// To prevent lag spikes.
    /// </remarks>
    IEnumerator DelayedSessionEnd()
    {
        yield return null;
        yield return null;

        Session.instance.End();
    }

    /// <summary>
    /// Button handler for the instructions panel button.
    /// </summary>
    /// <remarks>
    // Register on the OnClick of the button in the InstructionsPanel.
    /// </remarks>
    public void InstructionsButtonHandler()
    {
        HideInstructions();

        if (state == BlockState.PreBlock)
        {
            // Enable touch input device
            touchManager.Enable();

            // Begin first trial in the block
            state = BlockState.TrialRunning;
            Session.instance.BeginNextTrial();
        }
        else if (state == BlockState.PostBlock)
        {
            // This seems a bit of a hack. Maybe there's a better way for
            // flow control here. BeginBlock() shall only be called if another
            // block is to follow. If the last trial of the experiment already
            // ran, currentTrialNum is 0 again.
            if (Session.instance.currentTrialNum != 0)
                BeginBlock();
        }
        else if (state == BlockState.EndExperiment)
        {
            sessionManager.QuitApplication();
        }
    }
    
    // Register with UXF onSessionEnd
    public void OnSessionEnd()
    {
        state = BlockState.EndExperiment;
        instructionsPanelButtonText.GetComponent<Text>().text = "Beenden";
    }
}
