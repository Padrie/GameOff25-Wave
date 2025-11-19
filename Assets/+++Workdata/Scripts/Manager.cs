using UnityEngine;
using UnityEngine.SceneManagement;

public class Manager : MonoBehaviour
{
    void Start()
    {
        //cap fps to monitor refresh rate
        Application.targetFrameRate = (int)Screen.currentResolution.refreshRateRatio.value;
    }
    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}
