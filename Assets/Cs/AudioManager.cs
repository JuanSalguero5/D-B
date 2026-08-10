using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Audio Sources")]
    public AudioSource musicSource;
    public AudioSource sfxSource;

    [Header("Clips")]
    public AudioClip menuMusic;
    public AudioClip gameMusic;

    [Header("Clips de Conteo Regresivo")]
    public AudioClip readySound;
    public AudioClip goSound;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // El AudioManager no se destruye al cambiar de escena
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void PlayMenuMusic()
    {
        if (musicSource.clip != menuMusic)
        {
            musicSource.clip = menuMusic;
            musicSource.Play();
        }
    }

    public void PlayGameMusic()
    {
        if (musicSource.clip != gameMusic)
        {
            musicSource.clip = gameMusic;
            musicSource.Play();
        }
    }

    // En AudioManager.cs
    public void ResumeMusic()
    {
        if (musicSource.mute) musicSource.mute = false;
        if (!musicSource.isPlaying) musicSource.Play();
    }

    public void SetMusicVolume(bool isActive)
    {
        musicSource.mute = !isActive; // Mutea o desmutea
    }

    // --- SONIDOS DEL CONTEO REGRESIVO (Ready / Go) ---

    public void PlayReadySound()
    {
        if (sfxSource != null && readySound != null)
        {
            sfxSource.PlayOneShot(readySound);
        }
    }

    public void PlayGoSound()
    {
        if (sfxSource != null && goSound != null)
        {
            sfxSource.PlayOneShot(goSound);
        }
    }
}