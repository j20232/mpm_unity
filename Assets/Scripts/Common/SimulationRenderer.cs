using System.Collections;
using UnityEngine;
using Unity.Collections;

public class SimulationRenderer : MonoBehaviour
{
    [SerializeField] Mesh m_instanceMesh;
    [SerializeField] Material m_instanceMaterial;

    ComputeBuffer m_pointBuffer;
    ComputeBuffer m_argsBuffer;

    uint[] m_args = new uint[5] { 0, 0, 0, 0, 0 };

    Bounds m_bounds;

    public void Initialize(int bufferSize, int bufferElementSize)
    {
        m_pointBuffer = new ComputeBuffer(bufferSize, bufferElementSize, ComputeBufferType.Default);
        m_instanceMaterial.SetBuffer("particle_buffer", m_pointBuffer);

        m_argsBuffer = new ComputeBuffer(1, m_args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        m_args[0] = (uint)m_instanceMesh.GetIndexCount(0); // number of indices
        m_args[1] = (uint)m_pointBuffer.count;
        m_argsBuffer.SetData(m_args);

        m_bounds = new Bounds(Vector3.zero, new Vector3(100, 100, 100));
    }

    public void RenderFrame<T>(NativeArray<T> ps) where T : struct
    {
        m_pointBuffer.SetData(ps);
        Graphics.DrawMeshInstancedIndirect(m_instanceMesh, 0, m_instanceMaterial, m_bounds, m_argsBuffer);
    }

    void OnDisable()
    {
        if (m_argsBuffer != null) m_argsBuffer.Release();
        if (m_pointBuffer != null) m_pointBuffer.Release();
    }
}
