using UnityEngine;

public class WaveOrigin: MonoBehaviour
{
    public Material mat;
    public Transform player;

    void Update()
    {
        if (!mat || !player) return;
        mat.SetVector("_WaveOriginWS", player.position);
    }
}
