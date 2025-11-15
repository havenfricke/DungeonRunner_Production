using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Sources")]
    public AudioSource sfxSource;
    //public AudioSource musicSource; //add music later on...?

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }

    public void PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;
        sfxSource.PlayOneShot(clip, volume);
    }
}