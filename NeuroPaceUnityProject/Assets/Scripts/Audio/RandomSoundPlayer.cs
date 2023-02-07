using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomSoundPlayer : MonoBehaviour
{
    public AudioClip[] audioClips;
    public float minPitch = 0.9f;
    public float maxPitch = 1.1f;
    public float minVolume = 0.5f;
    public float maxVolume = 1.0f;
    public float minInterval = 1.0f;
    public float maxInterval = 3.0f;
    public bool randomizePitch = true;
    public bool randomizeVolume = true;
    public bool randomizeInterval = true;
    public AudioSource audioSource;

    public float timeToNextSound;
    public int currentClipIndex;
    private int[] playedClipsIndices;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        timeToNextSound = Time.time + Random.Range(minInterval, maxInterval);
        playedClipsIndices = new int[audioClips.Length];
    }

    private void Update()
    {
        if (Time.time > timeToNextSound)
        {
            if (audioSource.isPlaying)
            {
                audioSource.Stop();
            }
            PlayRandomSound();
            timeToNextSound = Time.time + (randomizeInterval ? Random.Range(minInterval, maxInterval) : (minInterval + maxInterval) / 2.0f);
        }
    }

    private void PlayRandomSound()
    {
        currentClipIndex = GetNextClipIndex();

        float randomPitch = randomizePitch ? Random.Range(minPitch, maxPitch) : (minPitch + maxPitch) / 2.0f;
        float randomVolume = randomizeVolume ? Random.Range(minVolume, maxVolume) : (minVolume + maxVolume) / 2.0f;

        audioSource.pitch = randomPitch;
        audioSource.volume = randomVolume;
        audioSource.clip = audioClips[currentClipIndex];
        audioSource.Play();
    }

    private int GetNextClipIndex()
    {
        int randomIndex = Random.Range(0, audioClips.Length);
        while (playedClipsIndices[randomIndex] == 1)
        {
            randomIndex = Random.Range(0, audioClips.Length);
        }
        playedClipsIndices[randomIndex] = 1;
        return randomIndex;
    }
}


