using EasyPeasyFirstPersonController;
using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class LightSaver : MonoBehaviour
{

    [SerializeField] private GameObject officeLights;
    [SerializeField] private GameObject mechanicLights;
    [SerializeField] private GameObject warehouseLights;


    void Start()
    {
        //if no PlayerPrefs exist for the lights, create them and set to off
        CheckIfPlayerPrefsExist();

        if (gameObject.scene.name == "MainMenuScene")
        {
            LoadAllLightCollections();
        }

        if (gameObject.scene.name == "InGame")
        {
            officeLights.SetActive(false);
            mechanicLights.SetActive(false);
            warehouseLights.SetActive(false);
        }

    }



    //0 means lights off by default
    private void CheckIfPlayerPrefsExist()
    {
        if (!PlayerPrefs.HasKey(officeLights.name))
        {
            PlayerPrefs.SetInt(officeLights.name, 0);
        }
        if (!PlayerPrefs.HasKey(mechanicLights.name))
        {
            PlayerPrefs.SetInt(mechanicLights.name, 0);
        }
        if (!PlayerPrefs.HasKey(warehouseLights.name))
        {
            PlayerPrefs.SetInt(warehouseLights.name, 0);
        }
    }


    private void Update()
    {
        //debug delete all light playerprefs
        if (Input.GetKeyDown(KeyCode.P))
        {
            PlayerPrefs.DeleteKey(officeLights.name);
            PlayerPrefs.DeleteKey(mechanicLights.name);
            PlayerPrefs.DeleteKey(warehouseLights.name);

            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            Debug.Log("Deleted all light PlayerPrefs");

        }
    }



    //Saves the state of a single light collection to PlayerPrefs
    public void SaveAllLightCollections()
    {

        GameObject[] allLightCollections = { officeLights, mechanicLights, warehouseLights };

        foreach (GameObject lightCollection in allLightCollections)
        {
            if (lightCollection != null)
            {
                SaveLightCollection(lightCollection);
            }
        }
        PlayerPrefs.Save();
    }

    private void SaveLightCollection(GameObject lightCollection)
    {
        int lightsOff = 0;
        int lightsOn = 1;
        if (lightCollection.activeSelf)
        {
            PlayerPrefs.SetInt(lightCollection.name, lightsOn);
            Debug.Log("Saved " + lightCollection.name + " as ON");
        }
        else
        {
            PlayerPrefs.SetInt(lightCollection.name, lightsOff);
            Debug.Log("Saved " + lightCollection.name + " as OFF");
        }
    }


    //Loads every light collection based on their saved PlayerPrefs state
    public void LoadAllLightCollections()
    {
        LoadLightCollection(officeLights);
        LoadLightCollection(mechanicLights);
        LoadLightCollection(warehouseLights);
    }


    //Loads a single light collection based on its saved PlayerPrefs state
    private void LoadLightCollection(GameObject lightCollection)
    {
        int lightsOff = 0;
        int lightsOn = 1;
        int lightState = PlayerPrefs.GetInt(lightCollection.name);

        if (lightState == lightsOn)
        {
            lightCollection.SetActive(true);
            Debug.Log("Loaded " + lightCollection.name + " as ON");
        }
        else if (lightState == lightsOff)
        {
            lightCollection.SetActive(false);
            Debug.Log("Loaded " + lightCollection.name + " as OFF");
        }
    }

}
