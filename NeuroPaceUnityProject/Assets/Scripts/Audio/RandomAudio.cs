using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomAudio : MonoBehaviour
{
    public AudioClip[] audioClips;
    public bool playOnAwake = false;
    public float volume = 1.0f;
    public bool useRandomization = true;
    public float randomization = 0.0f;
    public float pitch = 1.0f;
    public float delay = 0.0f;

    public AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.volume = volume;
        audioSource.pitch = pitch;
    }

    private void Start()
    {
        if (playOnAwake)
        {
            PlayRandomAudio();
        }
    }

    public void PlayRandomAudio()
    {
        if (audioClips.Length == 0)
        {
            return;
        }

        int randomIndex = Random.Range(0, audioClips.Length);
        audioSource.clip = audioClips[randomIndex];

        if (useRandomization)
        {
            audioSource.pitch += Random.Range(-randomization, randomization);
        }

        audioSource.PlayDelayed(delay);
        audioSource.SetScheduledEndTime(AudioSettings.dspTime + audioSource.clip.length + 1.0f);
    }

    public void TurnOffSound()
    {
        audioSource.Stop();
    }

    public void SetVolume(float newVolume)
    {
        audioSource.volume = newVolume;
    }

    public void ToggleRandomization(bool toggle)
    {
        useRandomization = toggle;
    }

    public void SetRandomization(float newRandomization)
    {
        randomization = newRandomization;
    }

    public void SetPitch(float newPitch)
    {
        audioSource.pitch = newPitch;
    }

    public void SetDelay(float newDelay)
    {
        delay = newDelay;
    }
}