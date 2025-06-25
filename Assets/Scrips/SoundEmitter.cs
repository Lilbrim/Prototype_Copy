using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundEmitter : MonoBehaviour
{
    private AudioSource audioSource;
    private AudioManager audioManager;
    private Coroutine playCoroutine;
    
    public void Initialize(AudioManager manager)
    {
        audioManager = manager;
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f; 
    }
    
    public void PlaySound(AudioManager.Sound sound, Vector3 position, float volumeMultiplier = 1f, float pitchMultiplier = 1f)
    {
        
        transform.position = position;
        
        
        audioSource.clip = sound.clip;
        audioSource.volume = sound.volume * volumeMultiplier * audioManager.GetMasterVolume();
        audioSource.pitch = sound.pitch * pitchMultiplier;
        audioSource.loop = sound.loop;
        
        
        gameObject.SetActive(true);
        audioSource.Play();
        
        
        if (!sound.loop)
        {
            playCoroutine = StartCoroutine(ReturnToPoolWhenFinished());
        }
    }
    
    private IEnumerator ReturnToPoolWhenFinished()
    {
        yield return new WaitWhile(() => audioSource.isPlaying);
        audioManager.ReturnEmitterToPool(this);
    }
    
    public void Stop()
    {
        if (audioSource.isPlaying)
        {
            audioSource.Stop();
        }
        
        if (playCoroutine != null)
        {
            StopCoroutine(playCoroutine);
            playCoroutine = null;
        }
        
        audioManager.ReturnEmitterToPool(this);
    }
    
    public void UpdateVolume(float masterVolume)
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            
            audioSource.volume = audioSource.volume / audioManager.GetMasterVolume() * masterVolume;
        }
    }
}