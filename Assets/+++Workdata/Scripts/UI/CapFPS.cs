using UnityEngine;

public class CapFPS : MonoBehaviour
{
    void Start()
    {
        Application.targetFrameRate = (int)Screen.currentResolution.refreshRateRatio.value;
    }
}
