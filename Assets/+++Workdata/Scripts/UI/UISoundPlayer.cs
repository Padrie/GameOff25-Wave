using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using UnityEngine.Audio;

public class UISoundPlayer : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    [Header("Hover Sounds")]
    [SerializeField] private List<AudioClip> hoverSounds = new List<AudioClip>();

    [Header("Click Sounds")]
    [SerializeField] private List<AudioClip> clickSounds = new List<AudioClip>();

    [Header("Audio Settings")]
    [SerializeField] private float hoverVolume = 1f;
    [SerializeField] private float clickVolume = 1f;

    private AudioSource audioSource;
    public AudioMixerGroup audioMixer;
    private List<AudioSource> additionalAudioSources = new List<AudioSource>();

    private void Awake()
    {
        InitializeAudioSource();
    }

    private void InitializeAudioSource()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.outputAudioMixerGroup = audioMixer;
        audioSource.playOnAwake = false;
        audioSource.loop = false;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        PlaySounds(hoverSounds, hoverVolume);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        PlaySounds(clickSounds, clickVolume);
    }

    private void PlaySounds(List<AudioClip> clips, float volume)
    {
        if (clips == null || clips.Count == 0)
            return;

        if (clips.Count > 0 && clips[0] != null)
        {
            PlayClipOnSource(audioSource, clips[0], volume);
        }

        for (int i = 1; i < clips.Count; i++)
        {
            if (clips[i] != null)
            {
                AudioSource layerSource = GetOrCreateAdditionalAudioSource(i - 1);
                PlayClipOnSource(layerSource, clips[i], volume);
            }
        }
    }

    private void PlayClipOnSource(AudioSource source, AudioClip clip, float volume)
    {
        if (source == null || clip == null)
            return;

        source.clip = clip;
        source.volume = volume;
        source.pitch = 1f;
        source.Play();
    }

    private AudioSource GetOrCreateAdditionalAudioSource(int index)
    {
        while (additionalAudioSources.Count <= index)
        {
            AudioSource newSource = gameObject.AddComponent<AudioSource>();
            newSource.playOnAwake = false;
            newSource.loop = false;
            additionalAudioSources.Add(newSource);
        }

        return additionalAudioSources[index];
    }

    public void PlayHoverSound()
    {
        PlaySounds(hoverSounds, hoverVolume);
    }

    public void PlayClickSound()
    {
        PlaySounds(clickSounds, clickVolume);
    }

    public void PlayCustomSound(AudioClip clip, float volume = 1f)
    {
        if (clip != null)
        {
            PlayClipOnSource(audioSource, clip, volume);
        }
    }

    public void PlayCustomSounds(List<AudioClip> clips, float volume = 1f)
    {
        PlaySounds(clips, volume);
    }

    public void StopAllSounds()
    {
        audioSource?.Stop();

        foreach (var source in additionalAudioSources)
        {
            source?.Stop();
        }
    }

    private void OnDestroy()
    {
        foreach (var source in additionalAudioSources)
        {
            if (source != null)
            {
                Destroy(source);
            }
        }
        additionalAudioSources.Clear();
    }
}