using EasyPeasyFirstPersonController;
using System;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class Manager : MonoBehaviour
{
    [SerializeField]
    private bool doCutscene = true;


    [SerializeField] private GameObject playerCar;

    bool isPauseMenuActive = false;
    bool isOptionsMenuActive = false;


    private LightSaver lightSaver;


    [Space(10)]
    public UnityEvent CallCutsceneBegins;
    public static event Action CutsceneBegins;

    [Space(10)]
    public UnityEvent CallAfterCarCutscene;
    public static event Action AferCarCutscene;

    FirstPersonController player;


    private void Awake()
    {
        lightSaver = FindFirstObjectByType<LightSaver>();
        player = FindFirstObjectByType<FirstPersonController>();
    }


    void Start()
    {
#if Unity_Editor
        // Cap fps to monitor refresh rate
        Application.targetFrameRate = (int)Screen.currentResolution.refreshRateRatio.value;
#endif

        if (doCutscene || !Application.isEditor)
        {
            CallCutsceneBegins?.Invoke();
            CutsceneBegins?.Invoke();

            // Ensure player controller is disabled while cutscene runs so its Update won't execute
            if (player != null)
            {
                player.enabled = false;
            }
        }
        else
        {
            CallAfterCarCutscene?.Invoke();
            AferCarCutscene?.Invoke();

            // Ensure player controller is enabled when not running a cutscene
            if (player != null)
            {
                player.enabled = true;
            }
        }
    }


    private void Update()
    {
        HandleInput();

        if (UIManager.Instance != null)
        {
            if (UIManager.Instance.IsUIActive(uiid.DeathScreenScene))
            {
                SetCursorState(false);
                UIManager.Instance.HideUI(uiid.OptionsScene);
                UIManager.Instance.HideUI(uiid.PauseScene);
            }
            else if (UIManager.Instance.IsUIActive(uiid.PauseScene) || UIManager.Instance.IsUIActive(uiid.OptionsScene))
            {
                SetCursorState(false);
            }
            else
            {
                SetCursorState(true);
            }
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
            if (player != null && player.isDead)
                return;
            

            if (UIManager.Instance != null && (UIManager.Instance.IsUIActive(uiid.PauseScene) || UIManager.Instance.IsUIActive(uiid.OptionsScene)))
            {
                UIManager.Instance.HideUI(uiid.PauseScene);
                UIManager.Instance.HideUI(uiid.OptionsScene);

                return;
            }

            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowUI(uiid.PauseScene);
            }

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
        CallAfterCarCutscene?.Invoke();
        AferCarCutscene?.Invoke();
        CarAnimation.inCutscene = false;

        // Re-enable player controller when gameplay is enabled
        if (player != null)
        {
            player.enabled = true;
        }
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

    public void PlayerDeath()
    {
        lightSaver.SaveAllLightCollections();
        player.isDead = true;
        UIManager.Instance.ShowUI(uiid.DeathScreenScene);
        Time.timeScale = 0f;
    }
}