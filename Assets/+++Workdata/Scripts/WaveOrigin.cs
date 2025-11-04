using UnityEngine;

public class WaveOrigin: MonoBehaviour
{
    public Renderer targetRenderer;
    public Transform player;
    static readonly int WaveOriginWS = Shader.PropertyToID("_WaveOriginWS");
    MaterialPropertyBlock mpb;

    void Awake() => mpb = new MaterialPropertyBlock();

    void LateUpdate()
    {
        targetRenderer.GetPropertyBlock(mpb);
        mpb.SetVector(WaveOriginWS, player.position);
        targetRenderer.SetPropertyBlock(mpb);
    }
}
