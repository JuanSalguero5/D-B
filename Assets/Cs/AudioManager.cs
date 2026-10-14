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

    [Header("Sonidos del Coche")]
    public AudioSource carSource; // Bucle del motor del coche
    public AudioClip carEngineClip;
    public AudioClip damageClip;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // El AudioManager no se destruye al cambiar de escena

            // Si no asignaron un AudioSource para el motor, lo creamos nosotros.
            if (carSource == null)
            {
                carSource = gameObject.AddComponent<AudioSource>();
                carSource.loop = true;
                carSource.playOnAwake = false;
                carSource.spatialBlend = 0f;
            }
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

    // --- SONIDOS DEL COCHE (Motor y Daño) ---

    public void PlayEngineSound()
    {
        if (carSource != null && carEngineClip != null)
        {
            if (carSource.clip != carEngineClip)
                carSource.clip = carEngineClip;
            if (!carSource.isPlaying)
                carSource.Play();
        }
    }

    public void StopEngineSound()
    {
        if (carSource != null)
            carSource.Stop();
    }

    public void PlayDamageSound()
    {
        if (sfxSource != null && damageClip != null)
        {
            sfxSource.PlayOneShot(damageClip);
        }
    }
}