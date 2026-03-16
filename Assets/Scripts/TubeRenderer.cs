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
using UnityEngine.Profiling;

/// <summary>
/// Generates and applies the continuous trajectories.
/// </summary>
/// <remarks>
/// The cubic splines are already pre-fitted with a Python script that yields
/// the coefficients (see Python/polyfit.ipynb). If you want to change the
/// trajectories, adjust the coordinates in polyfit.ipynb and then copy-paste
/// the coefficients here.
/// </remarks>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class TubeRenderer : MonoBehaviour
{
    public int points = 2000;
    public float radius = 0.001f;
    public int radialDivs = 8;

    public Material tubeLight;
    public Material tubeDark;
    
    private Dictionary<int, Mesh> mesh = new Dictionary<int, Mesh>();

    void Start()
    {
        // Pre-generate meshes for the trajectories (1 = easy, 2 = hard).
        // TODO: refactor the trajectories with enum
        mesh[1] = GenerateTube(1);
        mesh[2] = GenerateTube(2);
    }

    public void Show()
    {
        GetComponent<MeshRenderer>().enabled = true;
    }

    public void Hide()
    {
        GetComponent<MeshRenderer>().enabled = false;
    }

    public void SetMaterial(Material material)
    {
        Renderer renderer = GetComponent<Renderer>();
        renderer.material = material;
    }

    // Calculate Y coordinates based on the cubic spline (coefficients are
    // generated in Python with Python/polyfit.ipynb).
    // Easy trajectory (1)
    float CalculateY1(float x)
    {
        if (x < -0.22f || x > 0.22f) return 0f;

        if (x < -0.11f)
        {
            float dx = x - (-0.22f);
            return -272.351615f * dx * dx * dx + 44.008264f * dx * dx + 0.0f * dx + 0.030000f;
        }
        else if (x < 0.0f)
        {
            float dx = x - (-0.11f);
            return 283.621337f * dx * dx * dx - 45.867769f * dx * dx - 0.204545f * dx + 0.200000f;
        }
        else if (x < 0.11f)
        {
            float dx = x - 0.0f;
            return -283.621337f * dx * dx * dx + 47.727273f * dx * dx + 0.0f * dx + 0.000000f;
        }
        else
        {
            float dx = x - 0.11f;
            return 272.351615f * dx * dx * dx - 45.867769f * dx * dx + 0.204545f * dx + 0.200000f;
        }
    }

    // Calculate Y coordinates based on the cubic spline (coefficients are
    // generated in Python with Python/polyfit.ipynb).
    // Difficult trajectory (2)
    float CalculateY2(float x)
    {
        if (x < -0.22f || x > 0.22f) return 0f;

        if (x < -0.165f)
        {
            float dx = x - (-0.22f);
            return -1963.078244f * dx * dx * dx + 164.167651f * dx * dx + 0.0f * dx + 0.030000f;
        }
        else if (x < -0.11f)
        {
            float dx = x - (-0.165f);
            return 1922.292583f * dx * dx * dx - 159.740260f * dx * dx + 0.243506f * dx + 0.200000f;
        }
        else if (x < -0.055f)
        {
            float dx = x - (-0.11f);
            return -1999.570677f * dx * dx * dx + 157.438017f * dx * dx + 0.116883f * dx + 0.050000f;
        }
        else if (x < 0.0f)
        {
            float dx = x - (-0.055f);
            return 2169.153161f * dx * dx * dx - 172.491145f * dx * dx - 0.711039f * dx + 0.200000f;
        }
        else if (x < 0.055f)
        {
            float dx = x - 0.0f;
            return -2169.153161f * dx * dx * dx + 185.419126f * dx * dx + 0.0f * dx + 0.000000f;
        }
        else if (x < 0.11f)
        {
            float dx = x - 0.055f;
            return 1999.570677f * dx * dx * dx - 172.491145f * dx * dx + 0.711039f * dx + 0.200000f;
        }
        else if (x < 0.165f)
        {
            float dx = x - 0.11f;
            return -1922.292583f * dx * dx * dx + 157.438017f * dx * dx - 0.116883f * dx + 0.050000f;
        }
        else
        {
            float dx = x - 0.165f;
            return 1963.078244f * dx * dx * dx - 159.740260f * dx * dx - 0.243506f * dx + 0.200000f;
        }
    }

    /// <summary>
    /// Returns the minimal distance of a point to the trajectory.
    /// </summary>
    /// <param name="position"></param>
    /// <param name="trajectory"></param>
    /// <returns></returns>
    public float DistanceToTrajectory(Vector3 position, int trajectory)
    {
        float minDist = float.MaxValue;

        float z = transform.position.z;
        
        for (float x = -0.22f; x <= 0.22f; x += 0.001f)
        {
            float y = 0f;

            if (trajectory == 1)
                y = CalculateY1(x);
            else if (trajectory == 2)
                y = CalculateY2(x);

            float dist = Vector3.Distance(position, new Vector3(x, y, z));

            if (dist < minDist) minDist = dist;
        }
        return minDist;
    }

    /// <summary>
    /// Generates and returns the tube of a trajectory.
    /// </summary>
    /// <param name="trajectory"></param>
    /// <returns></returns>
    Mesh GenerateTube(int trajectory)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector3> normals = new List<Vector3>();

        // Generate points along -cosine wave path (symmetrical around 0)
        List<Vector3> pathPoints = new List<Vector3>();
        float totalLength = points * 0.0002f;
        float startX = -totalLength / 2f;

        for (int i = 0; i < points; i++)
        {
            float x = startX + i * 0.0002f;

            float y = 0;

            // Calculate Y coordinates depending on the type of trajectory (1 or 2 ~ easy or hard)
            if (trajectory == 1)
                y = CalculateY1(x);
            else if (trajectory == 2)
                y = CalculateY2(x);
            else
                Debug.LogError("[TubeRenderer] Invalid trajectory");

            pathPoints.Add(new Vector3(x, y, 0));
        }

        // Build tube vertices & normals
        for (int i = 0; i < points; i++)
        {
            Vector3 center = pathPoints[i];

            Vector3 forward;
            if (i < points - 1)
                forward = (pathPoints[i + 1] - center).normalized;
            else
                forward = (center - pathPoints[i - 1]).normalized;

            Vector3 up = Vector3.up;
            Vector3 right = Vector3.Cross(forward, up).normalized;
            up = Vector3.Cross(right, forward).normalized;

            for (int j = 0; j < radialDivs; j++)
            {
                float angle = (j / (float)radialDivs) * Mathf.PI * 2f;
                Vector3 offset = Mathf.Cos(angle) * right + Mathf.Sin(angle) * up;
                vertices.Add(center + offset * radius);
                normals.Add(offset);
            }
        }

        // Build triangles (using corrected indices)
        for (int i = 0; i < points - 1; i++)
        {
            for (int j = 0; j < radialDivs; j++)
            {
                int curr = i * radialDivs + j;
                int next = curr + radialDivs;
                int nextJ = (j + 1) % radialDivs;

                triangles.Add(curr);
                triangles.Add(next);
                triangles.Add(i * radialDivs + nextJ);

                triangles.Add(i * radialDivs + nextJ);
                triangles.Add(next);
                triangles.Add(next - j + nextJ);
            }
        }

        Mesh mesh = new Mesh();
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetNormals(normals);
        mesh.RecalculateBounds();

        return mesh;
    }

    /// <summary>
    /// Applies a tube to the MeshFilter.
    /// </summary>
    /// <param name="trajectory"></param>
    public void ApplyTube(int trajectory)
    {
        Profiler.BeginSample("GenerateTube");
        MeshFilter mf = GetComponent<MeshFilter>();
        mf.mesh = mesh[trajectory];
        Profiler.EndSample();
    }
}
