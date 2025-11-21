using UnityEngine;
using UnityEngine.SceneManagement;

public class Manager : MonoBehaviour
{
    private bool isFocused = true;

    [SerializeField]
    private bool doCutscene = true;

    [SerializeField]
    private GameObject player;

    [SerializeField]
    private GameObject cinemachineCamera;

    [SerializeField]
    private GameObject gameplayCanvas;

    [SerializeField] 
    private GameObject carLight;

    [SerializeField]
    private GameObject playerCar;

    [SerializeField]
    private AudioSource playerCarEngine;

    [SerializeField]
    private GameObject enemy;

    void Start()
    {
        // Cap fps to monitor refresh rate
        Application.targetFrameRate = (int)Screen.currentResolution.refreshRateRatio.value;
        SetCursorState(true);


        if(doCutscene)
        {
            player.SetActive(false);
            cinemachineCamera.SetActive(true);
            playerCar.GetComponent<CarAnimation>().enabled = true;
            gameplayCanvas.SetActive(false);
            playerCarEngine.enabled = true;
            carLight.SetActive(true);
            enemy.SetActive(false);
        }
        else
        {
            player.SetActive(true);
            cinemachineCamera.SetActive(false);
            playerCar.GetComponent<CarAnimation>().enabled = false;
            gameplayCanvas.SetActive(true);
            playerCarEngine.enabled = false;
            carLight.SetActive(false);
            enemy.SetActive(true);
        }
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

        // Skip cutscene
        if (Input.GetKeyDown(KeyCode.Tab) && doCutscene)
        {
            SkipCutscene();
        }

    }

    private void SkipCutscene()
    {
        player.SetActive(true);
        cinemachineCamera.SetActive(false);
        playerCar.GetComponent<CarAnimation>().TeleportToEnd();
        gameplayCanvas.SetActive(true);
        playerCarEngine.enabled = false;
        carLight.SetActive(false);
        enemy.SetActive(true);
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