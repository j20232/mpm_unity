using UnityEngine;

public class TinyComputeShader : MonoBehaviour {
    [SerializeField]
    ComputeShader m_computeShader;
    ComputeBuffer m_computeBuffer;
    int m_kernelIdxMul;
    void Start () {
        m_kernelIdxMul = m_computeShader.FindKernel ("MulKernel");
        m_computeBuffer = new ComputeBuffer (4, sizeof (int));
        m_computeShader.SetBuffer (m_kernelIdxMul, "intBuffer", m_computeBuffer);
        m_computeShader.SetInt ("intBuffer", 1);
        m_computeShader.Dispatch (m_kernelIdxMul, 1, 1, 1);
        int[] result = new int[4];
        m_computeBuffer.GetData (result);
        foreach (var val in result) Debug.Log (val);
        m_computeBuffer.Release ();
    }
}
