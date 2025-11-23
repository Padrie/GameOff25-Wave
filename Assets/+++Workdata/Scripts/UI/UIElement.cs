using System.Net.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIElement : MonoBehaviour
{
    public uiid id;

    private void Awake()
    {
        UIManager.RegisterUI(this);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void LoadOptionsMenu()
    {
        UIManager.Instance.ShowUI(uiid.OptionsScene);
    }

    public void UnloadOptionsMenu()
    {
        UIManager.Instance.HideUI(uiid.OptionsScene);
    }

    public void LoadMainMenu()
    {
        UIManager.Instance.ShowUI(uiid.MainMenuScene);
    }

    public void UnloadMainMenu()
    {
        UIManager.Instance.HideUI(uiid.MainMenuScene);
    }

    public void LoadPauseMenu()
    {
        UIManager.Instance.ShowUI(uiid.PauseScene);
    }

    public void UnloadPauseMenu()
    {
        UIManager.Instance.HideUI(uiid.PauseScene);
    }

    public void LoadMainMenuScene()
    {
        SceneManager.LoadSceneAsync("MainMenuScene", LoadSceneMode.Additive);
    }

    public void UnloadMainMenuScene()
    {
        SceneManager.UnloadSceneAsync("MainMenuScene");
    }

    public void LoadGameplayScene()
    {
        SceneManager.LoadSceneAsync("MarkScene", LoadSceneMode.Additive);
    }

    public void UnloadGameplayScene()
    {
        SceneManager.UnloadSceneAsync("MarkScene");
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
