using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundPlayer : MonoBehaviour
{
    public AudioSource audioSource;
    public AudioClip[] audioClips;
    public bool randomizeVolume = true;
    public float volumeMin = 0.5f;
    public float volumeMax = 1.0f;
    public bool randomizePitch = true;
    public float pitchMin = 0.5f;
    public float pitchMax = 1.5f;
    public bool randomizeDelay = true;
    public float delayMin = 0.0f;
    public float delayMax = 1.0f;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public void PlayRandomSoundWithDelay()
    {
        float randomDelay = Random.Range(delayMin, delayMax);
        Invoke("PlayRandomSound", randomDelay);
    }

    private void PlayRandomSound()
    {
        int randomClipIndex = Random.Range(0, audioClips.Length);
        audioSource.clip = audioClips[randomClipIndex];
        audioSource.loop = true;

        if (randomizeVolume)
        {
            audioSource.volume = Random.Range(volumeMin, volumeMax);
        }

        if (randomizePitch)
        {
            audioSource.pitch = Random.Range(pitchMin, pitchMax);
        }

        audioSource.Play();
    }

    private void OnEnable()
    {
        PlayRandomSoundWithDelay();
    }
}