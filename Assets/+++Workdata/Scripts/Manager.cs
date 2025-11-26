using UnityEngine;
using UnityEngine.SceneManagement;

public class Manager : MonoBehaviour
{
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


    bool isPauseMenuActive = false;
    bool isOptionsMenuActive = false;

    void Start()
    {
        // Cap fps to monitor refresh rate
        Application.targetFrameRate = (int)Screen.currentResolution.refreshRateRatio.value;

        if (doCutscene)
        {
            player.SetActive(false);
            cinemachineCamera.SetActive(true);
            playerCar.GetComponent<CarAnimation>().enabled = true;
            gameplayCanvas.SetActive(false);
            playerCarEngine.gameObject.SetActive(true);
            carLight.SetActive(true);
            //enemy.SetActive(false);
        }
        else
        {
            player.SetActive(true);
            cinemachineCamera.SetActive(false);
            playerCar.GetComponent<CarAnimation>().enabled = false;
            gameplayCanvas.SetActive(true);
            playerCarEngine.gameObject.SetActive(false);
            carLight.SetActive(false);
            //enemy.SetActive(true);
        }
    }

    private void Update()
    {
        HandleInput();

        if (UIManager.Instance.IsUIActive(uiid.PauseScene) || UIManager.Instance.IsUIActive(uiid.OptionsScene))
        {
            SetCursorState(false);
        }
        else if (!UIManager.Instance.IsUIActive(uiid.PauseScene) && !UIManager.Instance.IsUIActive(uiid.OptionsScene))
        {
            SetCursorState(true);
        }
    }

    private void HandleInput()
    {
        // Reload scene
        if (Input.GetKeyDown(KeyCode.R))
        {
            ReloadScene();
        }

        // Unfocus window
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (UIManager.Instance.IsUIActive(uiid.PauseScene) || UIManager.Instance.IsUIActive(uiid.OptionsScene))
            {
                UIManager.Instance.HideUI(uiid.PauseScene);
                UIManager.Instance.HideUI(uiid.OptionsScene);

                return;
            }

            UIManager.Instance.ShowUI(uiid.PauseScene);

            //SetCursorState(false);
        }

        //// Re-focus window
        //if (Input.GetMouseButtonDown(0) && !isFocused)
        //{
        //    SetCursorState(true);
        //}

        // Skip cutscene
        if (Input.GetKeyDown(KeyCode.Tab) && doCutscene)
        {
            playerCar.GetComponent<CarAnimation>().TeleportToEnd();
            EnableGameplay();
        }

    }

    public void EnableGameplay()
    {
        player.SetActive(true);
        cinemachineCamera.SetActive(false);
        gameplayCanvas.SetActive(true);
        playerCarEngine.gameObject.SetActive(false);
        carLight.SetActive(false);
        //enemy.SetActive(true);
    }

    private void SetCursorState(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;

        //Time.timeScale = locked ? 1f : 0f;

    }

    private void ReloadScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}