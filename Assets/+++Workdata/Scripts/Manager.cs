using UnityEngine;
using UnityEngine.SceneManagement;

public class Manager : MonoBehaviour
{
    private bool isFocused = true;

    void Start()
    {
        // Cap fps to monitor refresh rate
        Application.targetFrameRate = (int)Screen.currentResolution.refreshRateRatio.value;
        SetCursorState(true);
    }

    private void Update()
    {
        HandleInput();
    }

    private void HandleInput()
    {
        // Reload scene
        if (Input.GetKeyDown(KeyCode.R))
        {
            ReloadScene();
        }

        // Unfocus window
        if (Input.GetKeyDown(KeyCode.Escape) && isFocused)
        {
            SetCursorState(false);
        }

        // Re-focus window
        if (Input.GetMouseButtonDown(0) && !isFocused)
        {
            SetCursorState(true);
        }
    }

    private void SetCursorState(bool locked)
    {
        isFocused = locked;
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;

        Time.timeScale = locked ? 1f : 0f;
    }

    private void ReloadScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus && isFocused)
        {
            SetCursorState(true);
        }
    }
}