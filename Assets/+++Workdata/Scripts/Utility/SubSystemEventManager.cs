using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

public class SubSystemEventManager : MonoBehaviour
{
    int systemAmount;

    public int amountOfSystemsRepaired
    {
        get
        {
            return _amountOfSystemsRepaired;
        }
        set
        {
            _amountOfSystemsRepaired = value;
            if (_amountOfSystemsRepaired == systemAmount)
                RepairedAllSystems();
        }
    }

    int _amountOfSystemsRepaired;

    private void Awake()
    {
        systemAmount = FindObjectsByType(typeof(SubSystem), FindObjectsSortMode.None).Length;
        print(systemAmount);
    }

    private void OnEnable()
    {
        SubSystem.OnRepaired += AddToRepaired;
    }

    private void OnDisable()
    {
        SubSystem.OnRepaired -= AddToRepaired;
    }

    public void AddToRepaired()
    {
        amountOfSystemsRepaired++;
    }

    public void RepairedAllSystems()
    {
        print("Repaired all Systems");
    }
}