using UnityEngine;
using UnityEngine.SceneManagement;

public class LightSaver : MonoBehaviour
{
    void Start()
    {
        LoadAllLightCollections();
    }


    public void SaveLightCollection(GameObject lightCollection, string prefKey)
    {
        int lightsOn = 0;
        foreach (Transform light in lightCollection.transform)
        {
            if (light.gameObject.activeSelf)
            {
                lightsOn++;
            }
        }
        PlayerPrefs.SetInt(prefKey, lightsOn);
    }


    private void LoadAllLightCollections()
    {
        LoadLightCollection("OfficeLights", "OfficeLightCollection");
        LoadLightCollection("MechanicLights", "MechanicLightCollection");
        LoadLightCollection("WarehouseLights", "WarehouseLightCollection");
    }

    private void LoadLightCollection(string prefKey, string collectionName)
    {
        int lightsToActivate = PlayerPrefs.GetInt(prefKey, 0);
        GameObject lightCollection = GameObject.Find(collectionName);
        if (lightCollection != null)
        {
            int activatedCount = 0;
            foreach (Transform light in lightCollection.transform)
            {
                if (activatedCount < lightsToActivate)
                {
                    light.gameObject.SetActive(true);
                    activatedCount++;
                }
                else
                {
                    light.gameObject.SetActive(false);
                }
            }
        }
    }

}
