using UnityEngine;

namespace Hakaton.Lemmings
{
    public sealed class BackgroundMusicController : MonoBehaviour
    {
        private const float MusicVolume = 0.3f;

        private static BackgroundMusicController instance;

        private AudioSource audioSource;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            audioSource = gameObject.GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            audioSource.playOnAwake = false;
            audioSource.loop = true;
            audioSource.volume = MusicVolume;
            audioSource.spatialBlend = 0f;
        }

        private void Start()
        {
            if (audioSource.isPlaying)
            {
                return;
            }

            if (audioSource.clip == null)
            {
                audioSource.clip = Resources.Load<AudioClip>("audio/3");
            }

            if (audioSource.clip != null)
            {
                audioSource.loop = true;
                audioSource.volume = MusicVolume;
                audioSource.Play();
            }
        }
    }
}
