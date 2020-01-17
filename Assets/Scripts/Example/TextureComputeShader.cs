using System.Collections;
using UnityEngine;

public class TextureComputeShader : MonoBehaviour {
    [SerializeField]
    GameObject m_planeX;
    [SerializeField]
    GameObject m_planeY;
    [SerializeField]
    ComputeShader m_computeShader;
    [SerializeField, Range (6, 12)]
    int m_resolution;

    RenderTexture m_renderTextureX;
    RenderTexture m_renderTextureY;
    int m_kernelIdxX;
    int m_kernelIdxY;

    struct ThreadSize {
        public int x;
        public int y;
        public int z;
        public ThreadSize (uint x, uint y, uint z) {
            this.x = (int) x;
            this.y = (int) y;
            this.z = (int) z;
        }
    }

    ThreadSize m_kernelThreadSizeX;
    ThreadSize m_kernelThreadSizeY;

    void Start () {
        // Reserve render textures
        int reso = (int) Mathf.Pow (2, m_resolution);
        m_renderTextureX = new RenderTexture (reso, reso, 0, RenderTextureFormat.ARGB32);
        m_renderTextureY = new RenderTexture (reso, reso, 0, RenderTextureFormat.ARGB32);
        m_renderTextureX.enableRandomWrite = true;
        m_renderTextureY.enableRandomWrite = true;
        m_renderTextureX.Create ();
        m_renderTextureY.Create ();

        // Read kernel indices and thread sizes
        m_kernelIdxX = m_computeShader.FindKernel ("KernelFuncX");
        m_kernelIdxY = m_computeShader.FindKernel ("KernelFuncY");

        uint threadSizeX, threadSizeY, threadSizeZ;
        m_computeShader.GetKernelThreadGroupSizes (m_kernelIdxX, out threadSizeX, out threadSizeY, out threadSizeZ);
        m_kernelThreadSizeX = new ThreadSize (threadSizeX, threadSizeY, threadSizeZ);
        m_computeShader.GetKernelThreadGroupSizes (m_kernelIdxY, out threadSizeX, out threadSizeY, out threadSizeZ);
        m_kernelThreadSizeY = new ThreadSize (threadSizeX, threadSizeY, threadSizeZ);

        m_computeShader.SetTexture (m_kernelIdxX, "textureBuffer", m_renderTextureX);
        m_computeShader.SetTexture (m_kernelIdxY, "textureBuffer", m_renderTextureY);
    }

    void Update () {
        m_computeShader.SetFloat ("rand", Random.Range (.0f, .02f));
        m_computeShader.Dispatch (m_kernelIdxX,
            m_renderTextureX.width / m_kernelThreadSizeX.x,
            m_renderTextureX.height / m_kernelThreadSizeX.y,
            m_kernelThreadSizeX.z);
        m_computeShader.Dispatch (m_kernelIdxY,
            m_renderTextureY.width / m_kernelThreadSizeY.x,
            m_renderTextureY.height / m_kernelThreadSizeY.y,
            m_kernelThreadSizeY.z);
        m_planeX.GetComponent<Renderer> ().material.mainTexture = m_renderTextureX;
        m_planeY.GetComponent<Renderer> ().material.mainTexture = m_renderTextureY;
    }

}
