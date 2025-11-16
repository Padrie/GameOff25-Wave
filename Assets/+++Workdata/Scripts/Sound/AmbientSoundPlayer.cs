using UnityEngine;
using System.Collections;

public class AmbientSoundPlayer : MonoBehaviour
{
    [Header("Ambient Sounds")]
    public AudioClip windSound;
    public AudioClip ravenSound;
    public AudioClip chirpSound;

    [Header("Settings")]
    public float minDelay = 5f;
    public float maxDelay = 15f;
    [Range(0f, 1f)] public float volume = 1f;
    public float spatialBlend = 0f;
    public float audioSourceLifetime = 5f;

    private AudioSource ambientSource;

    void Start()
    {
        ambientSource = gameObject.AddComponent<AudioSource>();
        ambientSource.loop = true;
        ambientSource.volume = volume;
        ambientSource.spatialBlend = spatialBlend;

        //Play the wind sound continuously
        if (windSound != null)
        {
            ambientSource.clip = windSound;
            ambientSource.Play();
        }

        //Play random bird/raven sounds
        InvokeRepeating("PlayRandomBird", 2f, Random.Range(minDelay, maxDelay));
    }

    void PlayRandomBird()
    {
        AudioClip clip = Random.value > 0.5f ? ravenSound : chirpSound;
        if (clip != null)
        {
            PlayAmbientSound(clip);
        }
    }

    private void PlayAmbientSound(AudioClip clip)
    {
        if (clip == null) return;

        GameObject audioObject = new GameObject("AmbientAudio");
        audioObject.transform.position = transform.position;
        AudioSource audioSource = audioObject.AddComponent<AudioSource>();
        audioSource.clip = clip;
        audioSource.volume = volume;
        audioSource.spatialBlend = spatialBlend;
        audioSource.Play();

        StartCoroutine(DestroyAudioSource(audioObject, audioSourceLifetime));
    }

    private IEnumerator DestroyAudioSource(GameObject audioObject, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (audioObject != null)
            Destroy(audioObject);
    }
}