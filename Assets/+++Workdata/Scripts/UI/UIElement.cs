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

    public void LoadDeathScreen()
    {
        UIManager.Instance.ShowUI(uiid.DeathScreenScene);
        Time.timeScale = 0f;
    }

    public void UnloadDeathScreen()
    {
        UIManager.Instance.HideUI(uiid.DeathScreenScene);
        Time.timeScale = 1f;
    }

    public void LoadWinScreen()
    {
        UIManager.Instance.ShowUI(uiid.WinScreenScene);
    }

    public void UnloadWinScreen()
    {
        UIManager.Instance.HideUI(uiid.WinScreenScene);
        Time.timeScale = 1f;
    }

    public void LoadCreditsScreen()
    {
        UIManager.Instance.ShowUI(uiid.CreditsScene);
    }

    public void UnloadCreditsScreen()
    {
        UIManager.Instance.HideUI(uiid.CreditsScene);
    }

    public void LoadMainMenuScene()
    {
        SceneManager.LoadSceneAsync("MainMenuScene", LoadSceneMode.Additive);
    }

    public void UnloadMainMenuScene()
    {
        SceneManager.UnloadSceneAsync("MainMenuScene");
    }

    public void ReloadGameplayScene()
    {
        SceneManager.LoadScene("Ingame");
    }

    public void LoadGameplayScene()
    {
        SceneManager.LoadSceneAsync("InGame", LoadSceneMode.Additive);
    }

    public void UnloadGameplayScene()
    {
        SceneManager.UnloadSceneAsync("InGame");
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
