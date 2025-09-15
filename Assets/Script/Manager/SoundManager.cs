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

    // Inspector���� ���� ������ �ǽð����� �ݿ��ϱ� ���� ������
    private float lastMusicVolume;
    private float lastSfxVolume;

    private void Awake()
    {
        // �̱��� ����
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SetupAudioSources();
            PlayBackgroundMusic();

            // �ʱ� ������ ����
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
        // Inspector���� ���� ������ �ǽð����� �����ϰ� ����
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
        // ������ǿ� AudioSource
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.volume = musicVolume;
        musicSource.playOnAwake = false;

        // ȿ������ AudioSource
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

    // ���� ����
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