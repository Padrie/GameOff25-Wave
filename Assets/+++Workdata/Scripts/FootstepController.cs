using UnityEngine;

[System.Serializable]
public class FootstepSounds
{
    public AudioClip[] Walk;
    public AudioClip[] Run;
    public AudioClip[] Jump;
    public AudioClip[] Land;
}

public class FootstepController : MonoBehaviour
{
    [Header("Footstep Sets")]
    public FootstepSounds Dirt;
}
