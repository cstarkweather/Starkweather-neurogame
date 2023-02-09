using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayCustomizableRandomSoundOnAnimation : MonoBehaviour
{
    public AudioClip[] sounds;
    public bool randomizePitch = true;
    public float minPitch = 0.8f;
    public float maxPitch = 1.2f;
    public bool randomizeVolume = true;
    public float minVolume = 0.5f;
    public float maxVolume = 1.0f;

    public AudioSource source1;
    public AudioSource source2;

    private int assignedSoundIndex = 1;

    private void Start()
    {
        source1 = GetComponent<AudioSource>();
        source2 = GetComponent<AudioSource>();
    }

    public void PlayCustomizableRandomSound()
    {
        AudioSource source;
        if (assignedSoundIndex == 1)
        {
            source = source1;
        }
        else if (assignedSoundIndex == 2)
        {
            source = source2;
        }
        else
        {
            return;
        }

        int randomIndex = Random.Range(0, sounds.Length);

        float randomPitch = source.pitch;
        if (randomizePitch)
        {
            randomPitch = Random.Range(minPitch, maxPitch);
        }

        float randomVolume = source.volume;
        if (randomizeVolume)
        {
            randomVolume = Random.Range(minVolume, maxVolume);
        }

        source.pitch = randomPitch;
        source.volume = randomVolume;
        source.PlayOneShot(sounds[randomIndex]);
    }

    public void SetAssignedSoundIndex(int soundIndex)
    {
        assignedSoundIndex = soundIndex;
    }
}


