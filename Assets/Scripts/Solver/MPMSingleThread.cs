using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;
using UnityEngine.Profiling;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Random = UnityEngine.Random;

public class MPMSingleThread : MonoBehaviour
{
    struct Particle
    {
        public float2 x;
        public float2 v;
        public float2x2 C;
        public float mass;
        public float padding;
    }

    struct Cell
    {
        public float2 v;
        public float mass;
        public float padding;
    }

    [SerializeField]
    int m_gridResolution = 32;
    int m_numCells;

    [SerializeField]
    float m_dt = 1.0f; // time step
    int m_iterations;

    static float m_gravity = -0.05f;
    int m_numParticles;

    NativeArray<Particle> m_particles;
    NativeArray<Cell> m_grid;

    float2[] m_weights = new float2[3];

    [SerializeField]
    SimulationRenderer m_simulationRenderer;

    [SerializeField]
    float m_mouseRadius = 10.0f;

    [SerializeField]
    bool m_mouseDown = false;

    float2 m_mousePos;


    // Start is called before the first frame update
    void Start()
    {
        m_numCells = m_gridResolution * m_gridResolution;
        m_iterations = (int)(1.0f / m_dt);

        // 1. Initialize the grid by filling the grid array with res x res cells
        m_grid = new NativeArray<Cell>(m_numCells, Allocator.Persistent);
        for (int i = 0; i < m_numCells; i++)
        {
            m_grid[i] = new Cell();
        }

        // 2. Create a bunch of particles and set their positions somewhere
        List<float2> tempPositions = new List<float2>();
        const float spacing = 1.0f;
        int boxX = m_gridResolution / 4;
        int boxY = m_gridResolution / 4;
        float sx = m_gridResolution / 2.0f;
        float sy = m_gridResolution / 2.0f;
        for (float i = sx - boxX / 2; i < sx + boxX / 2; i += spacing)
        {
            for (float j = sy - boxY / 2; j < sy + boxY / 2; j += spacing)
            {
                var pos = math.float2(i, j);
                tempPositions.Add(pos);
            }
        }
        m_numParticles = tempPositions.Count;

        m_particles = new NativeArray<Particle>(m_numParticles, Allocator.Persistent);
        for (int i = 0; i < m_numParticles; i++)
        {
            Particle p = new Particle();
            p.x = tempPositions[i];
            p.v = math.float2(Random.value - 0.5f, Random.value - 0.5f + 2.75f) * 0.5f;
            p.C = 0;
            p.mass = 1.0f;
            m_particles[i] = p;
        }

        m_simulationRenderer.Initialize(m_numParticles, Marshal.SizeOf(new Particle()));
    }

    void Update()
    {
        HandleMouseInteraction();
        for (int i = 0; i < m_iterations; i++)
        {
            Simulate();
        }
        m_simulationRenderer.RenderFrame(m_particles);
    }

    void HandleMouseInteraction()
    {
        m_mouseDown = false;
        if (Input.GetMouseButton(0))
        {
            m_mouseDown = true;
            var mp = Camera.main.ScreenToViewportPoint(Input.mousePosition);
            m_mousePos = math.float2(mp.x * m_gridResolution, mp.y * m_gridResolution);
        }
    }

    void Simulate()
    {
        // 1. reset scratch-pad grid
        for (int i = 0; i < m_numCells; i++)
        {
            var cell = m_grid[i];
            cell.mass = 0;
            cell.v = 0;
            m_grid[i] = cell;
        }

        // 2. particle-to-grid
        for (int i = 0; i < m_numParticles; i++)
        {
            var p = m_particles[i];

            // Calculate quadratic kernel (see equation (123))
            uint2 cell_idx = (uint2)p.x;
            float2 cell_diff = (p.x - cell_idx) - 0.5f;
            m_weights[0] = 0.5f * math.pow(0.5f - cell_diff, 2);
            m_weights[1] = 0.75f - math.pow(cell_diff, 2);
            m_weights[2] = 0.5f * math.pow(0.5f + cell_diff, 2);

            // 2.1 calculate weight for the 3x3 neighbouring cells surrounding the particle's position
            // on the grid using an interpolation function
            for (uint gx = 0; gx < 3; gx++)
            {
                for (uint gy = 0; gy < 3; gy++)
                {
                    float weight = m_weights[gx].x * m_weights[gy].y;

                    uint2 cell_x = math.uint2(cell_idx.x + gx - 1, cell_idx.y + gy - 1);
                    float2 cell_dist = (cell_x - p.x) + 0.5f;
                    float2 Q = math.mul(p.C, cell_dist);

                    // 2.2 calculate quantities like stress (see equation (172))
                    float mass_contrib = weight * p.mass;

                    // Convert 2D index to 1D
                    int cell_index = (int)cell_x.x * m_gridResolution + (int)cell_x.y;
                    Cell cell = m_grid[cell_index];

                    // 2.3 scatter particles' momentum to the grid
                    cell.mass += mass_contrib;
                    cell.v += mass_contrib * (p.v + Q); // Note: v is momentum
                    m_grid[cell_index] = cell;
                }
            }
        }

        // 3. calculate grid velocities
        for (int i = 0; i < m_numCells; i++)
        {
            var cell = m_grid[i];
            if (cell.mass > 0)
            {
                // convert momentum to velocity
                cell.v /= cell.mass;

                //apply gravity
                cell.v += m_dt * math.float2(0, m_gravity);

                // boundary conditions
                int x = i / m_gridResolution;
                int y = i % m_gridResolution;
                if (x < 2 || x > m_gridResolution - 3) { cell.v.x = 0; }
                if (y < 2 || y > m_gridResolution - 3) { cell.v.y = 0; }

            }
            m_grid[i] = cell;
        }

        // 4. grid-to-particle
        for (int i = 0; i < m_numParticles; i++)
        {
            var p = m_particles[i];

            // reset particle velocity
            p.v = 0;

            // quadratic interpolation weights
            uint2 cell_idx = (uint2)p.x;
            float2 cell_diff = (p.x - cell_idx) - 0.5f;
            m_weights[0] = 0.5f * math.pow(0.5f - cell_diff, 2);
            m_weights[1] = 0.75f - math.pow(cell_diff, 2);
            m_weights[2] = 0.5f * math.pow(0.5f + cell_diff, 2);

            // construct affine per-particle momentum matrix from APIC
            float2x2 B = 0;
            for (uint gx = 0; gx < 3; gx++)
            {
                for (uint gy = 0; gy < 3; gy++)
                {
                    float weight = m_weights[gx].x * m_weights[gy].y;

                    uint2 cell_x = math.uint2(cell_idx.x + gx - 1, cell_idx.y + gy - 1);
                    int cell_index = (int)cell_x.x * m_gridResolution + (int)cell_x.y;

                    float2 dist = (cell_x - p.x) + 0.5f;
                    float2 weighted_velocity = m_grid[cell_index].v * weight;

                    // APIC paper's equation (10)
                    var term = math.float2x2(weighted_velocity * dist.x, weighted_velocity * dist.y);

                    // calculate new particle velocities
                    B += term;
                    p.v += weighted_velocity;
                }
            }
            p.C = B * 4;

            // advect particle positions
            p.x += p.v * m_dt;
            p.x = math.clamp(p.x, 1, m_gridResolution - 2);

            if (m_mouseDown)
            {
                var dist = p.x - m_mousePos;
                if (math.dot(dist, dist) < m_mouseRadius * m_mouseRadius)
                {
                    float norm_factor = (math.length(dist) / m_mouseRadius);
                    norm_factor = math.pow(math.sqrt(norm_factor), 8);
                    var force = math.normalize(dist) * norm_factor * 0.5f;
                    p.v += force;
                }
            }
            m_particles[i] = p;
        }
    }

    private void OnDestroy()
    {
        m_particles.Dispose();
        m_grid.Dispose();
    }
}
