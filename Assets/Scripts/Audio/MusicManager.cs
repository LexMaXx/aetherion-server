using UnityEngine;
using System.Collections;

/// <summary>
/// Менеджер фоновой музыки - singleton, который сохраняется между сценами
/// Плавно переключает музыку и контролирует громкость
/// </summary>
public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [Header("Audio Source")]
    [SerializeField] private AudioSource musicSource;

    [Header("Music Tracks")]
    [SerializeField] private AudioClip menuMusic; // Музыка для меню (Login, Character Selection, Game Scene)
    [SerializeField] private AudioClip battleMusic; // Музыка для боя
    [SerializeField] private AudioClip loadingMusic; // Музыка для загрузки

    [Header("Settings")]
    [SerializeField] private float defaultVolume = 0.5f;
    [SerializeField] private float fadeDuration = 2f; // Длительность плавного перехода

    private Coroutine fadeCoroutine;

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAudioSource();
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    /// <summary>
    /// Инициализация AudioSource
    /// </summary>
    private void InitializeAudioSource()
    {
        if (musicSource == null)
        {
            musicSource = gameObject.GetComponent<AudioSource>();
            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
            }
        }

        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.volume = defaultVolume;

        // Загружаем сохраненную громкость
        float savedVolume = PlayerPrefs.GetFloat("MusicVolume", defaultVolume);
        SetVolume(savedVolume);
    }

    /// <summary>
    /// Играть музыку меню
    /// </summary>
    public void PlayMenuMusic()
    {
        PlayMusic(menuMusic);
    }

    /// <summary>
    /// Играть музыку боя
    /// </summary>
    public void PlayBattleMusic()
    {
        PlayMusic(battleMusic);
    }

    /// <summary>
    /// Играть музыку загрузки
    /// </summary>
    public void PlayLoadingMusic()
    {
        PlayMusic(loadingMusic);
    }

    /// <summary>
    /// Играть конкретный трек
    /// </summary>
    public void PlayMusic(AudioClip clip, bool fade = true)
    {
        if (clip == null)
        {
            Debug.LogWarning("AudioClip не назначен!");
            return;
        }

        // Если уже играет этот трек - ничего не делаем
        if (musicSource.clip == clip && musicSource.isPlaying)
        {
            return;
        }

        if (fade)
        {
            // Плавный переход
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }
            fadeCoroutine = StartCoroutine(FadeToNewTrack(clip));
        }
        else
        {
            // Мгновенная смена
            musicSource.clip = clip;
            musicSource.Play();
        }
    }

    /// <summary>
    /// Остановить музыку
    /// </summary>
    public void StopMusic(bool fade = true)
    {
        if (fade)
        {
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }
            fadeCoroutine = StartCoroutine(FadeOut());
        }
        else
        {
            musicSource.Stop();
        }
    }

    /// <summary>
    /// Пауза
    /// </summary>
    public void PauseMusic()
    {
        musicSource.Pause();
    }

    /// <summary>
    /// Продолжить воспроизведение
    /// </summary>
    public void ResumeMusic()
    {
        musicSource.UnPause();
    }

    /// <summary>
    /// Установить громкость
    /// </summary>
    public void SetVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);
        musicSource.volume = volume;
        PlayerPrefs.SetFloat("MusicVolume", volume);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Получить текущую громкость
    /// </summary>
    public float GetVolume()
    {
        return musicSource.volume;
    }

    /// <summary>
    /// Плавный переход к новому треку
    /// </summary>
    private IEnumerator FadeToNewTrack(AudioClip newClip)
    {
        float startVolume = musicSource.volume;

        // Fade out
        if (musicSource.isPlaying)
        {
            float elapsed = 0f;
            while (elapsed < fadeDuration / 2f)
            {
                elapsed += Time.deltaTime;
                musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / (fadeDuration / 2f));
                yield return null;
            }
            musicSource.volume = 0f;
        }

        // Меняем трек
        musicSource.clip = newClip;
        musicSource.Play();

        // Fade in
        float elapsed2 = 0f;
        while (elapsed2 < fadeDuration / 2f)
        {
            elapsed2 += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(0f, startVolume, elapsed2 / (fadeDuration / 2f));
            yield return null;
        }
        musicSource.volume = startVolume;

        fadeCoroutine = null;
    }

    /// <summary>
    /// Плавное затухание
    /// </summary>
    private IEnumerator FadeOut()
    {
        float startVolume = musicSource.volume;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / fadeDuration);
            yield return null;
        }

        musicSource.volume = 0f;
        musicSource.Stop();
        musicSource.volume = startVolume; // Восстанавливаем громкость

        fadeCoroutine = null;
    }

    /// <summary>
    /// Проверка, играет ли музыка
    /// </summary>
    public bool IsPlaying()
    {
        return musicSource.isPlaying;
    }
}
