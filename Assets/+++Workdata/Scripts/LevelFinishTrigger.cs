using UnityEngine;

public class LevelFinishTrigger : MonoBehaviour
{
    private void Awake()
    {
        gameObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            UIManager.Instance.ShowUI(uiid.WinScreenScene);
        }
    }
}
