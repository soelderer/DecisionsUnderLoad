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

using UnityEngine;
using System.Collections.Generic;
using System.Threading;

namespace UXF
{
    /// <summary>
    /// Tracks the position of the haptic touch device in a dedicated thread.
    /// </summary>
    public class RealTimePositionTracker : PositionRotationTracker
    {
        public override string MeasurementDescriptor => "movement";

        public override IEnumerable<string> CustomHeader => new string[] {
            "device_pos_x", "device_pos_y", "device_pos_z", "device_rot_x",
            "device_rot_y", "device_rot_z", "event" };

        // Assign in inspector.
        public TrialManager trialManager;
        public TouchManager touchManager;

        // Things for multi-threading
        private Thread trackerThread;
        private volatile bool trackerThreadRunning = false;
        System.Diagnostics.Stopwatch stopwatch;

        void Awake()
        {
            updateType = TrackerUpdateType.Manual;
        }

        void OnDisable()
        {
            if (trackerThreadRunning)
                StopThread();
        }

        /// <summary>
        /// Returns current position and rotation values of the haptic touch.
        /// </summary>
        /// <returns></returns>
        protected override UXFDataRow GetCurrentValues()
        {
            double realTime = stopwatch.Elapsed.TotalSeconds;

            // Get position and rotation (eulerAngles) of the touch device
            Vector3 device_pos, device_rot;

            (device_pos, device_rot) = touchManager.GetPositionAndRotationFromAPI();

            // Return position, rotation (x, y, z) as an array
            var values = new UXFDataRow()
            {
                ("time", realTime), // we will use our own realTime and not
                                    // Unity's Time.time
                ("device_pos_x", device_pos.x),
                ("device_pos_y", device_pos.y),
                ("device_pos_z", device_pos.z),
                ("device_rot_x", device_rot.x),
                ("device_rot_y", device_rot.y),
                ("device_rot_z", device_rot.z),
                ("event", trialManager.eventForTracker)
            };

            return values;
            }

        // Records a row if the tracker is currently recording and
        // UpdateType is Manual.
        public void RecordRowIfRecording()
        {
            if (Recording && updateType == TrackerUpdateType.Manual) RecordRow();
        }

        public void ThreadLoop()
        {
            // Stopwatch helps to maintain consistent sampling rate
            // because Thread.Sleep() does not guarantee constant sleep time.
            stopwatch.Start();
            const double interval = 1.0 / 1000.0; // 1kHz
            double nextTime = stopwatch.Elapsed.TotalSeconds;

            while (trackerThreadRunning)
            {
                nextTime += interval;

                double now = stopwatch.Elapsed.TotalSeconds;
                double sleepTime = nextTime - now;

                // Thread.Sleep() is quite coarse, around 10ms up or down.
                // => Sleep only if we're more than 1ms early
                if (sleepTime > 0.001)
                    Thread.Sleep((int)((sleepTime - 0.0005) * 1000)); // Sleep a bit less

                // ...and then busy-wait until exact target time
                while (stopwatch.Elapsed.TotalSeconds < nextTime)
                    Thread.SpinWait(10);

                RecordRowIfRecording();
            }
        }

        public void StartThread()
        {
            trackerThread = new Thread(ThreadLoop);
            trackerThread.IsBackground = true;

            stopwatch = new System.Diagnostics.Stopwatch();

            Debug.Log("[RealTimePositionTracker] Started dedicated tracking thread.");

            trackerThreadRunning = true;
            trackerThread.Start();
        }

        public void StopThread()
        {
            if (trackerThread.IsAlive)
            {
                Debug.Log("[RealTimePositionTracker] Sent stop signal to tracking thread.");

                trackerThreadRunning = false;
                trackerThread.Join();

                Debug.Log("[RealTimePositionTracker] Tracking thread stopped.");
            }
            else
            {
                Debug.Log("[RealTimePositionTracker] Attempted to stop tracking thread, but it is not running.");
            }
        }
    }
}