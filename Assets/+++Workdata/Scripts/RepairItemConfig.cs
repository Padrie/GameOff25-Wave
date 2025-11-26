using UnityEngine;
using System;

[CreateAssetMenu(fileName = "RepairItemConfig", menuName = "WaveGameJam/Repair Item Config")]
public class RepairItemConfig : ScriptableObject
{
    [Serializable]
    public class ItemSettings
    {
        public RepairItemCategory category;
        public GameObject prefab;
        public float textOffsetY = 1f;
    }

    public ItemSettings[] itemSettings = new ItemSettings[]
    {
        new ItemSettings { category = RepairItemCategory.Battery},
        new ItemSettings { category = RepairItemCategory.Generator},
        new ItemSettings { category = RepairItemCategory.Fuse}
    };

    public ItemSettings GetSettings(RepairItemCategory category)
    {
        foreach (var setting in itemSettings)
        {
            if (setting.category == category)
                return setting;
        }
        return null;
    }
}