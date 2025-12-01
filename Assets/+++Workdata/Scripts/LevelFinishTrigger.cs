using EasyPeasyFirstPersonController;
using UnityEngine;

public class LevelFinishTrigger : MonoBehaviour
{
    public FirstPersonController player;

    private void Awake()
    {
        gameObject.SetActive(false);
        player = FindFirstObjectByType<FirstPersonController>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            player.isFinished = true;
            UIManager.Instance.ShowUI(uiid.WinScreenScene);
            Time.timeScale = 0f;
        }
    }
}
