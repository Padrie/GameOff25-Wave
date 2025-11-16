using System;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static event Action<Vector3, SoundStrength> onSoundEmitted;

    public static void EmitSound(Vector3 pos, SoundStrength strength)
    {
        onSoundEmitted?.Invoke(pos, strength);
    }
}
