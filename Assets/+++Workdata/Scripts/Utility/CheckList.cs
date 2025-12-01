using TMPro;
using UnityEngine;

public class CheckList : MonoBehaviour
{
    public GameObject CheckListObject;
    public TMP_Text batteryText;
    public TMP_Text generatorText;
    public TMP_Text fuseText;

    private void Awake()
    {
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
                break;
            case RepairItemCategory.Generator:
                generatorText.fontStyle = FontStyles.Strikethrough;
                break;
            case RepairItemCategory.Fuse:
                fuseText.fontStyle = FontStyles.Strikethrough;
                break;
        }
    }

    public GameObject GetCheckListObject()
    {
        return CheckListObject;
    }
}
