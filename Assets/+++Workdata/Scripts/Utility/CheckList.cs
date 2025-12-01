using TMPro;
using UnityEngine;

public class CheckList : MonoBehaviour
{
    public GameObject CheckListObject;
    public TMP_Text batteryText;
    public TMP_Text generatorText;
    public TMP_Text fuseText;
    public TMP_Text goToCarText;

    private void Awake()
    {
        goToCarText.gameObject.SetActive(false);
        CheckListObject.SetActive(false);
    }

    private void OnEnable()
    {
        SubSystem.OnRepaired += CrossOutTask;
    }

    private void OnDisable()
    {
        SubSystem.OnRepaired -= CrossOutTask;
    }

    public void CrossOutTask(RepairItemCategory repairItem)
    {
        switch (repairItem)
        {
            case RepairItemCategory.None:
                break;
            case RepairItemCategory.Battery:
                batteryText.fontStyle = FontStyles.Strikethrough;
                batteryText.color = Color.gray;
                break;
            case RepairItemCategory.Generator:
                generatorText.fontStyle = FontStyles.Strikethrough;
                generatorText.color = Color.gray;
                break;
            case RepairItemCategory.Fuse:
                fuseText.fontStyle = FontStyles.Strikethrough;
                fuseText.color = Color.gray;
                break;
        }
    }

    public GameObject GetCheckListObject()
    {
        return CheckListObject;
    }

    public void LevelFinished()
    {
        goToCarText.gameObject.SetActive(true);
    }
}
