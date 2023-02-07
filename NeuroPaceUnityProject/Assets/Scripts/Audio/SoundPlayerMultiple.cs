using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundPlayerMultipile : MonoBehaviour
{
    public int audioSourceIndex = 0;
    public AudioSource[] audioSources;
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
        for (int i = 0; i < audioSources.Length; i++)
        {
            audioSources[i].playOnAwake = false;
        }
    }

    public void PlaySound(int audioSourceIndex)
    {
        if (audioSourceIndex < 0 || audioSourceIndex >= audioSources.Length)
        {
            Debug.LogError("Invalid audio source index: " + audioSourceIndex);
            return;
        }

        float randomDelay = randomizeDelay ? Random.Range(delayMin, delayMax) : 0.0f;
        StartCoroutine(PlaySoundCoroutine(audioSourceIndex, randomDelay));
    }

    private IEnumerator PlaySoundCoroutine(int audioSourceIndex, float delay)
    {
        yield return new WaitForSeconds(delay);
        int randomClipIndex = Random.Range(0, audioClips.Length);
        audioSources[audioSourceIndex].clip = audioClips[randomClipIndex];

        if (randomizeVolume)
        {
            audioSources[audioSourceIndex].volume = Random.Range(volumeMin, volumeMax);
        }

        if (randomizePitch)
        {
            audioSources[audioSourceIndex].pitch = Random.Range(pitchMin, pitchMax);
        }

        audioSources[audioSourceIndex].Play();
    }
}

