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
using System.IO;
using System.Linq;

using UXF;

/// <summary>
/// Manages the experimental session, generates independent variables,
/// blocks, trials etc.
/// </summary>
public class SessionManager : MonoBehaviour
{
    // Helps to skip practice trials for debug purposes
    public enum ExperimentDebugLevel
    {
        Normal,
        Debug,
        SkipPractice
    }

    private ExperimentDebugLevel experimentDebugLevel;

    // Physical display settings - tweak if you run this on a different monitor!
    public float screenDiagonalInch = 31.5f;
    public float realDPI;
    
    private string instructionsInStartupPanel = @"
Gib die demografischen Daten der Versuchsperson ein.

(Hier Instruktionen für Versuchsleitung ...)

Ordner für Datenspeicherung: XY

Versuchspersonen-ID: ganzzahlig fortlaufende Nummer.

Sessionnummer: wähle 1 
    ";


    // assign in Inspector
    public TouchManager touchManager;
    public LSLSyncBackend lslBackend;
    public GameObject touchDeviceConnectedText;
    public GameObject instructionsInStartUpPanelText;
    public GameObject startExperimentButton;

    public void Awake()
    {
        // Change here to control the logic
        experimentDebugLevel = ExperimentDebugLevel.Normal;

        SetResolutionAndRefreshRate();
        LogDebugInfo();
        lslBackend.SetUp();
        StartCoroutine(CheckIfTouchIsConnected());
        
        // Set start-up instructions text
        SetTextOfStartupPanel(instructionsInStartupPanel);
    }
    
    public void SetTextOfStartupPanel(string instructions)
    {
        instructionsInStartUpPanelText.GetComponent<Text>().text = instructions;
    }
    
    public IEnumerator CheckIfTouchIsConnected()
    {
        // Skip one frame so that the device information is updated before.
        yield return null;

        var connected = touchManager.IsConnected();
        Debug.Log($"connected =  {connected}");

        var textComponent = touchDeviceConnectedText.GetComponent<Text>();
        var buttonCompoment = startExperimentButton.GetComponent<Button>();

        if (!touchManager.IsConnected())
        {
            Debug.Log("[SessionManager] Haptic touch device is not connected.");

            textComponent.text = "✖ Haptic Touch-Gerät ist NICHT verbunden! " +
                "Bitte verbinden und die Anwendung neu starten.";

            textComponent.color = Color.red;
            buttonCompoment.interactable = false;
        } else
        {
            textComponent.text = "✓ Haptic Touch-Gerät ist verbunden";
            textComponent.color = Color.green;
            buttonCompoment.interactable = true;
        }

    }

    public void SetupSession(Session session)
    {
        GenerateExperiment(session);
        LogDebugInfo();
    }

    public void SetResolutionAndRefreshRate()
    {
        int horizontalPx = 2560;
        int verticalPx = 1440;
        // int horizontalPx = 3840;
        // int verticalPx = 2160;

        Screen.SetResolution(
            horizontalPx,
            verticalPx,
            FullScreenMode.FullScreenWindow,
            new RefreshRate() {
                numerator = 144,
                denominator = 1
            }
        );

        realDPI = Mathf.Sqrt(
            horizontalPx*horizontalPx + verticalPx*verticalPx
        ) / screenDiagonalInch;

        Debug.Log("[SessionGenerator] Setting resolution and refresh rate to " +
                  Screen.currentResolution);

        Debug.Log($"[SessionGenerator] Unity thinks that Screen.dpi " +
                  $"is {Screen.dpi}. Hand-calculated DPI with monitor " +
                  $"diagonal of {screenDiagonalInch} inch and resulution of " +
                  $"{horizontalPx} x {verticalPx} is {realDPI}. " +
                  $"We use {realDPI} for calculating dot sizes and angular " + 
                  $"velocities.");
    }

    public void LogDebugInfo()
    {
        Debug.Log("[SessionGenerator] ExperimentDebugLevel " +
                  $"is {experimentDebugLevel}");

        Debug.Log("[SessionGenerator] 1 / Time.unscaledDeltaTime is " +
                  1.0f / Time.unscaledDeltaTime);

        Debug.Log("[SessionGenerator] Screen.currentResolution is " +
                  Screen.currentResolution);

        Debug.Log("[SessionGenerator] Application.targetFrameRate is " +
                  Application.targetFrameRate);
    }

    public int[] GetBlockSequenceFromPPID(int ppid)
    {
        // All combinations:
        // Nr | Coherence | Ball Mass | Trajectory
        // ---+-----------+-----------+------------
        //  1 | easy      | easy      | easy
        //  2 | easy      | easy      | hard 
        //  3 | easy      | hard      | easy
        //  4 | easy      | hard      | hard
        //  5 | hard      | easy      | easy
        //  6 | hard      | easy      | hard
        //  7 | hard      | hard      | easy
        //  8 | hard      | hard      | hard
        
        // We want balanced latin squares with n=8
        // => 8 permutations that we counterbalance between participants.
        // We collect multiples of 8 participants.

        int[,] latinSquare =
        {
            { 1, 2, 8, 3, 7, 4, 6, 5 },
            { 2, 3, 1, 4, 8, 5, 7, 6 },
            { 3, 4, 2, 5, 1, 6, 8, 7 },
            { 4, 5, 3, 6, 2, 7, 1, 8 },
            { 5, 6, 4, 7, 3, 8, 2, 1 },
            { 6, 7, 5, 8, 4, 1, 3, 2 },
            { 7, 8, 6, 1, 5, 2, 4, 3 },
            { 8, 1, 7, 2, 6, 3, 5, 4 }
        };

        int row = (ppid - 1) % 8;

        var sequence = new int[8];
        for (int i = 0; i < 8; i++)
            sequence[i] = latinSquare[row, i];

        return sequence;   
    }


    // *************************************************************************
    // Define levels of the independent variables. Tweak them if desired.
    // *************************************************************************
    private enum Difficulty { Easy, Hard }

    // Independent variables lookup table
    private static readonly Dictionary<int, Dictionary<string, Difficulty>> ivLookupTable =
        new()
    {
        [1] = new() { ["ballweight"] = Difficulty.Easy, ["trajectory"] = Difficulty.Easy, ["coherence"] = Difficulty.Easy },
        [2] = new() { ["ballweight"] = Difficulty.Easy, ["trajectory"] = Difficulty.Easy, ["coherence"] = Difficulty.Hard },
        [3] = new() { ["ballweight"] = Difficulty.Easy, ["trajectory"] = Difficulty.Hard, ["coherence"] = Difficulty.Easy },
        [4] = new() { ["ballweight"] = Difficulty.Easy, ["trajectory"] = Difficulty.Hard, ["coherence"] = Difficulty.Hard },
        [5] = new() { ["ballweight"] = Difficulty.Hard, ["trajectory"] = Difficulty.Easy, ["coherence"] = Difficulty.Easy },
        [6] = new() { ["ballweight"] = Difficulty.Hard, ["trajectory"] = Difficulty.Easy, ["coherence"] = Difficulty.Hard },
        [7] = new() { ["ballweight"] = Difficulty.Hard, ["trajectory"] = Difficulty.Hard, ["coherence"] = Difficulty.Easy },
        [8] = new() { ["ballweight"] = Difficulty.Hard, ["trajectory"] = Difficulty.Hard, ["coherence"] = Difficulty.Hard }
    };

    private static readonly Dictionary<Difficulty, float> motionCoherenceLevels =
        new() { [Difficulty.Easy] = 0.3f, [Difficulty.Hard] = 0.15f };

    private static readonly Dictionary<Difficulty, float> ballMassLevels =
        new() { [Difficulty.Easy] = 0.1f, [Difficulty.Hard] = 0.3f };

    private static readonly Dictionary<Difficulty, int> trajectoryLevels =
        new() { [Difficulty.Easy] = 1, [Difficulty.Hard] = 2 };


    private void GetIVsFromBlockNumber(int blockNumber, out float mass, out int trajectory, out float coherence)
    {
        if (!ivLookupTable.ContainsKey(blockNumber))
            Debug.LogError($"Block number {blockNumber} is invalid");

        var blockIVs = ivLookupTable[blockNumber];

        mass = ballMassLevels[blockIVs["ballweight"]];
        trajectory = trajectoryLevels[blockIVs["trajectory"]];
        coherence = motionCoherenceLevels[blockIVs["coherence"]];
    }

    /// <summary>
    /// Generates the blocks and trials with different levels of independent
    /// variables.
    /// </summary>
    /// <param name="session"></param>
    public void GenerateExperiment(Session session)
    {
        Debug.Log("[SessionGenerator] Generating trials");

        Trial newTrial;

        // *********************************************************************
        // Define some constants. Could be changed or included as IVs.
        // *********************************************************************
        string ballColor = "white";

        int nPracticeTrials = 4;

        int nMainTrials = 10;

        string postExperimentInstructions = @"Gut gemacht! Das Experiment " +
            "ist nun zu Ende. Bitte gib der Versuchsleitung Bescheid.";


        // *********************************************************************
        // Create the practice trials
        // *********************************************************************

        // *********************************************************************
        // Block 0: practice block, only movement of the device
        // *********************************************************************
        Block block0 = session.CreateBlock();

        string practicePreInstructions = @"Herzlich willkommen zum Reaktionszeitexperiment.

Du wirst mit dem Stift einige Kugeln bewegen und dabei eine visuelle Aufgabe bearbeiten.

Zuerst bekommst du die Möglichkeit, dich mit der Stiftbedienung vertraut zu machen.";

        block0.settings.SetValue("pre_instructions", practicePreInstructions);
        block0.settings.SetValue("post_instructions", "");

        block0.settings.SetValue("trial_type", "practice");

        // Trials start simple and get more complex.
        newTrial = block0.CreateTrial();
        newTrial.settings.SetValue("practice_type", "touch_movement");
        newTrial = block0.CreateTrial();
        newTrial.settings.SetValue("practice_type", "touch_movement2");
        newTrial = block0.CreateTrial();
        newTrial.settings.SetValue("practice_type", "touch_movement3");
        newTrial = block0.CreateTrial();
        newTrial.settings.SetValue("practice_type", "touch_movement4");


        // *********************************************************************
        // Block 1: practice block, follow trajectory with simple flanker arrows
        // *********************************************************************
        Block block1 = session.CreateBlock();

        block1.settings.SetValue("pre_instructions", $@"Gut gemacht!
        
Bewege nun den Stift zum schwarzen Kreuz, bis es blau wird.
Drücke und <b>halte</b> dann den dunkelgrauen Button.
Es erscheint eine Kugel an der Stiftspitze.

Gleichzeitig zeigt ein Pfeil in welche Kiste du die Kugel ablegen musst.

Bewege dann die Kugel entlang der blauen Bahn in die richtige Kiste.

Die Kugel soll dabei die Bahn berühren.
Die Bahn leuchtet hellblau, wenn die Kugel sie berührt.

Es ist wichtig, dass du der Bahn möglichst genau folgst.

Es folgen {nPracticeTrials} Übungstrials, bei denen du die Bewegung üben kannst.
");


        block1.settings.SetValue("post_instructions", "");

        block1.settings.SetValue("trial_type", "practice");

        block1.settings.SetValue("motion_coherence", 1);
        block1.settings.SetValue("ball_mass", 0.1);
        block1.settings.SetValue("trajectory", 1);
        block1.settings.SetValue("ball_color", ballColor);

        // Create the trials
        for (int i = 0; i < nPracticeTrials; i++)
        {
            newTrial = block1.CreateTrial();

            newTrial.settings.SetValue("practice_type", "follow_trajectory");

            // Randomly choose the coherent direction
            string coherentDirection = Random.value < 0.5f ? "left" : "right";

            // Apply the trial settings
            newTrial.settings.SetValue("coherent_direction", coherentDirection);
            newTrial.settings.SetValue("show_feedback", true);
        }

        // Randomize trial order
        block1.trials.Shuffle();


        // Block 2: realistic practice block
        Block block2 = session.CreateBlock();

        block2.settings.SetValue("pre_instructions", @$"Gut gemacht!
Als Nächstes folgt ein Übungsblock, der dem echten Experiment sehr ähnlich ist.

Bewege den Stift zum schwarzen Kreuz, bis es blau wird.
Drücke und halte dann den dunkelgrauen Button.
Es erscheint eine Kugel an der Stiftspitze.

Als Nächstes erscheint für einen kurzen Moment ein Fixationskreuz. Fokussiere dich darauf.

Danach siehst du blaue Punkte, die sich bewegen.
Ein Teil der Punkte bewegt sich gleichzeitig in eine Richtung.

Du musst entscheiden, ob sich diese Punkte nach rechts oder links bewegen.
Bewege dann die Kugel entlang der blauen Bahn in die Kiste auf der richtigen Seite.

Die Kugel soll dabei die Bahn berühren.
Die Bahn leuchtet hellblau, wenn die Kugel sie berührt.

Versuche, die Kugel so schnell wie möglich abzulegen und dabei der Bahn möglichst genau zu folgen.

Es folgen {nPracticeTrials} Übungstrials.
");


        block2.settings.SetValue("post_instructions", @$"Gut gemacht!

Jetzt folgen {ivLookupTable.Count} Experimentalblöcke. Zwischen den Blöcken kannst du eine Erholungspause machen.

Zur Erinnerung: Bewege den Stift zum schwarzen Kreuz, bis es blau wird.

Drücke und halte dann den dunkelgrauen Button.

Bearbeite die visuelle Aufgabe, indem du die Kugel entlang der blauen Bahn
in die richtige Kiste bewegst.

Die Kugel soll dabei die Bahn berühren.

Versuche, die Kugel so schnell wie möglich abzulegen und dabei der Bahn möglichst genau zu folgen.

Es folgen {nMainTrials} Trials.
");

        block2.settings.SetValue("trial_type", "practice");

        block2.settings.SetValue("motion_coherence", 0.40);
        block2.settings.SetValue("ball_mass", 0.1);
        block2.settings.SetValue("trajectory", 1);

        block2.settings.SetValue("ball_color", ballColor);

        // Create the trials
        for (int i = 0; i < nPracticeTrials; i++)
        {
            newTrial = block2.CreateTrial();

            newTrial.settings.SetValue("practice_type", "easy");

            // Randomly choose the coherent direction
            string coherentDirection = Random.value < 0.5f ? "left" : "right";

            // Apply the trial settings
            newTrial.settings.SetValue("coherent_direction", coherentDirection);
            newTrial.settings.SetValue("show_feedback", true);
        }

        // Randomize trial order
        block2.trials.Shuffle();

        // *********************************************************************
        // Create the 8 experimental blocks
        // *********************************************************************
        // Sequence according to latin squares
        var blockSequence = GetBlockSequenceFromPPID(int.Parse(session.ppid));
        
        Block block;

        // Temporarily store the new blocks -> need to shuffle the blocks but
        // keep practice block in the beginning;
        List<Block> blocks = new List<Block>();

        foreach (int blockNumber in blockSequence)
        {
            GetIVsFromBlockNumber(
                blockNumber,
                out float mass,
                out int trajectory,
                out float coherence
            );

#if UNITY_EDITOR
            Debug.Log($"Block {blockNumber} with mass = {mass}, trajectory = {trajectory}, coherence = {coherence}.");
#endif

            block = session.CreateBlock();
            blocks.Add(block);

            block.settings.SetValue("post_instructions", "Gut gemacht! Wenn du möchtest, kannst du kurz Pause machen.");

            block.settings.SetValue("trial_type", "experimental");

            block.settings.SetValue("practice_type", "");

            block.settings.SetValue("ball_color", ballColor);

            // Apply the IV block settings
            block.settings.SetValue("motion_coherence", coherence);
            block.settings.SetValue("ball_mass", mass);
            block.settings.SetValue("trajectory", trajectory);

            // Create the trials
            for (int i = 0; i < nMainTrials; i++)
            {
                newTrial = block.CreateTrial();

                // Randomly choose the coherent direction
                string coherentDirection = Random.value < 0.5f ? "left" : "right";

                // Apply the trial settings
                newTrial.settings.SetValue("coherent_direction", coherentDirection);
                newTrial.settings.SetValue("show_feedback", true);
            }

            // Randomize trial order
            block.trials.Shuffle();
        }

        // Pre-block instructions depend on the block number, so we have to
        // do them after shuffling (we actually don't shuffle now because of
        // latin squares, but if we would)

        for (int blockNum = 0; blockNum < blocks.Count; blockNum++)
        {
            var currentBlock = blocks[blockNum];

            currentBlock.settings.SetValue("pre_instructions", @$"Es folgt Block {blockNum + 1}.

Zur Erinnerung: Bewege den Stift zum schwarzen Kreuz, bis es blau wird.

Drücke und halte dann den dunkelgrauen Button.

Bearbeite die visuelle Aufgabe, indem du die Kugel entlang der blauen Bahn
in die richtige Kiste bewegst.

Die Kugel soll dabei die Bahn berühren.

Versuche, die Kugel so schnell wie möglich abzulegen und dabei der Bahn möglichst genau zu folgen.

Es folgen {nMainTrials} Trials.");
        }

        // No pre-block instructions for the first block necessary
        // (these are in the post-block instructions of the practice trials)
        blocks[0].settings.SetValue("pre_instructions", "");

        // Special post-block instructions for the last block.
        blocks[^1].settings.SetValue("post_instructions", postExperimentInstructions);

        // Manually set the session block list with our desired order
        session.blocks.Clear();

        if (experimentDebugLevel == ExperimentDebugLevel.Normal)
        {
            session.blocks.Add(block0);
            session.blocks.Add(block1);
            session.blocks.Add(block2);

            session.blocks.AddRange(blocks);
        }

        else if (experimentDebugLevel == ExperimentDebugLevel.SkipPractice)
            session.blocks.AddRange(blocks);

        // Debug block with one trial for debug purposes.
        else if (experimentDebugLevel == ExperimentDebugLevel.Debug)
        {
            Block debugBlock = session.CreateBlock();
            debugBlock.settings.SetValue("trial_type", "debug");
            debugBlock.settings.SetValue("pre_instructions", "");
            debugBlock.settings.SetValue("post_instructions", "");
            Trial debugTrial = debugBlock.CreateTrial();
        }

        Debug.Log("[SessionManager] Generated " + session.blocks.Count + " blocks");
    }
    
    // Called from quit button event.
    public void QuitApplication()
    {
        Debug.Log("[SessionManager] Received call to quit application.");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#elif UNITY_STANDALONE
        Application.Quit();
#endif
    }
}