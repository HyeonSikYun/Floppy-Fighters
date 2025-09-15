using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Audio Clips")]
    public AudioClip backgroundMusic;
    public AudioClip hitSound;

    [Header("Volume Settings")]
    [Range(0f, 1f)] public float musicVolume = 0.5f;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    private AudioSource musicSource;
    private AudioSource sfxSource;

    // Inspector에서 볼륨 변경을 실시간으로 반영하기 위한 변수들
    private float lastMusicVolume;
    private float lastSfxVolume;

    private void Awake()
    {
        // 싱글톤 패턴
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SetupAudioSources();
            PlayBackgroundMusic();

            // 초기 볼륨값 저장
            lastMusicVolume = musicVolume;
            lastSfxVolume = sfxVolume;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        // Inspector에서 볼륨 변경을 실시간으로 감지하고 적용
        if (musicVolume != lastMusicVolume)
        {
            lastMusicVolume = musicVolume;
            if (musicSource != null)
                musicSource.volume = musicVolume;
        }

        if (sfxVolume != lastSfxVolume)
        {
            lastSfxVolume = sfxVolume;
            if (sfxSource != null)
                sfxSource.volume = sfxVolume;
        }
    }

    void SetupAudioSources()
    {
        // 배경음악용 AudioSource
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.volume = musicVolume;
        musicSource.playOnAwake = false;

        // 효과음용 AudioSource
        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.loop = false;
        sfxSource.volume = sfxVolume;
        sfxSource.playOnAwake = false;
    }

    void PlayBackgroundMusic()
    {
        if (backgroundMusic != null && musicSource != null)
        {
            musicSource.clip = backgroundMusic;
            musicSource.Play();
            Debug.Log("Background music started");
        }
    }

    public void PlayHitSound()
    {
        if (hitSound != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(hitSound);
            Debug.Log("Hit sound played");
        }
    }

    // 볼륨 조절
    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        if (musicSource != null)
            musicSource.volume = musicVolume;
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        if (sfxSource != null)
            sfxSource.volume = sfxVolume;
    }
}