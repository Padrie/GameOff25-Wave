using UnityEngine;

public class AudioPlayer : MonoBehaviour
{
    [Header("Repair SFX")]
    public AudioClip falseRepairItemClip;
    public AudioClip correctRepairItemClip;

    [Header("Settings")]
    [Range(0f, 1f)]
    public float volume = 1f;

    [Range(0, 1)]
    public int spatialBlend = 1;

    public void PlayClipAtPoint(AudioClip clip, Vector3 position)
    {
        if (clip == null) return;

        GameObject tempAudioObject = new GameObject("TempAudio");
        tempAudioObject.transform.position = position;

        AudioSource audioSource = tempAudioObject.AddComponent<AudioSource>();
        audioSource.clip = clip;
        audioSource.volume = volume;
        audioSource.spatialBlend = spatialBlend;
        audioSource.Play();

        Destroy(tempAudioObject, clip.length);
    }

    public void PlayClipAtPoint(AudioClip clip, Vector3 position, float customVolume, int customSpatialBlend)
    {
        if (clip == null) return;

        GameObject tempAudioObject = new GameObject("TempAudio");
        tempAudioObject.transform.position = position;

        AudioSource audioSource = tempAudioObject.AddComponent<AudioSource>();
        audioSource.clip = clip;
        audioSource.volume = Mathf.Clamp01(customVolume);
        audioSource.spatialBlend = Mathf.Clamp01(customSpatialBlend);
        audioSource.Play();

        Destroy(tempAudioObject, clip.length);
    }

    public void PlayFalseRepairItemSFX(Vector3 position)
    {
        PlayClipAtPoint(falseRepairItemClip, position);
    }

    public void PlayCorrectRepairItemSFX(Vector3 position)
    {
        PlayClipAtPoint(correctRepairItemClip, position);
    }
}