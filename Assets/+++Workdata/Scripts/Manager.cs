using UnityEngine;

public class Manager : MonoBehaviour
{
    void Start()
    {
        //cap fps to monitor refresh rate
        Application.targetFrameRate = (int)Screen.currentResolution.refreshRateRatio.value;
    }
}
