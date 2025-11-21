using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System.Collections;

public class LightSystem : MonoBehaviour
{
    [SerializeField] List<Light> lightsConnectedToSubSystem = new List<Light>();
    [SerializeField] List<Light> flickerLights = new List<Light>();

    [SerializeField] Vector2Int flickDelayRange = new Vector2Int(5, 50);
    [SerializeField] Vector2Int flickAmountRange = new Vector2Int(5, 20);

    public void ActivateLights()
    {
        foreach (Light light in lightsConnectedToSubSystem)
        {
            light.gameObject.SetActive(true);
            light.intensity = 0;
            StartCoroutine(TurnOnLights(light));
        }
    }

    private IEnumerator TurnOnLights(Light light)
    {
        light.DOIntensity(20, 3f);

        yield return new WaitForSeconds(3f);

        if (flickerLights.Contains(light))
            StartCoroutine(LightFluctuation(light));
    }

    private IEnumerator LightFluctuation(Light light)
    {
        float minIntensity = 15f;
        float maxIntensity = 20f;

        int whenToFlick = UnityEngine.Random.Range(flickDelayRange.x, flickDelayRange.y);
        int flickAmount = UnityEngine.Random.Range(flickAmountRange.x, flickAmountRange.y);

        int i = 0;

        while (true)
        {
            if (i >= whenToFlick)
            {
                yield return StartCoroutine(LightFLicker(light, flickAmount));

                i = 0;
                whenToFlick = UnityEngine.Random.Range(flickDelayRange.x, flickDelayRange.y);
                flickAmount = UnityEngine.Random.Range(flickAmountRange.x, flickAmountRange.y);

                continue;
            }

            light.DOIntensity(UnityEngine.Random.Range(minIntensity, maxIntensity), .1f);
            yield return new WaitForSeconds(.11f);
            i++;
        }
    }

    private IEnumerator LightFLicker(Light light, int flickerAmount)
    {
        float minIntensity = 5f;
        float maxIntensity = 20f;

        int t = 0;

        while (t < flickerAmount)
        {
            t++;
            light.intensity = UnityEngine.Random.Range(minIntensity, maxIntensity);
            yield return new WaitForSeconds(0.05f);
        }

        yield return null;
    }
}
