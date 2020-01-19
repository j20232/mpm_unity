using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;
using UnityEngine.Profiling;

public class NeoHookean : MonoBehaviour {
    struct Particle {
        public float2 x;
        public float2 v;
        public float2x2 C; // affine momentum matrix
        public float mass;
        public float volume_0; // initial volume
    }

    struct Cell {
        public float2 v;
        public float mass;
    }

    [SerializeField]
    int m_gridResolution = 64;
    int m_numCells;

    [SerializeField]
    int m_batchSize = 16;

    // Simulation parameters
    [SerializeField, Range (0.1f, 2.0f)]
    float m_dt = 0.1f;
    int m_iterations;
    [SerializeField, Range (-1.0f, -0.05f)]
    float m_gravity = -0.05f;
    int m_numParticles;

    // Lame parameters
    [SerializeField, Range (5.0f, 15.0f)]
    float m_elasticLambda = 10.0f;
    [SerializeField, Range (15.0f, 25.0f)]
    float m_elasticMu = 20.0f;

    NativeArray<Particle> m_particles;
    NativeArray<Cell> m_grid;
    NativeArray<float2x2> m_Fs; // deformation gradient

    float2[] m_weights = new float2[3];

    [SerializeField]
    SimulationRenderer m_simulationRenderer;

    // Mouse
    [SerializeField, Range (1.0f, 15.0f)]
    float m_mouseRadius = 10.0f;
    [SerializeField]
    bool m_mouseDown = false;
    float2 m_mousePos;

    void Start () {
        m_numCells = m_gridResolution * m_gridResolution;
        m_iterations = (int) (1.0f / m_dt);

        // 1. Initialize the grid by filling the grid array with res x res cells
        m_grid = new NativeArray<Cell> (m_numCells, Allocator.Persistent);
        for (int i = 0; i < m_numCells; i++) {
            var c = new Cell ();
            c.v = 0;
            m_grid[i] = c;
        }

        // 2. Create a bunch of particles and set their positions somewhere
        List<float2> tempPositions = new List<float2> ();
        const float spacing = 1.0f;
        int boxX = m_gridResolution / 2;
        int boxY = m_gridResolution / 2;
        float sx = m_gridResolution / 2.0f;
        float sy = m_gridResolution / 2.0f;
        for (float i = sx - boxX / 2; i < sx + boxX / 2; i += spacing) {
            for (float j = sy - boxY / 2; j < sy + boxY / 2; j += spacing) {
                var pos = math.float2 (i, j);
                tempPositions.Add (pos);
            }
        }
        m_numParticles = tempPositions.Count;

        m_particles = new NativeArray<Particle> (m_numParticles, Allocator.Persistent);
        m_Fs = new NativeArray<float2x2> (m_numParticles, Allocator.Persistent);
        for (int i = 0; i < m_numParticles; i++) {
            Particle p = new Particle ();
            p.x = tempPositions[i];
            p.v = 0;
            p.C = 0;
            p.mass = 1.0f;
            m_particles[i] = p;

            // Initialize the deformation gradient to the identity
            m_Fs[i] = math.float2x2 (1, 0, 0, 1);
        }

        // Launch a job to scatter the particle mass to the grid
        // TODO: write here

        //  Precomuptation of particle volumes: mpm course eq.152
        for (int i = 0; i < m_numParticles; i++) {
            var p = m_particles[i];

            float2 cell_idx = math.floor (p.x);
            float2 cell_diff = (p.x - cell_idx) - 0.5f;
            m_weights[0] = 0.5f * math.pow (0.5f - cell_diff, 2);
            m_weights[1] = 0.75f - math.pow (cell_diff, 2);
            m_weights[2] = 0.5f * math.pow (0.5f + cell_diff, 2);

            float density = 0.0f;
            for (int gx = 0; gx < 3; gx++) {
                for (int gy = 0; gy < 3; gy++) {
                    float weight = m_weights[gx].x * m_weights[gy].y;
                    int cell_index = ((int) cell_idx.x + (gx - 1)) * m_gridResolution + ((int) cell_idx.y + gy - 1);
                    density += m_grid[cell_index].mass * weight;
                }
            }
            p.volume_0 = p.mass / density;
            m_particles[i] = p;
        }

        m_simulationRenderer.Initialize (m_numParticles, Marshal.SizeOf (new Particle ()));
    }

    // Update is called once per frame
    void Update () {
        HandleMouseInteraction ();
        for (int i = 0; i < m_iterations; i++) Simulate ();
        m_simulationRenderer.RenderFrame (m_particles);
    }

    void HandleMouseInteraction () {
        m_mouseDown = false;
        if (Input.GetMouseButton (0)) {
            m_mouseDown = true;
            var mp = Camera.main.ScreenToViewportPoint (Input.mousePosition);
            m_mousePos = math.float2 (mp.x * m_gridResolution, mp.y * m_gridResolution);
        }
    }

    void Simulate () {
        // TODO: write here
    }

    private void OnDestroy () {
        m_particles.Dispose ();
        m_grid.Dispose ();
    }
}
