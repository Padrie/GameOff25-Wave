using UnityEngine;

[ExecuteAlways]
public class WaveHeightTuner : MonoBehaviour
{
    public float heightFactor = 0.5f;
    static readonly int HeightFactorID = Shader.PropertyToID("_HeightFactor");

    void OnEnable() { Shader.SetGlobalFloat(HeightFactorID, heightFactor); }
    void Update() { Shader.SetGlobalFloat(HeightFactorID, heightFactor); }
}
