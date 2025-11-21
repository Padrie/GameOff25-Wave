using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

public class RepairItemSpawner : MonoBehaviour
{
    public List<RepairItemSpawnLocation> repairItemSpawnLocations;

    private void Awake()
    {
        foreach (RepairItemSpawnLocation location in repairItemSpawnLocations)
        {
            GameObject item = Instantiate(location.possibleRepairItems[UnityEngine.Random.Range(0, location.possibleRepairItems.Length)], location.spawnLocation);
        }
    }
}

[Serializable]
public class RepairItemSpawnLocation
{
    public string name;
    public GameObject[] possibleRepairItems;
    public Transform spawnLocation;
}