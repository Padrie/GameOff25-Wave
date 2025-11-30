using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

public class RepairItemSpawner : MonoBehaviour
{
    public RepairItem repairItemPrefab;
    public List<RepairItemSpawnLocation> repairItemSpawnLocations;

    private void Start()
    {
        foreach (RepairItemSpawnLocation location in repairItemSpawnLocations)
        {
            GameObject item = Instantiate(repairItemPrefab.gameObject, location.spawnLocation);
            repairItemPrefab.repairItem = location.possibleRepairItems[UnityEngine.Random.Range(0, location.possibleRepairItems.Length)];
        }
    }
}

[Serializable]
public class RepairItemSpawnLocation
{
    public string name;
    public RepairItemCategory[] possibleRepairItems;
    public Transform spawnLocation;
}