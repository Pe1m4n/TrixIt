using UnityEngine;

namespace Trixit
{
    public class AudioPlayer : MonoBehaviour
    {
        private static float VOLUME = 0.5f;
        private static AudioPlayer _instance;

        public static AudioPlayer Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new GameObject().AddComponent<AudioPlayer>();
                }

                return _instance;
            }
        }
        
        private AudioSource _sourceGlobal;
        private AudioSource _sourceLocal;
        
        private void Awake()
        {
            _sourceGlobal = gameObject.AddComponent<AudioSource>();
            _sourceLocal = gameObject.AddComponent<AudioSource>();

            _sourceGlobal.volume = VOLUME;
            _sourceLocal.volume = VOLUME;
            DontDestroyOnLoad(gameObject);
            _instance = this;
        }

        public void PlaySound(AudioClip clip, bool global = false)
        {
            var source = global ? _sourceGlobal : _sourceLocal;
            source.PlayOneShot(clip);
        }

        public void StopLocal()
        {
            _sourceLocal.Stop();
        }
        
        private void OnDestroy()
        {
            _instance = null;
        }
    }
}