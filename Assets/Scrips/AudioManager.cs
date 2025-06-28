using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class AudioManager : MonoBehaviour
{
    [System.Serializable]
    public class Sound
    {
        public string name;
        public AudioClip clip;
        [Range(0f, 1f)]
        public float volume = 1f;
        [Range(0.1f, 3f)]
        public float pitch = 1f;
        public bool loop = false;

        [HideInInspector]
        public AudioSource source;
    }

    public static AudioManager Instance;

    [Header("Background Music")]
    public AudioClip defaultBGM;
    public AudioClip levelManagerBGM;
    public AudioClip sparManagerBGM;

    [Header("Sound Effects")]
    public Sound[] soundEffects;

    [Header("Audio Sources")]
    public AudioSource bgmSource;
    private List<AudioSource> sfxSources = new List<AudioSource>();
    private const int MAX_SFX_SOURCES = 10;

    [Header("Pooled Sound Emitters")]
    [SerializeField] private int poolSize = 20;
    private Queue<SoundEmitter> availableEmitters = new Queue<SoundEmitter>();
    private List<SoundEmitter> activeEmitters = new List<SoundEmitter>();

    [Header("Volume Settings")]
    [Range(0f, 0.1f)]
    public float masterVolume = 0.05f;
    private const string VOLUME_PREF_KEY = "MasterVolume";

    private LevelManager levelManager;
    private SparManager sparManager;
    private AudioClip currentBGM;


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAudioManager();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeAudioManager()
    {

        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.loop = true;
        bgmSource.playOnAwake = false;


        for (int i = 0; i < MAX_SFX_SOURCES; i++)
        {
            AudioSource sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSources.Add(sfxSource);
        }


        foreach (Sound sound in soundEffects)
        {
            sound.source = gameObject.AddComponent<AudioSource>();
            sound.source.clip = sound.clip;
            sound.source.volume = sound.volume;
            sound.source.pitch = sound.pitch;
            sound.source.loop = sound.loop;
            sound.source.playOnAwake = false;
        }


        InitializeSoundEmitterPool();


        masterVolume = PlayerPrefs.GetFloat(VOLUME_PREF_KEY, 0.05f);
        ApplyVolumeSettings();
    }

    private void InitializeSoundEmitterPool()
    {

        GameObject poolParent = new GameObject("Sound Emitter Pool");
        poolParent.transform.SetParent(transform);


        for (int i = 0; i < poolSize; i++)
        {
            GameObject emitterObj = CreateSoundEmitter();
            emitterObj.transform.SetParent(poolParent.transform);
            SoundEmitter emitter = emitterObj.GetComponent<SoundEmitter>();
            emitter.Initialize(this);
            emitterObj.SetActive(false);
            availableEmitters.Enqueue(emitter);
        }
    }

    private GameObject CreateSoundEmitter()
    {
        GameObject emitter = new GameObject("Sound Emitter");
        emitter.AddComponent<AudioSource>();
        emitter.AddComponent<SoundEmitter>();
        return emitter;
    }

    private void Start()
    {
        StartCoroutine(CheckForManagers());
    }

    private IEnumerator CheckForManagers()
    {
        while (true)
        {
            LevelManager currentLevelManager = FindObjectOfType<LevelManager>();
            SparManager currentSparManager = FindObjectOfType<SparManager>();

            AudioClip targetBGM = defaultBGM;

            if (currentLevelManager != null && currentLevelManager.gameObject.activeInHierarchy)
            {
                targetBGM = levelManagerBGM;
            }
            else if (currentSparManager != null && currentSparManager.gameObject.activeInHierarchy)
            {
                targetBGM = sparManagerBGM;
            }

            if (targetBGM != currentBGM)
            {
                SwitchBGM(targetBGM);
            }

            yield return new WaitForSeconds(1f);
        }
    }

    private void SwitchBGM(AudioClip newBGM)
    {
        if (newBGM == null) return;

        currentBGM = newBGM;
        bgmSource.clip = newBGM;
        bgmSource.Play();
    }


    public void PlaySound(string soundName)
    {
        Sound sound = System.Array.Find(soundEffects, s => s.name == soundName);
        if (sound == null)
        {
            Debug.LogWarning($"Sound '{soundName}' not found in AudioManager!");
            return;
        }

        sound.source.Play();
    }


    public void PlaySound(string soundName, Vector3 position, float volumeMultiplier = 1f, float pitchMultiplier = 1f)
    {
        Sound sound = System.Array.Find(soundEffects, s => s.name == soundName);
        if (sound == null)
        {
            Debug.LogWarning($"Sound '{soundName}' not found in AudioManager!");
            return;
        }

        SoundEmitter emitter = GetAvailableEmitter();
        if (emitter != null)
        {
            emitter.PlaySound(sound, position, volumeMultiplier, pitchMultiplier);
        }
    }


    public void PlaySoundAtGameObject(string soundName, GameObject target, float volumeMultiplier = 1f, float pitchMultiplier = 1f)
    {
        if (target != null)
        {
            PlaySound(soundName, target.transform.position, volumeMultiplier, pitchMultiplier);
        }
    }


    public static void PlaySoundStatic(string soundName, Vector3 position, float volumeMultiplier = 1f, float pitchMultiplier = 1f)
    {
        if (Instance != null)
        {
            Instance.PlaySound(soundName, position, volumeMultiplier, pitchMultiplier);
        }
    }


    public static void PlaySoundAtGameObjectStatic(string soundName, GameObject target, float volumeMultiplier = 1f, float pitchMultiplier = 1f)
    {
        if (Instance != null)
        {
            Instance.PlaySoundAtGameObject(soundName, target, volumeMultiplier, pitchMultiplier);
        }
    }

    private SoundEmitter GetAvailableEmitter()
    {
        if (availableEmitters.Count > 0)
        {
            SoundEmitter emitter = availableEmitters.Dequeue();
            activeEmitters.Add(emitter);
            return emitter;
        }

        Debug.LogWarning("No available sound emitters! Creating temporary emitter.");
        GameObject tempEmitter = CreateSoundEmitter();
        SoundEmitter tempEmitterComponent = tempEmitter.GetComponent<SoundEmitter>();
        tempEmitterComponent.Initialize(this);
        activeEmitters.Add(tempEmitterComponent);
        return tempEmitterComponent;
    }


    public void ReturnEmitterToPool(SoundEmitter emitter)
    {
        if (activeEmitters.Contains(emitter))
        {
            activeEmitters.Remove(emitter);
            emitter.gameObject.SetActive(false);
            availableEmitters.Enqueue(emitter);
        }
    }

    private AudioSource GetAvailableSFXSource()
    {
        foreach (AudioSource source in sfxSources)
        {
            if (!source.isPlaying)
            {
                return source;
            }
        }

        return sfxSources[0];
    }

    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(VOLUME_PREF_KEY, masterVolume);
        PlayerPrefs.Save();

        ApplyVolumeSettings();
    }

    private void ApplyVolumeSettings()
    {
        if (bgmSource != null)
        {
            bgmSource.volume = masterVolume;
        }

        foreach (Sound sound in soundEffects)
        {
            if (sound.source != null)
            {
                sound.source.volume = sound.volume * masterVolume;
            }
        }

        foreach (AudioSource source in sfxSources)
        {
            source.volume = masterVolume;
        }

        foreach (SoundEmitter emitter in activeEmitters)
        {
            emitter.UpdateVolume(masterVolume);
        }
    }

    public float GetMasterVolume()
    {
        return masterVolume;
    }

    public void StopSound(string soundName)
    {
        Sound sound = System.Array.Find(soundEffects, s => s.name == soundName);
        if (sound != null && sound.source != null)
        {
            sound.source.Stop();
        }
    }

    public void StopAllSounds()
    {
        foreach (Sound sound in soundEffects)
        {
            if (sound.source != null)
            {
                sound.source.Stop();
            }
        }

        foreach (AudioSource source in sfxSources)
        {
            source.Stop();
        }

        foreach (SoundEmitter emitter in activeEmitters)
        {
            emitter.Stop();
        }
    }

    public void PauseBGM()
    {
        if (bgmSource != null && bgmSource.isPlaying)
        {
            bgmSource.Pause();
        }
    }

    public void ResumeBGM()
    {
        if (bgmSource != null && !bgmSource.isPlaying)
        {
            bgmSource.UnPause();
        }
    }

    public void StopBGM()
    {
        if (bgmSource != null)
        {
            bgmSource.Stop();
        }
    }
    
}