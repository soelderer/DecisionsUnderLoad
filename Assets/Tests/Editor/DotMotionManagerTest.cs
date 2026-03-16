// -----------------------------------------------------------------------------
// Copyright (C) 2026 Cognition, Action, and Sustainability Unit
// University of Freiburg, Department of Psychology
// Implementation: Paul Soelder
// Supervision: Dr. Andrea Kiesel, Dr. Irina Monno
// All rights reserved.
// 
// This file is part of a GPL-licensed project.
// Proprietary assets used at runtime are excluded from this license.
// SPDX-License-Identifier: MIT
// -----------------------------------------------------------------------------

using UnityEngine;
using UnityEditor;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Unit tests for the DotMotionManager.
/// </summary>
/// <remarks>
/// Currently, they are broken due to a refactor with object pooling
/// and SpriteRenderers.
/// </remarks>
public class DotMotionManagerTest
{
    [SetUp]
    public void SetUp()
    {
        UnityEngine.Random.InitState(42);
    }

    // Tests for DotMotionDot class
    [Test]
    public void DotMotionDot_Constructor_AssignsInitialValues()
    {
        // Arrange
        var testDot = new GameObject("TestDot");

        // Act
        var dot = new DotMotionDot(new Vector2(-1, 1), Vector2.down, 1f,
                                   DotMotionDot.DotMotionColor.Red,
                                   testDot, 0.5f);

        // Assert
        Assert.That(dot.position, Is.EqualTo(new Vector2(-1, 1)));
        Assert.That(dot.direction, Is.EqualTo(Vector2.down));
        Assert.That(dot.speed, Is.EqualTo(1f));
        Assert.That(dot.dotMotionColor,
                    Is.EqualTo(DotMotionDot.DotMotionColor.Red));
        Assert.That(dot.prefab, Is.EqualTo(testDot));
        Assert.That(dot.scale, Is.EqualTo(0.5f));
    }

    [Test]
    public void DotMotionDot_Update_MovesCorrectlyWithPositiveSpeed()
    {
        // Arrange
        var testDot = new GameObject("TestDot");
        var initialPosition = Vector2.zero;
        var direction = Vector2.down;
        var speed = 2f;
        var deltaTime = Time.deltaTime;
        var dot = new DotMotionDot(initialPosition, direction, speed,
                                   DotMotionDot.DotMotionColor.Red,
                                   testDot, 0.5f);

        // Act
        dot.UpdatePosition();

        // Assert
        Assert.That(dot.position, Is.EqualTo(initialPosition +
                                             speed * deltaTime * direction));
    }

    [Test]
    public void DotMotionDot_Update_DoesNotMoveWhenSpeedIsZero()
    {
        // Arrange
        var testDot = new GameObject("TestDot");
        var initialPosition = Vector2.zero;
        var direction = Vector2.down;
        var speed = 0f;
        var deltaTime = Time.deltaTime;
        var dot = new DotMotionDot(initialPosition, direction, speed,
                                   DotMotionDot.DotMotionColor.Red,
                                   testDot, 0.5f);

        // Act
        dot.UpdatePosition();

        // Assert
        Assert.That(dot.position, Is.EqualTo(initialPosition));
    }

    [Test]
    public void DotMotionDot_Update_DoesNotMoveWhenDirectionIsZero()
    {
        // Arrange
        var testDot = new GameObject("TestDot");
        var initialPosition = Vector2.zero;
        var direction = Vector2.zero;
        var speed = 2f;
        var deltaTime = Time.deltaTime;
        var dot = new DotMotionDot(initialPosition, direction, speed,
                                   DotMotionDot.DotMotionColor.Red,
                                   testDot, 0.5f);

        // Act
        dot.UpdatePosition();

        // Assert
        Assert.That(dot.position, Is.EqualTo(initialPosition));
    }

    [Test]
    public void DotMotionDot_Update_MovesCorrectlyWithNegativeSpeed()
    {
        // Arrange
        var testDot = new GameObject("TestDot");
        var initialPosition = Vector2.zero;
        var direction = Vector2.up;
        var speed = -2f;
        var deltaTime = Time.deltaTime;
        var dot = new DotMotionDot(initialPosition, direction, speed,
                                   DotMotionDot.DotMotionColor.Red,
                                   testDot, 0.5f);

        // Act
        dot.UpdatePosition();

        // Assert
        Assert.That(dot.position, Is.EqualTo(initialPosition +
                                             speed * deltaTime * direction));
    }

    // Tests for DotMotionTrial class
    // [Test]
    // public void DotMotionTrial_Constructor_AssignsInitialValues()
    // {
    //     // Arrange
    //     // Mock canvas manager
    //     var dummyObject = new GameObject("CanvasManager");
    //     var canvasManager = dummyObject.AddComponent<CanvasManager>();
    //     var dotPrefab = new GameObject("TestDot");
    //     var numDots = 20;
    //     var motionCoherence = 0.65f;

    //     // Act
    //     var dotMotionTrial = new DotMotionTrial(
    //         DotMotionTrial.SignalSelectionRule.Same,
    //         DotMotionTrial.NoiseType.RandomDirection,
    //         DotMotionTrial.ApertureShape.Square,
    //         DotMotionTrial.OutOfBoundsDecision.RandomlyOpposite,
    //         Vector2.left,
    //         DotMotionDot.DotMotionColor.Blue,
    //         numDots,
    //         motionCoherence,
    //         10f,
    //         false,
    //         new Vector2(10, 10),
    //         canvasManager,
    //         1f,
    //         dotPrefab
    //     );

    //     // Assert
    //     // Assignment of initial values
    //     Assert.That(dotMotionTrial.signalSelectionRule,
    //                 Is.EqualTo(DotMotionTrial.SignalSelectionRule.Same));
    //     Assert.That(dotMotionTrial.noiseType,
    //                 Is.EqualTo(DotMotionTrial.NoiseType.RandomDirection));
    //     Assert.That(dotMotionTrial.apertureShape,
    //                 Is.EqualTo(DotMotionTrial.ApertureShape.Square));
    //     Assert.That(dotMotionTrial.outOfBoundsDecision,
    //                 Is.EqualTo(DotMotionTrial.OutOfBoundsDecision.RandomlyOpposite));
    //     Assert.That(dotMotionTrial.coherentDirection,
    //                 Is.EqualTo(Vector2.left));
    //     Assert.That(dotMotionTrial.coherentColor,
    //                 Is.EqualTo(DotMotionDot.DotMotionColor.Blue));
    //     Assert.That(dotMotionTrial.numDots, Is.EqualTo(20));
    //     Assert.That(dotMotionTrial.motionCoherence, Is.EqualTo(0.65f));
    //     Assert.That(dotMotionTrial.speed, Is.EqualTo(10f));
    //     Assert.That(dotMotionTrial.experimentSize,
    //                 Is.EqualTo(new Vector2(10, 10)));
    //     Assert.That(dotMotionTrial.canvasManager,
    //                 Is.EqualTo(canvasManager));
    //     Assert.That(dotMotionTrial.scale,
    //                 Is.EqualTo(1f));
    //     Assert.That(dotMotionTrial.dotPrefab,
    //                 Is.EqualTo(dotPrefab));
    // }

    // [TestCase(20, 0.5f)]
    // [TestCase(100, 0.8f)]
    // [TestCase(0, 0f)]
    // public void DotMotionTrial_SetupTrial_CreatesCorrectMotionCoherence(
    //     int numDots, float motionCoherence)
    // {
    //     // Arrange
    //     // Mock canvas manager
    //     var dummyObject = new GameObject("CanvasManager");
    //     var canvasManager = dummyObject.AddComponent<CanvasManager>();
    //     var dotPrefab = new GameObject("TestDot");
    //     var coherentDirection = Vector2.left;

    //     // Act
    //     var dotMotionTrial = new DotMotionTrial(
    //         DotMotionTrial.SignalSelectionRule.Same,
    //         DotMotionTrial.NoiseType.RandomDirection,
    //         DotMotionTrial.ApertureShape.Square,
    //         DotMotionTrial.OutOfBoundsDecision.RandomlyOpposite,
    //         coherentDirection,
    //         DotMotionDot.DotMotionColor.Blue,
    //         numDots,
    //         motionCoherence,
    //         10f,
    //         false,
    //         new Vector2(10, 10),
    //         canvasManager,
    //         1f,
    //         dotPrefab
    //     );

    //     // Assert

    //     // The first motionCoherence percent of the dots need to have
    //     // the same direction.
    //     int actualNumCoherent = dotMotionTrial.dots.Count(dot => dot.direction == coherentDirection);

    //     Assert.That(actualNumCoherent, Is.EqualTo((int)(numDots * motionCoherence)));
    // }


    // TODO: fix unit test due to the revision of the implementation
    // [Test]
    // public void DotMotionTrial_RandomlyOpposite()
    // {
    //     // Arrange
    //     // Mock canvas manager
    //     GameObject dummyObject = new GameObject("CanvasManager");
    //     var canvasManager = dummyObject.AddComponent<CanvasManager>();
    //     GameObject dotPrefab = new GameObject("TestDot");
    //     Vector2 coherentDirection = Vector2.left;
    //     int numDots = 20;
    //     float motionCoherence = 0.5f;
    //     Vector2 experimentSize = new(10, 10);

    //     var dotMotionTrial = new DotMotionTrial(
    //         DotMotionTrial.SignalSelectionRule.Same,
    //         DotMotionTrial.NoiseType.RandomDirection,
    //         DotMotionTrial.ApertureShape.Square,
    //         DotMotionTrial.OutOfBoundsDecision.RandomlyOpposite,
    //         coherentDirection,
    //         DotMotionDot.DotMotionColor.Blue,
    //         numDots,
    //         motionCoherence,
    //         10f,
    //         false,
    //         experimentSize,
    //         canvasManager,
    //         1f,
    //         dotPrefab
    //     );

    //     var positions = new List<Vector2>();

    //     // Regular position
    //     Vector2 pos = Vector2.zero;
    //     positions.Add(pos);

    //     // On upper x edge
    //     pos = new(10, 0);
    //     positions.Add(pos);

    //     // On lower x edge
    //     pos = new(-10, 0);
    //     positions.Add(pos);

    //     // On upper x edge
    //     pos = new(10, 0);
    //     positions.Add(pos);

    //     // On lower x edge
    //     pos = new(-10, 0);
    //     positions.Add(pos);

    //     // On upper y edge
    //     pos = new(0, 10);
    //     positions.Add(pos);

    //     // On lower y edge
    //     pos = new(0, -10);
    //     positions.Add(pos);

    //     // On upper y edge
    //     pos = new(0, 10);
    //     positions.Add(pos);

    //     // On lower y edge
    //     pos = new(0, -10);
    //     positions.Add(pos);

    //     // Above upper x edge
    //     pos = new(11, 0);
    //     positions.Add(pos);

    //     // Below lower x edge
    //     pos = new(-11, 0);
    //     positions.Add(pos);

    //     // Above upper x edge
    //     pos = new(11, 0);
    //     positions.Add(pos);

    //     // Below lower x edge
    //     pos = new(-11, 0);
    //     positions.Add(pos);

    //     // Above upper y edge
    //     pos = new(0, 11);
    //     positions.Add(pos);

    //     // Below lower y edge
    //     pos = new(0, -11);
    //     positions.Add(pos);

    //     // Above upper y edge
    //     pos = new(0, 11);
    //     positions.Add(pos);

    //     // Below lower y edge
    //     pos = new(0, -11);
    //     positions.Add(pos);

    //     // Act
    //     for (int i = 0; i < positions.Count; i++)
    //     {
    //         var (newPosition, wasOutOfBounds) = dotMotionTrial.RandomlyOpposite(positions[i]);
    //         positions[i] = newPosition;
    //     }


    //     // Assert
    //     // Regular
    //     Assert.That(positions[0], Is.EqualTo(Vector2.zero));

    //     // On edges - should not change
    //     Assert.That(positions[1], Is.EqualTo(new Vector2(10, 0)));
    //     Assert.That(positions[2], Is.EqualTo(new Vector2(-10, 0)));
    //     Assert.That(positions[3], Is.EqualTo(new Vector2(10, 0)));
    //     Assert.That(positions[4], Is.EqualTo(new Vector2(-10, 0)));
    //     Assert.That(positions[5], Is.EqualTo(new Vector2(0, 10)));
    //     Assert.That(positions[6], Is.EqualTo(new Vector2(0, -10)));
    //     Assert.That(positions[7], Is.EqualTo(new Vector2(0, 10)));
    //     Assert.That(positions[8], Is.EqualTo(new Vector2(0, -10)));

    //     // Above/below x -> x flipped to -10/10 + offset, y randomized
    //     Assert.That(positions[9].x, Is.EqualTo(-9));
    //     Assert.That(positions[9].y, Is.Not.EqualTo(0));

    //     Assert.That(positions[10].x, Is.EqualTo(9));
    //     Assert.That(positions[10].y, Is.Not.EqualTo(0));

    //     Assert.That(positions[11].x, Is.EqualTo(-9));
    //     Assert.That(positions[11].y, Is.Not.EqualTo(0));

    //     Assert.That(positions[12].x, Is.EqualTo(9));
    //     Assert.That(positions[12].y, Is.Not.EqualTo(0));

    //     // Check for duplicate y (extremely unlikely)
    //     var flippedPositions = positions.GetRange(9, 4).Select(pos => pos.y);
    //     Assert.That(flippedPositions.Count, Is.EqualTo(flippedPositions.Distinct().Count()));

    //     // Above/below y -> y flipped to -10/10 + offset, x randomized
    //     Assert.That(positions[13].y, Is.EqualTo(-9));
    //     Assert.That(positions[13].x, Is.Not.EqualTo(0));

    //     Assert.That(positions[14].y, Is.EqualTo(9));
    //     Assert.That(positions[14].x, Is.Not.EqualTo(0));

    //     Assert.That(positions[15].y, Is.EqualTo(-9));
    //     Assert.That(positions[15].x, Is.Not.EqualTo(0));

    //     Assert.That(positions[16].y, Is.EqualTo(9));
    //     Assert.That(positions[16].x, Is.Not.EqualTo(0));

    //     // Check for duplicate x (extremely unlikely)
    //     flippedPositions = positions.GetRange(13, 4).Select(pos => pos.x);
    //     Assert.That(flippedPositions.Count, Is.EqualTo(flippedPositions.Distinct().Count()));
    // }


    [Test]
    public void DotMotionManager_SimpleTrialAssignsInitialValues()
    {
        // Arrange
        // Mock canvas manager
        GameObject dummyCanvasManager = new GameObject("CanvasManager");
        var canvasManager = dummyCanvasManager.AddComponent<CanvasManager>();

        // Mock dot motion manager
        GameObject dummyDotMotionManager = new GameObject("DotMotionManager");
        var dotMotionManager = dummyDotMotionManager.AddComponent<DotMotionManager>();
        dotMotionManager.dotPrefab = new GameObject("TestDot");

        string direction = "left";
        float motionCoherence = 0.5f;

        // Act
        DotMotionTrial dotMotionTrial = dotMotionManager.SimpleTrial(direction,
            motionCoherence, true);

        // Assert
        Assert.That(dotMotionTrial.numDots, Is.EqualTo(200));
        Assert.That(dotMotionTrial.signalSelectionRule, Is.EqualTo(DotMotionTrial.SignalSelectionRule.Same));
        Assert.That(dotMotionTrial.noiseType, Is.EqualTo(DotMotionTrial.NoiseType.RandomDirection));
        Assert.That(dotMotionTrial.apertureShape, Is.EqualTo(DotMotionTrial.ApertureShape.Square));
        Assert.That(dotMotionTrial.outOfBoundsDecision, Is.EqualTo(DotMotionTrial.OutOfBoundsDecision.RandomlyOpposite));
        Assert.That(dotMotionTrial.coherentDirection, Is.EqualTo(Vector2.left));
        Assert.That(dotMotionTrial.ShowFeedback, Is.True);
        Assert.That(dotMotionTrial.experimentSize, Is.EqualTo(new Vector2(200, 200)));
        Assert.That(dotMotionTrial.dotPrefab, Is.EqualTo(dotMotionManager.dotPrefab));

        // Test all dot's positions for distributional properties.
        var positions = dotMotionTrial.dots.Select(dot => dot.position).ToList();

        foreach (var pos in positions)
        {
            Assert.That(pos.x,
                        Is.InRange(-dotMotionTrial.experimentSize.x,
                                   dotMotionTrial.experimentSize.x));
            Assert.That(pos.y,
                        Is.InRange(-dotMotionTrial.experimentSize.y,
                                   dotMotionTrial.experimentSize.y));
        }

        // Check for duplicates (extremely unlikely, around 10^-10)
        var posX = positions.Select(dot => dot.x).ToList();
        var posY = positions.Select(dot => dot.x).ToList();

        Assert.That(posX.Count, Is.EqualTo(posX.Distinct().Count()));
        Assert.That(posY.Count, Is.EqualTo(posX.Distinct().Count()));
    }
}