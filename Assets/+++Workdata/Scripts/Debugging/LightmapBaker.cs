using UnityEngine;

[System.Serializable]
public class BakeableObject
{
    public GameObject gameObject;
    public bool shouldBake = false;
}

public class LightmapBaker : MonoBehaviour
{
    [SerializeField]
    public BakeableObject[] objectsToBake = new BakeableObject[0];

    [SerializeField]
    public ReflectionProbe[] reflectionProbesToBake = new ReflectionProbe[0];

    [SerializeField]
    public Object lightmapSettings;
}