using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomizedSoundTrigger : MonoBehaviour
{
    [SerializeField] private AudioClip[] audioClips;
    [SerializeField] private AudioSource audioSource;

    [SerializeField] private bool playOnAwake;
    [SerializeField] private bool randomizePitch;
    [SerializeField] private float pitchRangeMin = 0.9f;
    [SerializeField] private float pitchRangeMax = 1.1f;
    [SerializeField] private bool randomizeVolume;
    [SerializeField] private float volumeRangeMin = 0.9f;
    [SerializeField] private float volumeRangeMax = 1.1f;
    [SerializeField] private int randomizeFrom = 0;

    private int lastClipIndex = -1;

    private void Start()
    {
        if (playOnAwake)
        {
            PlayRandomSound();
        }
    }

    public void PlayRandomSound()
    {
        int clipIndex = GetRandomClipIndex();
        audioSource.clip = audioClips[clipIndex];

        if (randomizePitch)
        {
            audioSource.pitch = Random.Range(pitchRangeMin, pitchRangeMax);
        }

        if (randomizeVolume)
        {
            audioSource.volume = Random.Range(volumeRangeMin, volumeRangeMax);
        }

        audioSource.Play();
        lastClipIndex = clipIndex;

        // Save the index of the last played clip to PlayerPrefs
        PlayerPrefs.SetInt("lastClipIndex", lastClipIndex);
        PlayerPrefs.Save();
    }

    private int GetRandomClipIndex()
    {
        int randomIndex = lastClipIndex;

        while (randomIndex == lastClipIndex || randomIndex >= randomizeFrom)
        {
            randomIndex = Random.Range(0, randomizeFrom);
        }

        return randomIndex;
    }
}


